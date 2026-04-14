"""API callers for each provider."""

import os
import time
import httpx
from dataclasses import dataclass


@dataclass
class APIResult:
    content: str
    input_tokens: int
    output_tokens: int
    stop_reason: str


def call_model(
    provider_name: str,
    model_id: str,
    system_prompt: str,
    user_message: str,
    max_tokens: int,
    config: dict
) -> APIResult:
    """Call an AI API synchronously. Handles retries."""
    caller = {
        "anthropic": _call_anthropic,
        "google": _call_google,
    }[provider_name]

    max_retries = config["retry"]["max_retries"]
    backoff_base = config["retry"]["backoff_base_seconds"]
    last_error = None

    for attempt in range(max_retries + 1):
        try:
            return caller(model_id, system_prompt, user_message,
                          max_tokens, config)
        except (httpx.HTTPStatusError, httpx.TimeoutException) as e:
            last_error = e
            if attempt < max_retries:
                wait = backoff_base * (2 ** attempt)
                time.sleep(wait)

    raise RuntimeError(
        f"API call failed after {max_retries + 1} attempts: {last_error}"
    )


def _call_anthropic(model_id, system_prompt, user_message,
                     max_tokens, config):
    api_key = os.environ[config["api_key_env"]]
    with httpx.Client(timeout=config["timeout_seconds"]) as client:
        resp = client.post(
            config.get("base_url",
                        "https://api.anthropic.com/v1/messages"),
            headers={
                "x-api-key": api_key,
                "anthropic-version": config.get("api_version",
                                                "2023-06-01"),
                "content-type": "application/json",
            },
            json={
                "model": model_id,
                "max_tokens": max_tokens,
                "system": system_prompt,
                "messages": [
                    {"role": "user", "content": user_message}
                ],
            },
        )
        resp.raise_for_status()
        data = resp.json()

        content = "".join(
            b["text"] for b in data["content"] if b["type"] == "text"
        )
        return APIResult(
            content=content,
            input_tokens=data["usage"]["input_tokens"],
            output_tokens=data["usage"]["output_tokens"],
            stop_reason=data.get("stop_reason", "unknown"),
        )


def _call_google(model_id, system_prompt, user_message,
                  max_tokens, config):
    api_key = os.environ[config["api_key_env"]]
    base = config.get(
        "base_url",
        "https://generativelanguage.googleapis.com/v1beta"
    )
    url = f"{base}/models/{model_id}:generateContent?key={api_key}"

    with httpx.Client(timeout=config["timeout_seconds"]) as client:
        resp = client.post(url, json={
            "system_instruction": {"parts": [{"text": system_prompt}]},
            "contents": [{"parts": [{"text": user_message}]}],
            "generationConfig": {"maxOutputTokens": max_tokens},
        })
        resp.raise_for_status()
        data = resp.json()

        content = (
            data["candidates"][0]["content"]["parts"][0]["text"]
        )
        usage = data.get("usageMetadata", {})
        finish = data["candidates"][0].get("finishReason", "STOP")
        stop_reason = (
            "max_tokens" if finish == "MAX_TOKENS"
            else "end_turn" if finish == "STOP"
            else finish.lower()
        )

        return APIResult(
            content=content,
            input_tokens=usage.get("promptTokenCount", 0),
            output_tokens=usage.get("candidatesTokenCount", 0),
            stop_reason=stop_reason,
        )
