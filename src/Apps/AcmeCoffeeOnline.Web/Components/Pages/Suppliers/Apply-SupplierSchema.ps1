# Apply-SupplierSchema.ps1
#
# Companion post-process to `dotnet new dabbler-crud-page --entity Supplier ...`.
#
# `dotnet new` cannot iterate over an arbitrary list of properties (its template engine
# is built around 1:1 substitution + a small set of generators; it has no per-row repeat
# construct). The `crud-scaffolding-spike` review's adjustment B5 explicitly allows a
# "schema-file-driven post-process" alongside the `dotnet new` invocation; this script
# is that post-process.
#
# What it does:
#   - Reads the .dabbler-entity.json schema (`-SchemaPath` parameter; defaults to
#     `Supplier.dabbler-entity.json` or `t1entity.dabbler-entity.json` in the current
#     directory).
#   - Walks `properties[]` and classifies each property as Text / Bool / Numeric / Date.
#   - Emits MudDataGrid column snippets (one per `isInGrid: true` property) into the
#     `@* DABBLER-SCAFFOLD:GRID-COLUMNS:* *@` region in `SupplierList.razor`.
#   - Emits MudForm field snippets — using `Value` + `ValueChanged` (NEVER `@bind-Value`,
#     per the platform's CRUD Form Conventions) — into the
#     `@* DABBLER-SCAFFOLD:FORM-FIELDS:* *@` region in `SupplierDetail.razor` (one per
#     `isInForm: true` property other than the primary key).
#   - Emits FilterProperty entries (one per `isFilterable: true` property) into the
#     `// DABBLER-SCAFFOLD:FILTER-PROPERTIES:*` region in `SupplierList.razor`.
#
# Idempotent: re-running replaces the previously emitted block in-place (matched by the
# BEGIN/END markers), so it is safe to re-run whenever the schema changes.
#
# Supported property types (schema `type` field, case-insensitive):
#   string | bool | int | int32 | int16 | short | long | int64 | decimal | money | double | float | DateTime
# Unsupported types — DateOnly, DateTimeOffset, Guid, enums, FK references — fall into
# the `Unknown` classification and emit an obvious hand-edit-required placeholder
# `<MudItem>` (a warning-coloured `MudText` naming the unsupported type). The slice
# still compiles; the developer refines by hand.
#
# Parameters:
#   -SchemaPath <path>   Optional. Defaults to `./Supplier.dabbler-entity.json` or
#                        `./t1entity.dabbler-entity.json` (whichever exists).
#   -ProjectRoot <path>  Optional. Defaults to the closest ancestor containing a *.csproj.
#                        The schema path is resolved relative to ProjectRoot when not absolute.

[CmdletBinding()]
param(
    [Parameter(Mandatory=$false)]
    [string]$SchemaPath,
    [Parameter(Mandatory=$false)]
    [string]$ProjectRoot
)

$ErrorActionPreference = 'Stop'

$entitySingular = 'Supplier'
$scriptDir = $PSScriptRoot

function Find-ProjectRoot([string]$start) {
    $dir = Get-Item -LiteralPath $start
    while ($null -ne $dir) {
        $csproj = Get-ChildItem -LiteralPath $dir.FullName -Filter '*.csproj' -File -ErrorAction SilentlyContinue | Select-Object -First 1
        if ($null -ne $csproj) { return $dir.FullName }
        $dir = $dir.Parent
    }
    return (Get-Location).Path
}

if (-not $ProjectRoot) { $ProjectRoot = Find-ProjectRoot $scriptDir }

# The schema path baked in at template-invocation time (via `dotnet new --schema <path>`).
# `dotnet new` substitutes the literal value into supplier.dabbler-entity.json; if the developer renamed
# the file or moved it, they can override at runtime via the -SchemaPath parameter.
$stampedSchemaPath = 'supplier.dabbler-entity.json'

if (-not $SchemaPath) {
    $cwd = (Get-Location).Path
    $candidates = @()
    if ($stampedSchemaPath) {
        $candidates += (Join-Path $cwd $stampedSchemaPath)
        $candidates += (Join-Path $ProjectRoot $stampedSchemaPath)
    }
    $candidates += @(
        (Join-Path $cwd "$entitySingular.dabbler-entity.json"),
        (Join-Path $cwd ($entitySingular.ToLowerInvariant() + '.dabbler-entity.json')),
        (Join-Path $ProjectRoot "$entitySingular.dabbler-entity.json"),
        (Join-Path $ProjectRoot ($entitySingular.ToLowerInvariant() + '.dabbler-entity.json'))
    )
    $SchemaPath = $candidates | Where-Object { Test-Path -LiteralPath $_ } | Select-Object -First 1
    if (-not $SchemaPath) {
        $tried = ($candidates -join "`n  ")
        throw "Schema file not found. Pass -SchemaPath <path>, or place the schema at one of:`n  $tried"
    }
}

if (-not (Test-Path -LiteralPath $SchemaPath)) {
    throw "Schema file not found at: $SchemaPath"
}

Write-Host "Apply-${entitySingular}Schema.ps1: reading $SchemaPath"
$schema = Get-Content -LiteralPath $SchemaPath -Raw | ConvertFrom-Json

if (-not $schema.properties) {
    throw "Schema at $SchemaPath has no 'properties' array."
}

# ── Helpers ─────────────────────────────────────────────────────────────────

function Get-Humanized([string]$name) {
    if ([string]::IsNullOrWhiteSpace($name)) { return $name }
    return ($name -creplace '([a-z0-9])([A-Z])', '$1 $2')
}

function Get-Kebab([string]$name) {
    return ((Get-Humanized $name) -replace '\s+', '-').ToLowerInvariant()
}

function Get-TypeClassification([string]$type) {
    # MVP-supported types only. Unknown types (DateOnly, DateTimeOffset, Guid,
    # enums, FK references, etc.) fall through to 'Unknown' so the emitter
    # produces an obviously-hand-edit-required fallback rather than broken code.
    switch -Regex ($type) {
        '^(string|str|text)$'                  { return 'Text' }
        '^(bool|boolean)$'                     { return 'Bool' }
        '^(int|int32|short|int16)$'            { return 'Integer' }
        '^(long|int64)$'                       { return 'Integer' }
        '^(decimal|double|float|money)$'       { return 'Decimal' }
        '^(datetime)$'                         { return 'Date' }
        default                                { return 'Unknown' }
    }
}

function Get-MudFieldType([string]$type) {
    switch -Regex ($type) {
        '^(int|int32|short|int16)$'         { return 'int' }
        '^(long|int64)$'                    { return 'long' }
        '^(decimal|money)$'                 { return 'decimal' }
        '^(double|float)$'                  { return 'double' }
        default                             { return $type }
    }
}

# ── Build per-region snippet collections ────────────────────────────────────

$gridSnippets = New-Object System.Collections.Generic.List[string]
$formSnippets = New-Object System.Collections.Generic.List[string]
$filterSnippets = New-Object System.Collections.Generic.List[string]

$pkName = if ($schema.primaryKey -and $schema.primaryKey.name) { [string]$schema.primaryKey.name } else { 'Id' }

foreach ($prop in $schema.properties) {
    $name = [string]$prop.name
    $type = [string]$prop.type
    $classification = Get-TypeClassification $type
    $label = Get-Humanized $name
    $kebab = Get-Kebab $name
    $isInGrid = [bool]$prop.isInGrid
    $isInForm = [bool]$prop.isInForm
    $isFilterable = [bool]$prop.isFilterable
    $isReadOnly = [bool]$prop.isReadOnly
    $isSortable = if ($null -ne $prop.PSObject.Properties['isSortable']) { [bool]$prop.isSortable } else { $true }
    $isPk = ($name -ieq $pkName)

    if ($isInGrid) {
        if ($isSortable) {
            $gridSnippets.Add("        <PropertyColumn Property=`"e => e.$name`" Title=`"$label`" />")
        } else {
            $gridSnippets.Add("        <PropertyColumn Property=`"e => e.$name`" Title=`"$label`" Sortable=`"false`" />")
        }
    }

    if ($isInForm -and -not $isPk) {
        switch ($classification) {
            'Text' {
                $formSnippets.Add(@"
            <MudItem xs="12" md="6">
                <MudTextField T="string"
                              Value="@_record.$name"
                              ValueChanged="@((string v) => { _record.$name = v; _session?.NotifyChanged(); })"
                              Label="$label"
                              Variant="Variant.Outlined"
                              Immediate="true"
                              ReadOnly="$($isReadOnly.ToString().ToLowerInvariant())"
                              data-testid="field-$kebab" />
            </MudItem>
"@)
            }
            'Bool' {
                $formSnippets.Add(@"
            <MudItem xs="12" md="6">
                <MudCheckBox T="bool"
                             Value="@_record.$name"
                             ValueChanged="@((bool v) => { _record.$name = v; _session?.NotifyChanged(); })"
                             Label="$label"
                             Color="Color.Primary"
                             ReadOnly="$($isReadOnly.ToString().ToLowerInvariant())"
                             data-testid="field-$kebab" />
            </MudItem>
"@)
            }
            'Integer' {
                $tArg = Get-MudFieldType $type
                $formSnippets.Add(@"
            <MudItem xs="12" md="6">
                <MudNumericField T="$tArg"
                                 Value="@_record.$name"
                                 ValueChanged="@(($tArg v) => { _record.$name = v; _session?.NotifyChanged(); })"
                                 Label="$label"
                                 Variant="Variant.Outlined"
                                 Immediate="true"
                                 ReadOnly="$($isReadOnly.ToString().ToLowerInvariant())"
                                 data-testid="field-$kebab" />
            </MudItem>
"@)
            }
            'Decimal' {
                $tArg = Get-MudFieldType $type
                $formSnippets.Add(@"
            <MudItem xs="12" md="6">
                <MudNumericField T="$tArg"
                                 Value="@_record.$name"
                                 ValueChanged="@(($tArg v) => { _record.$name = v; _session?.NotifyChanged(); })"
                                 Label="$label"
                                 Variant="Variant.Outlined"
                                 Immediate="true"
                                 Format="N2"
                                 ReadOnly="$($isReadOnly.ToString().ToLowerInvariant())"
                                 data-testid="field-$kebab" />
            </MudItem>
"@)
            }
            'Date' {
                # The `v ?? default` shape compiles for BOTH non-nullable `DateTime`
                # and nullable `DateTime?` DTO properties — for `DateTime`, clearing
                # produces `DateTime.MinValue`; for `DateTime?`, clearing produces a
                # boxed `MinValue` (NOT null). The schema does not currently encode
                # nullability so we cannot pick one semantic over the other; this
                # shape errs on the side of compile-safety. Hand-edit the assignment
                # to `_record.$name = v;` if the DTO property is nullable and you
                # want cleared dates to round-trip as null.
                $formSnippets.Add(@"
            <MudItem xs="12" md="6">
                <MudDatePicker Date="@_record.$name"
                               DateChanged="@((DateTime? v) => { _record.$name = v ?? default; _session?.NotifyChanged(); })"
                               Label="$label"
                               Variant="Variant.Outlined"
                               DateFormat="MM/dd/yyyy"
                               ReadOnly="$($isReadOnly.ToString().ToLowerInvariant())"
                               data-testid="field-$kebab" />
            </MudItem>
"@)
            }
            default {
                # Unknown — DateOnly, DateTimeOffset, Guid, enums, FK refs, etc.
                # Emit a placeholder MudItem the developer must hand-edit. The
                # snippet does NOT pretend to round-trip the value; it is an
                # obvious TODO marker that still compiles (no field interaction
                # with `_record.$name` from this scaffold).
                $formSnippets.Add(@"
            <MudItem xs="12" md="6" data-testid="field-$kebab">
                @* TODO: hand-edit — schema type '$type' is not auto-scaffolded. Add a MudBlazor field for _record.$name and call _session?.NotifyChanged() on change. *@
                <MudText Typo="Typo.body2" Color="Color.Warning">$label ($type) — hand-edit required</MudText>
            </MudItem>
"@)
            }
        }
    }

    if ($isFilterable) {
        switch ($classification) {
            'Text'    { $filterSnippets.Add("            new FilterProperty<${entitySingular}Dto>.Text { Property = e => (object?)e.$name, Label = `"$label`" },") }
            'Bool'    { $filterSnippets.Add("            new FilterProperty<${entitySingular}Dto>.Bool { Property = e => (object?)e.$name, Label = `"$label`" },") }
            'Integer' { $filterSnippets.Add("            new FilterProperty<${entitySingular}Dto>.Numeric { Property = e => (object?)e.$name, Label = `"$label`" },") }
            'Decimal' { $filterSnippets.Add("            new FilterProperty<${entitySingular}Dto>.Numeric { Property = e => (object?)e.$name, Label = `"$label`" },") }
            'Date'    { $filterSnippets.Add("            new FilterProperty<${entitySingular}Dto>.Date { Property = e => (object?)e.$name, Label = `"$label`" },") }
            default   { } # skip unknown types in the filter panel; hand-edit if needed
        }
    }
}

# ── Apply substitutions ─────────────────────────────────────────────────────

function Set-RegionContent([string]$content, [string]$beginMarker, [string]$endMarker, [string]$body) {
    # `[regex]::Escape` would over-escape the spaces; build the pattern by hand.
    $escapedBegin = [regex]::Escape($beginMarker)
    $escapedEnd = [regex]::Escape($endMarker)
    $pattern = "(?ms)" + $escapedBegin + ".*?" + $escapedEnd
    $replacement = $beginMarker + "`r`n" + $body + "`r`n" + (' ' * (Get-IndentFor $beginMarker)) + $endMarker
    if ([regex]::IsMatch($content, $pattern)) {
        return [regex]::Replace($content, $pattern, [System.Text.RegularExpressions.MatchEvaluator]{ param($m) $replacement })
    } else {
        Write-Warning "Region markers not found in file (skipped): $beginMarker"
        return $content
    }
}

function Get-IndentFor([string]$marker) {
    # The END marker is typically aligned with whatever indent the BEGIN marker has on its line.
    # We don't actually re-derive it from the file; the .razor templates use 8-space indent for
    # @* GRID-COLUMNS *@ markers (inside <Columns>), 12-space for @* FORM-FIELDS *@ markers (inside
    # <MudGrid>), and 12-space for the C# FILTER-PROPERTIES marker. Approximate via the marker text.
    if ($marker -like '*GRID-COLUMNS*')        { return 8 }
    if ($marker -like '*FORM-FIELDS*')         { return 12 }
    if ($marker -like '*FILTER-PROPERTIES*')   { return 12 }
    return 0
}

$listPath = Join-Path $scriptDir ("${entitySingular}List.razor")
$detailPath = Join-Path $scriptDir ("${entitySingular}Detail.razor")

if (-not (Test-Path -LiteralPath $listPath)) { throw "Expected file not found: $listPath" }
if (-not (Test-Path -LiteralPath $detailPath)) { throw "Expected file not found: $detailPath" }

$listContent = Get-Content -LiteralPath $listPath -Raw
$gridBody = if ($gridSnippets.Count -gt 0) { $gridSnippets -join "`r`n" } else { "        @* No grid columns — schema had no isInGrid properties. *@" }
$filterBody = if ($filterSnippets.Count -gt 0) { $filterSnippets -join "`r`n" } else { "            // No filter properties — schema had no isFilterable properties." }
$listContent = Set-RegionContent $listContent '@* DABBLER-SCAFFOLD:GRID-COLUMNS:BEGIN *@' '@* DABBLER-SCAFFOLD:GRID-COLUMNS:END *@' $gridBody
$listContent = Set-RegionContent $listContent '// DABBLER-SCAFFOLD:FILTER-PROPERTIES:BEGIN' '// DABBLER-SCAFFOLD:FILTER-PROPERTIES:END' $filterBody
Set-Content -LiteralPath $listPath -Value $listContent -NoNewline

$detailContent = Get-Content -LiteralPath $detailPath -Raw
$formBody = if ($formSnippets.Count -gt 0) { $formSnippets -join "`r`n" } else { "            @* No form fields — schema had no editable isInForm properties. *@" }
$detailContent = Set-RegionContent $detailContent '@* DABBLER-SCAFFOLD:FORM-FIELDS:BEGIN *@' '@* DABBLER-SCAFFOLD:FORM-FIELDS:END *@' $formBody
Set-Content -LiteralPath $detailPath -Value $detailContent -NoNewline

Write-Host "Apply-${entitySingular}Schema.ps1: wrote $($gridSnippets.Count) grid columns, $($formSnippets.Count) form fields, $($filterSnippets.Count) filter properties."
