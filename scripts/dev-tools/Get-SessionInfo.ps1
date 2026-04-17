param(
    [string]$BaseUrl = "http://localhost:5010",
    [string]$Secret = "acme-local-seeded-session"
)

$uri = [Uri]$BaseUrl
$loopbackHosts = @("localhost", "127.0.0.1", "::1")
if ($loopbackHosts -notcontains $uri.Host) {
    throw "BaseUrl must remain loopback-only. Received host '$($uri.Host)'."
}

$headers = @{
    "X-Test-Auth-Secret" = $Secret
}

Invoke-RestMethod -Method Get -Uri ($uri.ToString().TrimEnd("/") + "/test-auth/session-info") -Headers $headers
