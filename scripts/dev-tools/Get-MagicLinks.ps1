param(
    [string]$BaseUrl = "http://localhost:5010",
    [string]$Email,
    [switch]$Clear,
    [string]$Secret = "acme-local-seeded-session"
)

$uri = [Uri]$BaseUrl
$loopbackHosts = @("localhost", "127.0.0.1", "::1")
if ($loopbackHosts -notcontains $uri.Host) {
    throw "BaseUrl must remain loopback-only. Received host '$($uri.Host)'."
}

$query = [System.Web.HttpUtility]::ParseQueryString([string]::Empty)
if ($Email) {
    $query["email"] = $Email
}
if ($Clear.IsPresent) {
    $query["clear"] = "true"
}

$builder = [System.UriBuilder]($uri.ToString().TrimEnd("/") + "/test-auth/magic-links")
$builder.Query = $query.ToString()

$headers = @{
    "X-Test-Auth-Secret" = $Secret
}

Invoke-RestMethod -Method Get -Uri $builder.Uri -Headers $headers
