"""Simple token-bucket rate limiter per provider."""

import time
import threading


class RateLimiter:
    def __init__(self, requests_per_minute: int,
                 tokens_per_minute: int):
        self.rpm = requests_per_minute
        self.tpm = tokens_per_minute
        self._request_times: list[float] = []
        self._lock = threading.Lock()

    def wait(self):
        """Block until a request slot is available."""
        with self._lock:
            now = time.time()
            window_start = now - 60.0

            # Purge old entries
            self._request_times = [
                t for t in self._request_times if t > window_start
            ]

            if len(self._request_times) >= self.rpm:
                # Wait until the oldest request exits the window
                sleep_until = self._request_times[0] + 60.0
                sleep_duration = sleep_until - now
                if sleep_duration > 0:
                    time.sleep(sleep_duration)

            self._request_times.append(time.time())
