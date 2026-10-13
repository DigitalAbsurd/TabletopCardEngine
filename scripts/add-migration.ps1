<#
.SYNOPSIS
    Adds a new migration to every provider package at once.

.DESCRIPTION
    Scaffolds a migration named `$Name` for each database provider via `dotnet ef migrations add`,
    then edits the new migration file to declare the class as `internal` instead of `public`.
    The script fails on the first provider that fails, so the migration sets are left in a
    consistent state or not started.

    Changing the schema version is a decision, not a consequence: the script reminds to bump
    `HoldemSchema.Version` and `EngineVersion` as docs/persistence.md requires. It does not
    edit them.

.EXAMPLE
    pwsh ./scripts/add-migration.ps1 -Name AddUserColumn
#>
[CmdletBinding()]
param(
    [Parameter(Mandatory)]
    [ValidatePattern('^[A-Z][A-Za-z0-9]+$')]
    [string] $Name
)

$ErrorActionPreference = 'Stop'
Set-StrictMode -Version Latest

$repoRoot = Split-Path -Parent $PSScriptRoot

# Providers that ship migrations. Add one row per new provider package, with a comment pointing here.
$providers = @(
    @{ Project = 'src/TabletopCardEngine.Sqlite' }
    @{ Project = 'src/TabletopCardEngine.PostgreSql' }
    @{ Project = 'src/TabletopCardEngine.SqlServer' }
)

# Ensure dotnet-ef is available.
Write-Host "Restoring tools ..."
dotnet tool restore
if ($LASTEXITCODE -ne 0) { throw "dotnet tool restore failed with exit code $LASTEXITCODE" }

# Add the migration for each provider.
foreach ($provider in $providers) {
    $project = Join-Path $repoRoot $provider.Project
    if (-not (Test-Path $project)) {
        Write-Error "Project not found: $project"
        exit 1
    }

    Write-Host "Adding migration ''$Name'' to $($provider.Project) ..."
    dotnet ef migrations add $Name `
        --project $project `
        --startup-project $project `
        --context HoldemDbContext `
        --output-dir Migrations/Holdem
    if ($LASTEXITCODE -ne 0) { throw "dotnet ef migrations add failed for $($provider.Project) with exit code $LASTEXITCODE" }

    # Find the new migration file (the one ending in _$Name.cs, not .Designer.cs).
    $migrationDir = Join-Path $project 'Migrations/Holdem'
    $migrationFile = Get-ChildItem -Path $migrationDir -Filter "*_$Name.cs" -ErrorAction SilentlyContinue | Select-Object -First 1
    if (-not $migrationFile) {
        throw "Migration file not found for $Name in $migrationDir"
    }

    # Replace 'public partial class' with 'internal partial class'.
    $content = Get-Content -Path $migrationFile.FullName -Raw
    $newContent = $content -replace 'public partial class', 'internal partial class'
    [System.IO.File]::WriteAllText($migrationFile.FullName, $newContent, [System.Text.UTF8Encoding]::new($true))
    Write-Host "Updated migration class to 'internal' in $($migrationFile.Name)"
}

Write-Host ""
Write-Host "Reminder: if this migration changes the database schema version, bump HoldemSchema.Version"
Write-Host "in src/TabletopCardEngine/Holdem/Data/HoldemSchema.cs and EngineVersion in Directory.Build.props as"
Write-Host "required by docs/persistence.md"