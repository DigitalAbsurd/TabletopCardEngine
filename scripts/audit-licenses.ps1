<#
.SYNOPSIS
    Lists the NuGet packages the solution references directly, with the license each nuspec declares.

.DESCRIPTION
    Reads obj/project.assets.json of every project under src/ and tests/ (run `dotnet restore` first), looks
    each package's nuspec up in the NuGet package folders the assets file names, and prints five
    Markdown tables in the shape THIRD-PARTY-NOTICES.md uses:

    - "TabletopCardEngine": the packages the library references.
    - "TabletopCardEngine.OpenTelemetry": what the instrumentation package references on top of the library.
    - "TabletopCardEngine.Sqlite": what the SQLite package references on top of the library.
    - "TabletopCardEngine.Demo": what the deployed demo site references on top of the library.
    - "Test and development only": what the test projects reference on top of both.

    Only direct references are listed: transitive packages, and the ones the SDK adds implicitly
    (autoReferenced, such as Microsoft.NET.ILLink.Tasks), are left out. ThirdPartyNoticesTests
    requires exactly these packages to have a row in the notices with the same license, so paste
    the tables over the ones in THIRD-PARTY-NOTICES.md.

    The script exits 1 when any resolved package, transitive ones included, declares a license
    outside the permissive allowlist below, the same allowlist ThirdPartyNoticesTests enforces. It is a tool for regenerating the tables
    by hand after a dependency change; it is NOT part of the build or CI.

.EXAMPLE
    pwsh ./scripts/audit-licenses.ps1
#>
[CmdletBinding()]
param()

$ErrorActionPreference = 'Stop'

$repoRoot = Split-Path -Parent $PSScriptRoot
$sections = [ordered]@{
    'TabletopCardEngine' = 'TabletopCardEngine'
    'TabletopCardEngine.OpenTelemetry' = 'TabletopCardEngine.OpenTelemetry'
    'TabletopCardEngine.AspNetCore' = 'TabletopCardEngine.AspNetCore'
    'TabletopCardEngine.Sqlite' = 'TabletopCardEngine.Sqlite'
    'TabletopCardEngine.PostgreSql' = 'TabletopCardEngine.PostgreSql'
    'TabletopCardEngine.SqlServer' = 'TabletopCardEngine.SqlServer'
    'TabletopCardEngine.InMemory' = 'TabletopCardEngine.InMemory'
    'TabletopCardEngine.Demo' = 'TabletopCardEngine.Demo'
    'TabletopCardEngine.Server' = 'TabletopCardEngine.Server'
}
$testSection = 'Test and development only'

# SPDX expressions accepted as-is. The same as the test's _permissiveLicenses.
$permissiveExpressions = @(
    'MIT',
    'Apache-2.0',
    'BSD-2-Clause',
    'BSD-3-Clause',
    # OSI-approved PostgreSQL License, MIT/BSD-equivalent: permission plus notice (ADR-0003).
    'PostgreSQL'
)

# Packages that predate nuspec license expressions and only carry a URL.
$legacyLicenseUrls = @{
    # The Microsoft .NET Library License of the netstandard1.x-era System.* packages.
    'http://go.microsoft.com/fwlink/?LinkId=329770' = 'MS-.NET-Library'
    # The same packages' newer URL form, which points at the MIT-licensed dotnet/corefx repository.
    'https://github.com/dotnet/corefx/blob/master/LICENSE.TXT' = 'MIT'
    'https://raw.githubusercontent.com/dotnet/corefx/master/LICENSE.TXT' = 'MIT'
    # OneOf 2.1.155; the linked licence.md is the MIT license, (c) 2016 Harry McIntyre.
    'https://github.com/mcintyre321/OneOf/blob/master/licence.md' = 'MIT'
}

# Packages outside the allowlist that are accepted for exactly one package id when reached
# transitively (not referenced directly). See ADR-0003.
$acceptedExceptions = @{
    'Microsoft.Data.SqlClient.SNI.runtime' = 'file:LICENSE.txt'
}

function Get-PackageLicense([string[]] $packageFolders, [string] $path, [string] $id) {
    foreach ($folder in $packageFolders) {
        $nuspec = Join-Path $folder (Join-Path $path "$($id.ToLowerInvariant()).nuspec")
        if (Test-Path $nuspec) {
            $metadata = ([xml](Get-Content -Raw $nuspec)).package.metadata
            $license = $metadata.license
            if ($license -and $license.type -eq 'expression') {
                return $license.'#text'
            }
            if ($license -and $license.type -eq 'file') {
                return "file:$($license.'#text')"
            }
            $url = [string]$metadata.licenseUrl
            if ($legacyLicenseUrls.ContainsKey($url)) {
                return $legacyLicenseUrls[$url]
            }
            return "url:$url"
        }
    }
    throw "No nuspec found for $id at $path in: $($packageFolders -join ', ')"
}

$packages = @{}
$direct = @{}
foreach ($project in ('src', 'tests' | ForEach-Object { Get-ChildItem (Join-Path $repoRoot $_) -Recurse -Filter '*.csproj' }) |
        Where-Object { $_.FullName -notmatch '[\\/](bin|obj)[\\/]' }) {
    $assetsFile = Join-Path $project.DirectoryName 'obj/project.assets.json'
    if (-not (Test-Path $assetsFile)) {
        throw "Missing $assetsFile — run 'dotnet restore' first."
    }
    $assets = Get-Content -Raw $assetsFile | ConvertFrom-Json
    $packageFolders = @($assets.packageFolders.PSObject.Properties.Name)
    $section = if ($sections.Contains($project.BaseName)) { $sections[$project.BaseName] } else { $testSection }
    $rank = if ($section -eq $testSection) { $sections.Count } else { @($sections.Keys).IndexOf($project.BaseName) }

    # Every resolved package, transitive ones included, for the allowlist check.
    $resolvedById = @{}
    foreach ($library in $assets.libraries.PSObject.Properties) {
        if ($library.Value.type -ne 'package') {
            continue
        }
        $key = $library.Name
        $id, $version = $key -split '/'
        $resolvedById[$id] = $key
        if (-not $packages.ContainsKey($key)) {
            $packages[$key] = [pscustomobject]@{
                Id = $id
                Version = $version
                License = Get-PackageLicense $packageFolders $library.Value.path $id
            }
        }
    }

    # The packages the project references itself; the SDK's implicit ones are autoReferenced.
    foreach ($framework in $assets.project.frameworks.PSObject.Properties) {
        foreach ($dependency in $framework.Value.dependencies.PSObject.Properties) {
            if ($dependency.Value.autoReferenced -or -not $resolvedById.ContainsKey($dependency.Name)) {
                continue
            }
            $key = $resolvedById[$dependency.Name]
            # A package belongs to the first section that references it: library, then instrumentation, then demo, then tests.
            if (-not $direct.ContainsKey($key) -or $rank -lt $direct[$key]) {
                $direct[$key] = $rank
            }
        }
    }
}

function Write-Table([object[]] $rows) {
    '| Package | Version | License |'
    '| --- | --- | --- |'
    foreach ($row in $rows | Sort-Object Id, Version) {
        "| $($row.Id) | $($row.Version) | $($row.License) |"
    }
}

$titles = @($sections.Keys) + $testSection
for ($rank = 0; $rank -lt $titles.Count; $rank++) {
    if ($rank -gt 0) {
        ''
    }
    "### $($titles[$rank])"
    ''
    Write-Table @($direct.Keys | Where-Object { $direct[$_] -eq $rank } | ForEach-Object { $packages[$_] })
}

$accepted = $permissiveExpressions + @('MS-.NET-Library')
$direct = @($direct.Keys | ForEach-Object { $_ -split '/' | Select-Object -First 1 } | Get-Unique)
$rejected = @($packages.Values | Where-Object {
    if ($accepted -contains $_.License) {
        return $false
    }
    if ($direct -contains $_.Id) {
        return $true
    }
    if ($acceptedExceptions.ContainsKey($_.Id) -and $acceptedExceptions[$_.Id] -eq $_.License) {
        return $false
    }
    return $true
})
if ($rejected.Count -gt 0) {
    Write-Error ("Packages outside the license allowlist:`n" +
        (($rejected | ForEach-Object { "  $($_.Id) $($_.Version): $($_.License)" }) -join "`n"))
    exit 1
}
