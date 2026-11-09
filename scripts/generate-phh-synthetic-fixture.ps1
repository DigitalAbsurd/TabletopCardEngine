<#
.SYNOPSIS
    Regenerates the synthetic pot-limit and fixed-limit PHH test fixtures with pokerkit.

.DESCRIPTION
    Produces tests/TabletopCardEngine.Tests/Resources/phh/pokerkit-potlimit.phhs.gz and
    pokerkit-fixedlimit.phhs.gz: 2 000 hands each, played by scripts/phh-synthetic/generate.py with
    pokerkit 0.7.7, an independent MIT implementation of the rules. PHH has no pot-limit hold'em data
    at all, and the real fixed-limit corpora never go all in, so these hands are generated rather
    than vendored.

    The generator runs in `docker run --rm` on a python:3.12-slim image pinned by digest, which
    installs pokerkit==0.7.7 with --no-cache-dir; nothing is installed on the host. The generator's
    folder is mounted read-only, and its output folder is a fresh temporary directory. The seeds are
    fixed below, so a run reproduces the committed files byte for byte. The generator prints the
    hand counts per seat count, the hands with an all-in and, for fixed-limit, the hands tagged
    `_tce_divergence = 'incomplete-all-in'`.

    This script is committed for provenance only; it is NOT part of the build or CI. Run it by hand
    when the fixtures need to be regenerated, then commit the resulting .gz files. Needs PowerShell 7
    and Docker.

.EXAMPLE
    pwsh ./scripts/generate-phh-synthetic-fixture.ps1
#>
[CmdletBinding()]
param(
    [string] $OutputDirectory = (Join-Path $PSScriptRoot '../tests/TabletopCardEngine.Tests/Resources/phh')
)

$ErrorActionPreference = 'Stop'
Set-StrictMode -Version Latest

$image = 'python:3.12-slim@sha256:5e2bdb1b0d542d5039cffb8181c65930093949194a65c365dfb1b0fe13a4fa5b'
$pokerkit = 'pokerkit==0.7.7'
$hands = 2000
$corpora = @(
    [pscustomobject]@{ Structure = 'pot-limit'; Seed = 657; File = 'pokerkit-potlimit.phhs' },
    [pscustomobject]@{ Structure = 'fixed-limit'; Seed = 658; File = 'pokerkit-fixedlimit.phhs' }
)

$generatorDirectory = [System.IO.Path]::GetFullPath((Join-Path $PSScriptRoot 'phh-synthetic'))
$work = Join-Path ([System.IO.Path]::GetTempPath()) ("tce-phh-synthetic-" + [Guid]::NewGuid().ToString('N'))
New-Item -ItemType Directory -Path $work | Out-Null

try {
    $commands = @("pip install --quiet --no-cache-dir --disable-pip-version-check --root-user-action=ignore $pokerkit")
    foreach ($corpus in $corpora) {
        $commands += "python /gen/generate.py --structure $($corpus.Structure) --hands $hands --seed $($corpus.Seed) --out /out/$($corpus.File)"
    }
    docker run --rm `
        -v "${generatorDirectory}:/gen:ro" `
        -v "${work}:/out" `
        $image sh -ec ($commands -join ' && ')
    if ($LASTEXITCODE -ne 0) { throw "The generator failed with exit code $LASTEXITCODE." }

    $outputFull = [System.IO.Path]::GetFullPath($OutputDirectory)
    New-Item -ItemType Directory -Force -Path $outputFull | Out-Null
    foreach ($corpus in $corpora) {
        $source = Join-Path $work $corpus.File
        $target = Join-Path $outputFull "$($corpus.File).gz"
        $bytes = [System.IO.File]::ReadAllBytes($source)
        $fileStream = [System.IO.File]::Create($target)
        try {
            $gzipStream = [System.IO.Compression.GZipStream]::new($fileStream, [System.IO.Compression.CompressionLevel]::Optimal)
            try { $gzipStream.Write($bytes, 0, $bytes.Length) }
            finally { $gzipStream.Dispose() }
        }
        finally { $fileStream.Dispose() }

        $sha = (Get-FileHash -Algorithm SHA256 $target).Hash.ToLowerInvariant()
        $sizeKb = [math]::Round((Get-Item $target).Length / 1KB, 1)
        Write-Host "SHA-256 $sha  $target ($sizeKb KB)"
    }
}
finally {
    Remove-Item -Recurse -Force $work
}
