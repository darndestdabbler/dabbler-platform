param(
    [Parameter(Mandatory = $true)]
    [string]$ProjectName,

    [Parameter(Mandatory = $true)]
    [string]$AppName,

    [Parameter(Mandatory = $true)]
    [string]$SessionSetName
)

$replacements = @{
    '{{PROJECT_NAME}}' = $ProjectName
    '{{APP_NAME}}' = $AppName
    '{{SESSION_SET_NAME}}' = $SessionSetName
    '{{REPO_ROOT}}' = (Get-Location).Path
}

Get-ChildItem -Recurse -File | ForEach-Object {
    $content = Get-Content -Raw -LiteralPath $_.FullName
    $updated = $content

    foreach ($entry in $replacements.GetEnumerator()) {
        $updated = $updated.Replace($entry.Key, $entry.Value)
    }

    if ($updated -ne $content) {
        Set-Content -LiteralPath $_.FullName -Value $updated -NoNewline
    }
}
