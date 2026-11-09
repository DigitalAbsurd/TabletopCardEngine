<#
.SYNOPSIS
    Rebuilds the vendored ACPC fixed-limit PHH test fixture from the Zenodo edition of the PHH dataset.

.DESCRIPTION
    Produces tests/TabletopCardEngine.Tests/Resources/phh/acpc-fixedlimit.phhs.gz: two files of the
    Annual Computer Poker Competition logs, 3 000 heads-up and 3 000 three-player fixed-limit hands,
    packed into one gzipped .phhs file (TOML array of tables with numeric headers, the same layout the
    other pack-phh-*.ps1 scripts write).

    The Zenodo record holds a single 20 GB ZIP64 archive, and nothing of it is downloaded but what is
    needed. The script reads the last 64 KiB with an HTTP Range request to find the ZIP64 end of
    central directory record, fetches the central directory by range, finds the two entries by exact
    path, and fetches each one by range (local header, then data). Each entry is inflated and checked
    against the central directory (size, CRC-32) and against the SHA-256 pinned below; any mismatch
    stops the script. A server that answers a Range request with anything but 206 stops it too, and
    there is no fallback to another source.

    Hands are copied verbatim apart from the [N] header, which is renumbered across both files, and a
    `# acpc/<entry path>` comment above each file's first hand.

    Upstream: https://doi.org/10.5281/zenodo.10796885 (record 17136841, CC-BY-4.0).

    This script is committed for provenance only; it is NOT part of the build or CI. Run it by hand
    when the fixture needs to be regenerated, then commit the resulting .gz. Needs PowerShell 7.

.EXAMPLE
    pwsh ./scripts/pack-phh-acpc-fixture.ps1
#>
[CmdletBinding()]
param(
    [string] $Url = 'https://zenodo.org/api/records/17136841/files/poker-hand-histories.zip/content',

    [string] $OutputPath = (Join-Path $PSScriptRoot '../tests/TabletopCardEngine.Tests/Resources/phh/acpc-fixedlimit.phhs.gz')
)

$ErrorActionPreference = 'Stop'
Set-StrictMode -Version Latest

$prefix = 'data/annual-computer-poker-competition/competitions/'
# Order matters: the heads-up file comes first in the fixture.
$entries = @(
    [pscustomobject]@{
        Path   = $prefix + '2014/logs/2pl_logs/2014_2pl/2pl.652.Cleverpiggy.0.0.phhs'
        Sha256 = '27ae7ec6034e313e794588a6e02ca014a4e972f3d1365b26f775c664569c778d'
        Hands  = 3000
    },
    [pscustomobject]@{
        Path   = $prefix + '2014/logs/3pl_logs/2014_3pl/3pl.HITSZ_CS_14.Hyperborean_iro.SmooCT.0.0.phhs'
        Sha256 = 'fd06609f5a99f249373091cb2fe86ecba78b7183605b3628b9fae61fe8112170'
        Hands  = 3000
    }
)

$http = [System.Net.Http.HttpClient]::new()
$http.Timeout = [TimeSpan]::FromMinutes(5)
# Zenodo answers 403 to a request with no User-Agent.
$http.DefaultRequestHeaders.UserAgent.ParseAdd('TabletopCardEngine-fixture-packer')

# Sends a GET for bytes [from, to] of the archive and insists on a 206.
function Send-Range([long] $from, [long] $to) {
    $request = [System.Net.Http.HttpRequestMessage]::new([System.Net.Http.HttpMethod]::Get, $Url)
    $request.Headers.Range = [System.Net.Http.Headers.RangeHeaderValue]::new($from, $to)
    $response = $http.Send($request, [System.Net.Http.HttpCompletionOption]::ResponseHeadersRead)
    if ([int]$response.StatusCode -ne 206) {
        $status = [int]$response.StatusCode
        $response.Dispose()
        throw "Range $from-$to was answered with $status instead of 206; refusing to download the whole archive."
    }
    return $response
}

function Get-Range([long] $from, [long] $to) {
    $response = Send-Range $from $to
    try {
        $buffer = [System.IO.MemoryStream]::new()
        $response.Content.ReadAsStream().CopyTo($buffer)
        $bytes = $buffer.ToArray()
        if ($bytes.Length -ne ($to - $from + 1)) { throw "Range $from-$to returned $($bytes.Length) bytes" }
        return , $bytes
    }
    finally { $response.Dispose() }
}

function Get-Crc32([byte[]] $bytes) {
    $table = [uint64[]]::new(256)
    for ($n = 0; $n -lt 256; $n++) {
        [uint64] $c = $n
        for ($k = 0; $k -lt 8; $k++) {
            if ($c -band 1) { $c = [uint64]3988292384 -bxor ($c -shr 1) } else { $c = $c -shr 1 }
        }
        $table[$n] = $c
    }
    [uint64] $crc = [uint64]4294967295
    foreach ($b in $bytes) { $crc = $table[($crc -bxor $b) -band 255] -bxor ($crc -shr 8) }
    return [uint64]($crc -bxor [uint64]4294967295)
}

try {
    # 1. The archive's length, from the Content-Range of a one-byte range.
    $probe = Send-Range 0 0
    $total = $probe.Content.Headers.ContentRange.Length
    $probe.Dispose()
    Write-Host "Archive: $total bytes"

    # 2. The ZIP64 end of central directory record, in the last 64 KiB.
    $tailLength = [Math]::Min(65536L, $total)
    $tail = Get-Range ($total - $tailLength) ($total - 1)
    $eocd = -1
    for ($i = $tail.Length - 56; $i -ge 0; $i--) {
        if ($tail[$i] -eq 0x50 -and $tail[$i + 1] -eq 0x4B -and $tail[$i + 2] -eq 0x06 -and $tail[$i + 3] -eq 0x06) { $eocd = $i; break }
    }
    if ($eocd -lt 0) { throw 'ZIP64 end of central directory record (PK 06 06) not found in the last 64 KiB.' }
    $entryCount = [BitConverter]::ToUInt64($tail, $eocd + 32)
    $cdSize = [BitConverter]::ToUInt64($tail, $eocd + 40)
    $cdOffset = [BitConverter]::ToUInt64($tail, $eocd + 48)
    Write-Host "ZIP64: $entryCount entries, central directory $cdSize bytes at offset $cdOffset"

    # 3. The central directory, walked for the two entries.
    $cd = Get-Range ([long]$cdOffset) ([long]($cdOffset + $cdSize - 1))
    $wanted = @{}
    foreach ($entry in $entries) { $wanted[$entry.Path] = $entry }
    $found = @{}
    $pos = 0
    while ($pos + 46 -le $cd.Length -and $found.Count -lt $entries.Count) {
        if ([BitConverter]::ToUInt32($cd, $pos) -ne 0x02014B50) { throw "Bad central directory signature at $pos" }
        $method = [BitConverter]::ToUInt16($cd, $pos + 10)
        $crc = [BitConverter]::ToUInt32($cd, $pos + 16)
        [uint64] $compressed = [BitConverter]::ToUInt32($cd, $pos + 20)
        [uint64] $uncompressed = [BitConverter]::ToUInt32($cd, $pos + 24)
        $nameLength = [BitConverter]::ToUInt16($cd, $pos + 28)
        $extraLength = [BitConverter]::ToUInt16($cd, $pos + 30)
        $commentLength = [BitConverter]::ToUInt16($cd, $pos + 32)
        [uint64] $localOffset = [BitConverter]::ToUInt32($cd, $pos + 42)
        $name = [System.Text.Encoding]::UTF8.GetString($cd, $pos + 46, $nameLength)

        if ($wanted.ContainsKey($name)) {
            # ZIP64 extra field (id 0x0001): only the fields that are 0xFFFFFFFF are present, in this order.
            $extraStart = $pos + 46 + $nameLength
            $e = $extraStart
            while ($e + 4 -le $extraStart + $extraLength) {
                $id = [BitConverter]::ToUInt16($cd, $e)
                $size = [BitConverter]::ToUInt16($cd, $e + 2)
                if ($id -eq 0x0001) {
                    $f = $e + 4
                    if ($uncompressed -eq [uint64]4294967295) { $uncompressed = [BitConverter]::ToUInt64($cd, $f); $f += 8 }
                    if ($compressed -eq [uint64]4294967295) { $compressed = [BitConverter]::ToUInt64($cd, $f); $f += 8 }
                    if ($localOffset -eq [uint64]4294967295) { $localOffset = [BitConverter]::ToUInt64($cd, $f) }
                }
                $e += 4 + $size
            }
            $found[$name] = [pscustomobject]@{
                Method = $method; Crc = $crc; Compressed = $compressed; Uncompressed = $uncompressed; LocalOffset = $localOffset
            }
        }
        $pos += 46 + $nameLength + $extraLength + $commentLength
    }
    foreach ($entry in $entries) {
        if (-not $found.ContainsKey($entry.Path)) { throw "Entry not found in the central directory: $($entry.Path)" }
    }

    # 4. Fetch, inflate and verify each entry, then pack its hands.
    $outputFull = [System.IO.Path]::GetFullPath($OutputPath)
    New-Item -ItemType Directory -Force -Path ([System.IO.Path]::GetDirectoryName($outputFull)) | Out-Null
    $handCount = 0
    $fileStream = [System.IO.File]::Create($outputFull)
    try {
        $gzipStream = [System.IO.Compression.GZipStream]::new($fileStream, [System.IO.Compression.CompressionLevel]::Optimal)
        try {
            $writer = [System.IO.StreamWriter]::new($gzipStream, [System.Text.UTF8Encoding]::new($false))
            try {
                $writer.NewLine = "`n"
                $writer.WriteLine('# Poker hand histories in PHH format: two files of the Annual Computer Poker Competition logs (2014),')
                $writer.WriteLine('# fixed-limit hold''em, from "A Dataset of Poker Hand Histories" (the Zenodo edition of the PHH dataset).')
                $writer.WriteLine('# Source: https://zenodo.org/records/17136841, DOI 10.5281/zenodo.10796885, licence CC-BY-4.0.')
                $writer.WriteLine('# Creators: Universal, Open, Free, and Transparent Computer Poker Research Group.')
                $writer.WriteLine('# Two files extracted, records renumbered. See THIRD-PARTY-NOTICES.md and Resources/phh/README.md.')
                $writer.WriteLine('# Generated by scripts/pack-phh-acpc-fixture.ps1 - do not edit by hand.')

                foreach ($entry in $entries) {
                    $meta = $found[$entry.Path]
                    if ($meta.Method -ne 8) { throw "$($entry.Path) is not deflated (method $($meta.Method))" }

                    $header = Get-Range ([long]$meta.LocalOffset) ([long]($meta.LocalOffset + 29))
                    if ([BitConverter]::ToUInt32($header, 0) -ne 0x04034B50) { throw "Bad local header signature for $($entry.Path)" }
                    $dataStart = [long]$meta.LocalOffset + 30 + [BitConverter]::ToUInt16($header, 26) + [BitConverter]::ToUInt16($header, 28)
                    $deflated = Get-Range $dataStart ($dataStart + [long]$meta.Compressed - 1)

                    $inflater = [System.IO.Compression.DeflateStream]::new([System.IO.MemoryStream]::new($deflated), [System.IO.Compression.CompressionMode]::Decompress)
                    $output = [System.IO.MemoryStream]::new()
                    $inflater.CopyTo($output)
                    $bytes = $output.ToArray()

                    if ([uint64]$bytes.Length -ne $meta.Uncompressed) { throw "$($entry.Path): inflated $($bytes.Length) bytes, expected $($meta.Uncompressed)" }
                    $actualCrc = Get-Crc32 $bytes
                    if ($actualCrc -ne $meta.Crc) { throw "$($entry.Path): CRC-32 $($actualCrc.ToString('x8')) differs from the central directory's $($meta.Crc.ToString('x8'))" }
                    $sha = [Convert]::ToHexString([System.Security.Cryptography.SHA256]::HashData($bytes)).ToLowerInvariant()
                    if ($sha -ne $entry.Sha256) { throw "$($entry.Path): SHA-256 $sha differs from the pinned $($entry.Sha256)" }
                    Write-Host "SHA-256 $sha  $($entry.Path)  ($($bytes.Length) bytes, CRC-32 ok)"

                    $text = [System.Text.Encoding]::UTF8.GetString($bytes) -replace "`r`n", "`n"
                    $records = @([regex]::Split($text, '(?m)^\[\d+\][ \t]*\n') | Where-Object { $_.Trim() })
                    if ($records.Count -ne $entry.Hands) { throw "$($entry.Path): $($records.Count) hands, expected $($entry.Hands)" }

                    $first = $true
                    foreach ($record in $records) {
                        $handCount++
                        $writer.WriteLine()
                        if ($first) { $writer.WriteLine("# acpc/$($entry.Path)"); $first = $false }
                        $writer.WriteLine("[$handCount]")
                        $writer.WriteLine($record.Trim())
                    }
                }
            }
            finally { $writer.Dispose() }
        }
        finally { $gzipStream.Dispose() }
    }
    finally { $fileStream.Dispose() }

    $sizeKb = [math]::Round((Get-Item $outputFull).Length / 1KB, 1)
    Write-Host "Packed $handCount hands into $outputFull ($sizeKb KB)"
}
finally {
    $http.Dispose()
}
