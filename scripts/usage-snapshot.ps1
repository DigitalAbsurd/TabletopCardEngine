<#
.SYNOPSIS
    Captures a snapshot of the package consumption numbers from nuget.org, Docker Hub and GitHub.

.DESCRIPTION
    A tool run by hand to collect the current usage statistics for the project.
    It is not part of the build or CI. The snapshot captures:

    - NuGet.org: Total downloads per package and version-level breakdowns.
    - Docker Hub: Pull counts and star counts.
    - GitHub: Stars, forks, watchers, traffic (views and clones), referrers, paths, and external issues/PRs.

    The counts are not people. On 2026-10-31 every package, including mutually exclusive providers
    (Sqlite, PostgreSql, SqlServer, InMemory), had about the same downloads per version, and
    pre-releases replaced within a day still had about 55 each. That is the signature of nuget.org
    mirrors and security scanners. The release workflow's pack also restores the package validation
    baseline from nuget.org, and ContainerImageTests pulls the 1.0.0 images.

    Prints one JSON object to stdout. With -OutFile, also appends that object as one compressed line
    to the file, creating it when missing, so repeated runs build a JSON Lines history.

.EXAMPLE
    pwsh ./scripts/usage-snapshot.ps1
    pwsh ./scripts/usage-snapshot.ps1 -OutFile snapshot-history.jsonl
    pwsh ./scripts/usage-snapshot.ps1 -Repository "MyOrg/MyRepo" -DockerRepository "myorg/myimage"
#>
[CmdletBinding()]
param(
    [string]$Repository = 'DigitalAbsurd/TabletopCardEngine',
    [string]$DockerRepository = 'digitalabsurd/tabletopcardengine',
    [string]$OutFile
)

$ErrorActionPreference = 'Stop'

$repoRoot = Split-Path -Parent $PSScriptRoot

# Read package IDs from TabletopCardEngine.Packages.slnf
$slnfPath = Join-Path $repoRoot 'TabletopCardEngine.Packages.slnf'
$slnf = Get-Content -Raw $slnfPath | ConvertFrom-Json
$packageIds = @($slnf.solution.projects | ForEach-Object {
    $path = $_
    # Extract the project name from the path (e.g., "src\TabletopCardEngine\TabletopCardEngine.csproj" -> "TabletopCardEngine")
    $fileName = Split-Path -Leaf $path
    $fileName -replace '\.csproj$', ''
})

# Get NuGet data
$nugetData = @()
foreach ($packageId in $packageIds) {
    try {
        # First, get the service index to find the search endpoint
        $indexResponse = Invoke-RestMethod -Uri 'https://api.nuget.org/v3/index.json' -ErrorAction Stop
        $searchEndpoint = ($indexResponse.resources | Where-Object { $_.'@type' -eq 'SearchQueryService' })[0].'@id'

        # Query the package
        $searchUrl = "$($searchEndpoint)?q=packageid:$packageId&prerelease=true&semVerLevel=2.0.0"
        $packageResponse = Invoke-RestMethod -Uri $searchUrl -ErrorAction Stop

        if ($packageResponse.data -and $packageResponse.data.Count -gt 0) {
            $package = $packageResponse.data[0]
            $versions = @($package.versions | ForEach-Object {
                [PSCustomObject]@{
                    version = $_.version
                    downloads = $_.downloads
                }
            })

            $nugetData += [PSCustomObject]@{
                id = $packageId
                totalDownloads = $package.totalDownloads
                versions = $versions
            }
        }
    }
    catch {
        Write-Error "Failed to fetch NuGet data for $packageId from $searchUrl : $_"
        exit 1
    }
}

# Get Docker Hub data
$dockerData = $null
try {
    $dockerRepoUrl = "https://hub.docker.com/v2/repositories/$DockerRepository/"
    $repoResponse = Invoke-RestMethod -Uri $dockerRepoUrl -ErrorAction Stop

    $tagsUrl = "https://hub.docker.com/v2/repositories/$DockerRepository/tags?page_size=100"
    $tagsResponse = Invoke-RestMethod -Uri $tagsUrl -ErrorAction Stop

    $tags = @($tagsResponse.results | ForEach-Object {
        [PSCustomObject]@{
            name = $_.name
            lastUpdated = $_.last_updated
            lastPulled = $_.tag_last_pulled
        }
    })

    $dockerData = [PSCustomObject]@{
        repository = $DockerRepository
        pullCount = $repoResponse.pull_count
        starCount = $repoResponse.star_count
        tags = $tags
    }
}
catch {
    Write-Error "Failed to fetch Docker Hub data from $dockerRepoUrl : $_"
    exit 1
}

# Get GitHub data
$githubData = $null
try {
    # Test authentication
    $authCheck = gh auth status *>&1
    if ($LASTEXITCODE -ne 0) {
        Write-Error "GitHub authentication required. Run 'gh auth login' first."
        exit 1
    }

    $owner, $name = $Repository -split '/'

    # Get basic repo stats
    $repoInfo = gh api repos/$Repository --jq '{stargazers_count, forks_count, subscribers_count}' | ConvertFrom-Json

    # Get traffic views
    $viewsData = $null
    try {
        $viewsData = gh api repos/$Repository/traffic/views --jq '{count, uniques}' | ConvertFrom-Json
    }
    catch {
        if ($_ -match '403') {
            Write-Error "GitHub traffic endpoints require push access. Verify gh auth has appropriate permissions."
            exit 1
        }
        throw $_
    }

    # Get traffic clones
    $clonesData = gh api repos/$Repository/traffic/clones --jq '{count, uniques}' | ConvertFrom-Json

    # Get popular referrers
    $referrersJson = gh api repos/$Repository/traffic/popular/referrers
    $referrersArray = @($referrersJson | ConvertFrom-Json)
    if ($referrersArray.Count -eq 0) {
        $referrers = @()
    }
    elseif ($referrersArray.Count -eq 1 -and $referrersArray -isnot [array]) {
        $referrers = @(@{
            referrer = $referrersArray.referrer
            count = $referrersArray.count
            uniques = $referrersArray.uniques
        })
    }
    else {
        $referrers = @($referrersArray | ForEach-Object {
            @{
                referrer = $_.referrer
                count = $_.count
                uniques = $_.uniques
            }
        })
    }

    # Get popular paths
    $pathsJson = gh api repos/$Repository/traffic/popular/paths
    $pathsArray = @($pathsJson | ConvertFrom-Json)
    if ($pathsArray.Count -eq 0) {
        $paths = @()
    }
    elseif ($pathsArray.Count -eq 1 -and $pathsArray -isnot [array]) {
        $paths = @(@{
            path = $pathsArray.path
            count = $pathsArray.count
            uniques = $pathsArray.uniques
        })
    }
    else {
        $paths = @($pathsArray | ForEach-Object {
            @{
                path = $_.path
                count = $_.count
                uniques = $_.uniques
            }
        })
    }

    # Get issues and PRs
    $allIssuesJson = @(gh api repos/$Repository/issues?state=all --paginate --slurp)
    $allIssuesData = $allIssuesJson | ConvertFrom-Json
    $filteredIssues = @($allIssuesData | Where-Object { $_.author_association -ne 'OWNER' -and $_.author_association -ne 'MEMBER' -and $_.author_association -ne 'COLLABORATOR' })
    $outsideIssues = @($filteredIssues | Where-Object { -not $_.pull_request })
    $outsidePullRequests = @($filteredIssues | Where-Object { $_.pull_request })

    # Get discussions (may be null if discussions are disabled)
    $discussions = $null
    try {
        $discussionQuery = 'query{repository(owner:"' + $owner + '",name:"' + $name + '"){discussions{totalCount}}}'
        $discussionResponse = gh api graphql -f query=$discussionQuery | ConvertFrom-Json
        $discussions = $discussionResponse.data.repository.discussions.totalCount
    }
    catch {
        # Discussions may be disabled or unavailable
    }

    $githubData = [PSCustomObject]@{
        repository = $Repository
        stars = $repoInfo.stargazers_count
        forks = $repoInfo.forks_count
        watchers = $repoInfo.subscribers_count
        views = [PSCustomObject]@{
            count = $viewsData.count
            uniques = $viewsData.uniques
        }
        clones = [PSCustomObject]@{
            count = $clonesData.count
            uniques = $clonesData.uniques
        }
        referrers = @($referrers)
        paths = @($paths)
        outsideIssues = $outsideIssues.Count
        outsidePullRequests = $outsidePullRequests.Count
        discussions = $discussions
    }
}
catch {
    Write-Error "Failed to fetch GitHub data: $_"
    exit 1
}

# Build the output object
$output = [PSCustomObject]@{
    takenAt = ([DateTime]::UtcNow).ToString('o')
    nuget = @($nugetData)
    docker = $dockerData
    github = $githubData
}

# Convert to JSON and output
$json = $output | ConvertTo-Json -Depth 10

# Print to stdout
Write-Output $json

# Append to file if specified
if ($OutFile) {
    $jsonCompressed = $output | ConvertTo-Json -Compress -Depth 10
    Add-Content -Path $OutFile -Value $jsonCompressed
}
