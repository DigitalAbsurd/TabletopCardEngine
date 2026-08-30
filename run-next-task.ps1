<#
.SYNOPSIS
    Picks the next ready issue(s) and hands each one to an AI code assistant to implement,
    build/format/test, commit, push, open a PR and merge it - fully unattended, no human
    review step.

.DESCRIPTION
    The script itself knows nothing about any particular assistant. Everything that is
    specific to one - the executable to run, its command line, which models it exposes and
    which model each size label maps to - lives in an "agent.<name>.json" settings file in
    the repository root, selected with -Agent. Those files are git-ignored (see .gitignore);
    agent.claude.json.example is the committed template.

    Before starting anything new (and again after every task) it checks that no automated
    task is left in flight: if there is an open PR on a "<something>/issue-<n>" branch,
    something did not merge cleanly, and the script stops rather than piling more work on
    top of a broken state.

    If clear, it looks at open issues - in the milestone named by -Milestone, or across the
    whole repository when that is not given - in ascending issue number order, and picks the
    first one whose "**Depends on:** #N, #M" references are all already closed. An issue
    with no such line has no dependencies and is always ready. When nothing is ready, it
    prints why each open issue was skipped, because a wrong number in a Depends-on line is
    otherwise invisible.

    It then invokes the agent with an instruction prompt that points at docs/task-run.md -
    the execution contract every issue links to - asking it to implement the issue end to
    end, open a PR and merge it itself.

    After each task, the script switches to main and pulls, so the next task (and the
    dependency check that picks it) sees the merged result.

.PARAMETER Agent
    Which AI code assistant to drive. Selects "agent.<Agent>.json" next to this script.
    Defaults to "claude".

.PARAMETER Milestone
    Optional. Restricts the search to one milestone and sets the branch prefix to its slug
    ("Blazor demo" -> "blazor-demo/issue-<n>-..."). Omit it to take the first ready issue in
    the repository, on a "task/issue-<n>-..." branch.

.PARAMETER Model
    Auto (default) routes each issue by its size label, using the "modelBySizeLabel" map of
    the agent settings file (7b -> Haiku, 35b -> Sonnet, 70b -> Opus for Claude Code). An
    issue with no size label falls back to "fallbackModel" with a warning; an issue with
    more than one is an error. Any key of the settings file's "models" map overrides the
    labels entirely.

.PARAMETER TaskCount
    How many issues to work through in one invocation. 0 means "keep going until no ready
    issue is left".

.NOTES
    Requires on PATH: gh (authenticated against GitHub), git, dotnet SDK, and whatever
    "command" the agent settings file names. Run from anywhere inside the repository; the
    script cd's to its own location, which must be the repository root.

    Because there is no human review, everything hinges on the agent actually enforcing the
    repository's own gates (dotnet build / format --verify-no-changes / test) before it
    merges - the prompt tells it to, but nothing outside the agent double-checks it.
#>

[CmdletBinding()]
param(
    [string]$Repo = 'miekhra/OpenHoldemEngine',
    [string]$Agent = 'claude',
    [string]$Milestone,
    [string]$Model = 'Auto',
    [ValidateRange(0, 100)]
    [int]$TaskCount = 1
)

$ErrorActionPreference = 'Stop'
Set-Location -LiteralPath $PSScriptRoot

# Runaway backstop for -TaskCount 0. Every iteration either closes an issue or exits, so
# this should never be reached; it is here so a bug cannot turn into an unbounded loop.
$UnlimitedTaskCap = 200

# Any branch this script (or an earlier version of it) would have created.
$AutomatedBranchPattern = '^[A-Za-z0-9._-]+/issue-\d+'

function Fail([string]$Message) {
    Write-Error $Message
    exit 1
}

function ConvertTo-Map($Object) {
    $map = @{}
    if ($null -ne $Object) {
        foreach ($property in $Object.PSObject.Properties) {
            $map[$property.Name] = $property.Value
        }
    }
    return $map
}

function Import-AgentSettings([string]$AgentName, [string]$Root) {
    if ([string]::IsNullOrWhiteSpace($AgentName)) {
        Fail 'The -Agent name is empty.'
    }

    $path = Join-Path -Path $Root -ChildPath "agent.$AgentName.json"
    if (-not (Test-Path -LiteralPath $path -PathType Leaf)) {
        Fail "Agent settings file '$path' was not found. These files are git-ignored on purpose - copy agent.$AgentName.json.example (or agent.claude.json.example) to agent.$AgentName.json and adjust it."
    }

    try {
        $raw = (Get-Content -LiteralPath $path -Raw) | ConvertFrom-Json
    } catch {
        Fail "Agent settings file '$path' is not valid JSON: $($_.Exception.Message)"
    }

    if ([string]::IsNullOrWhiteSpace($raw.command)) {
        Fail "Agent settings file '$path' has no 'command'."
    }

    $agentArgs = @($raw.args)
    if ($agentArgs.Count -eq 0) {
        Fail "Agent settings file '$path' has no 'args'."
    }
    if (-not ($agentArgs | Where-Object { $_ -like '*{prompt}*' })) {
        # Without it the agent would be started with no task at all.
        Fail "Agent settings file '$path' has no '{prompt}' placeholder in 'args'."
    }

    $models = ConvertTo-Map $raw.models
    if ($models.Count -eq 0) {
        Fail "Agent settings file '$path' has no 'models'."
    }

    $modelBySizeLabel = ConvertTo-Map $raw.modelBySizeLabel
    foreach ($label in $modelBySizeLabel.Keys) {
        if (-not $models.ContainsKey($modelBySizeLabel[$label])) {
            Fail "Agent settings file '$path' maps size label '$label' to model '$($modelBySizeLabel[$label])', which is not in 'models'."
        }
    }

    $fallback = $raw.fallbackModel
    if (-not [string]::IsNullOrWhiteSpace($fallback) -and -not $models.ContainsKey($fallback)) {
        Fail "Agent settings file '$path' names fallbackModel '$fallback', which is not in 'models'."
    }

    return [pscustomobject]@{
        Path             = $path
        Name             = if ([string]::IsNullOrWhiteSpace($raw.name)) { $AgentName } else { $raw.name }
        Command          = $raw.command
        Args             = $agentArgs
        Models           = $models
        ModelBySizeLabel = $modelBySizeLabel
        FallbackModel    = $fallback
        ContextFiles     = @($raw.contextFiles | Where-Object { -not [string]::IsNullOrWhiteSpace($_) })
        Env              = ConvertTo-Map $raw.env
    }
}

function Sync-Main {
    git fetch origin --quiet
    git checkout main --quiet
    git reset --hard origin/main --quiet
}

function Get-BranchPrefix {
    if ([string]::IsNullOrWhiteSpace($Milestone)) {
        return 'task/issue-'
    }

    $slug = ($Milestone.ToLowerInvariant() -replace '[^a-z0-9]+', '-').Trim('-')
    if ([string]::IsNullOrEmpty($slug)) {
        return 'task/issue-'
    }

    return "$slug/issue-"
}

function Get-Scope {
    if ([string]::IsNullOrWhiteSpace($Milestone)) {
        return 'the repository'
    }

    return "milestone '$Milestone'"
}

function Get-InFlightAutomatedPrs {
    $openPrs = (gh pr list -R $Repo --state open --json number,headRefName,url) | ConvertFrom-Json
    return @($openPrs | Where-Object { $_.headRefName -match $AutomatedBranchPattern })
}

function Assert-NoInFlightAutomatedPr {
    $inFlight = Get-InFlightAutomatedPrs
    if ($inFlight) {
        Write-Host 'An automated task did not finish cleanly - it left an open PR behind:'
        foreach ($pr in $inFlight) {
            Write-Host "  PR #$($pr.number) $($pr.url)"
        }
        Fail 'Exiting: resolve the PR(s) above (merge or close manually) before running this script again.'
    }
}

function Get-ReadyIssue {
    # Open and closed alike, so "Depends on #N" can be resolved against issues that have
    # already been done. A dependency outside this set is fetched one by one below.
    $ghArgs = @(
        'issue', 'list', '-R', $Repo,
        '--state', 'all',
        '--json', 'number,title,body,state,labels',
        '--limit', '500'
    )
    if (-not [string]::IsNullOrWhiteSpace($Milestone)) {
        $ghArgs += @('--milestone', $Milestone)
    }

    $allIssues = (gh @ghArgs) | ConvertFrom-Json

    $stateByNumber = @{}
    foreach ($i in $allIssues) {
        $stateByNumber[[int]$i.number] = $i.state
    }

    $openIssues = @($allIssues | Where-Object { $_.state -eq 'OPEN' } | Sort-Object { [int]$_.number })
    $skipped = @()

    foreach ($issue in $openIssues) {
        $dependsLine = [regex]::Match($issue.body, '(?m)^\*\*Depends on:?\*\*(.*)$')
        $depNumbers = @()
        if ($dependsLine.Success) {
            $depNumbers = @([regex]::Matches($dependsLine.Groups[1].Value, '#(\d+)') |
                ForEach-Object { [int]$_.Groups[1].Value })
        }

        foreach ($dep in $depNumbers) {
            if (-not $stateByNumber.ContainsKey($dep)) {
                # Outside the milestone, or past the fetch limit. Resolve it directly so a
                # cross-milestone dependency still works.
                $depState = 'MISSING'
                try {
                    $depState = ((gh issue view $dep -R $Repo --json state 2>$null) | ConvertFrom-Json).state
                } catch {
                    $depState = 'MISSING'
                }
                $stateByNumber[$dep] = $depState
            }
        }

        $unmet = @($depNumbers | Where-Object { $stateByNumber[$_] -ne 'CLOSED' })
        if ($unmet.Count -eq 0) {
            return $issue
        }

        $detail = ($unmet | ForEach-Object { "#$_ ($($stateByNumber[$_]))" }) -join ', '
        $skipped += "  #$($issue.number) waits on $detail"
    }

    if ($skipped.Count -gt 0) {
        # A number that does not exist reads as MISSING here. Without this the issue would
        # simply never run, and nothing would say so.
        Write-Host "Every open issue in $(Get-Scope) has an unmet dependency:"
        $skipped | ForEach-Object { Write-Host $_ }
    }

    return $null
}

function Resolve-ModelId($Issue, $Settings) {
    if ($Model -ne 'Auto') {
        return $Settings.Models[$Model]
    }

    $sizeLabels = @(@($Issue.labels | ForEach-Object { $_.name }) |
        Where-Object { $Settings.ModelBySizeLabel.ContainsKey($_) })

    if ($sizeLabels.Count -gt 1) {
        Fail "Issue #$($Issue.number) carries more than one size label ($($sizeLabels -join ', ')). Fix the labels, or pass -Model explicitly."
    }

    if ($sizeLabels.Count -eq 0) {
        $known = ($Settings.ModelBySizeLabel.Keys | Sort-Object) -join '/'
        if ([string]::IsNullOrWhiteSpace($Settings.FallbackModel)) {
            Fail "Issue #$($Issue.number) has no size label ($known) and '$($Settings.Path)' declares no fallbackModel. Label the issue, or pass -Model explicitly."
        }
        Write-Warning "Issue #$($Issue.number) has no size label ($known). Falling back to $($Settings.FallbackModel) - label it, or pass -Model explicitly."
        return $Settings.Models[$Settings.FallbackModel]
    }

    return $Settings.Models[$Settings.ModelBySizeLabel[$sizeLabels[0]]]
}

function New-TaskPrompt([int]$IssueNumber, [string]$BranchPrefix, [string[]]$ContextFiles) {
    # Each assistant has its own house-rules file (CLAUDE.md, AGENTS.md, ...), so the
    # settings file names them rather than the script hard-coding one.
    $docs = (@('docs/task-run.md') + @($ContextFiles)) -join ', '

    return @"
Work in the repository $Repo (the gh CLI is already authenticated). Implement exactly one task - issue #$IssueNumber, and nothing beyond it. Act entirely on your own: there will be no human review, and the decision to merge is yours.

1. Read $docs and the full text of issue #$IssueNumber (``gh issue view $IssueNumber -R $Repo``). docs/task-run.md is the execution contract: the gates, what "done" means, the git protocol, the repository's traps and the engine facts. It is shared by every task; the issue is the task itself.
2. Dependencies were already checked before this task was handed to you: every issue listed on the "**Depends on:**" line of issue #$IssueNumber's body (if there is such a line at all) was CLOSED at launch time. Do not re-check them and do not block on them.
3. Branch off the current main, naming the branch "$BranchPrefix$IssueNumber-<short-slug>".
4. Implement the task strictly as the issue states it: Scope, Files, Definition of done, Notes for the implementer and Context for an AI assistant are acceptance criteria, not suggestions. Do not widen the scope: if you find something else broken nearby, file a separate issue rather than a second commit.
5. Run from the repository root: dotnet build, dotnet format OpenHoldemEngine.sln --verify-no-changes, dotnet test - all three must finish without errors. This is a precondition for every step below; do not move on if any of them failed.
6. If the Definition of done carries a browser probe, run it and quote in the pull request what it printed. "It looks right" is not a check. Do not weaken a check to make it pass: a failing assertion is either a defect in your change or a real defect in the product, and the second case needs a follow-up issue, not a softened assertion.
7. If the Definition of done is not met in full, do not merge and do not close the issue half-done. Finish it yourself; if that is genuinely impossible within this task, stop, merge nothing, and say plainly in a comment on the issue what is blocking completion.
8. Commit with a meaningful message in English and push the branch.
9. Open a pull request: gh pr create -R $Repo --base main --title "..." --body "Closes #$IssueNumber`n`n<description>". The body must contain "Closes #$IssueNumber".
10. If the repository has required CI checks for pull requests, wait for them to finish. Merge the pull request yourself: gh pr merge <number> --squash --delete-branch --auto. Do not leave it open and do not wait for someone else's approval - there will not be one.
11. After the merge, confirm that issue #$IssueNumber actually closed (gh issue view $IssueNumber -R $Repo --json state). If it did not, close it explicitly with a comment saying why.
12. If something you learn while implementing contradicts docs/task-run.md, document it in the pull request and file a separate issue to fix the document.
"@
}

function Invoke-Agent($Settings, [string]$Prompt, [string]$ModelId) {
    $invocation = @($Settings.Args | ForEach-Object {
            $_.Replace('{prompt}', $Prompt).Replace('{model}', $ModelId)
        })

    $saved = @{}
    foreach ($name in $Settings.Env.Keys) {
        $saved[$name] = [Environment]::GetEnvironmentVariable($name)
        [Environment]::SetEnvironmentVariable($name, $Settings.Env[$name])
    }

    try {
        & $Settings.Command @invocation
        return $LASTEXITCODE
    } finally {
        foreach ($name in $saved.Keys) {
            [Environment]::SetEnvironmentVariable($name, $saved[$name])
        }
    }
}

# --- Preconditions ------------------------------------------------------------

$agentSettings = Import-AgentSettings -AgentName $Agent -Root $PSScriptRoot

if ($Model -ne 'Auto' -and -not $agentSettings.Models.ContainsKey($Model)) {
    $known = (($agentSettings.Models.Keys | Sort-Object) -join ', ')
    Fail "-Model '$Model' is not a model of agent '$Agent'. Use Auto or one of: $known."
}

foreach ($tool in @('gh', 'git', 'dotnet', $agentSettings.Command)) {
    if (-not (Get-Command $tool -ErrorAction SilentlyContinue)) {
        Fail "Required tool '$tool' was not found on PATH."
    }
}

try {
    gh auth status --hostname github.com | Out-Null
} catch {
    Fail 'gh CLI is not authenticated against github.com (run "gh auth login").'
}

$dirty = git status --porcelain
if ($dirty) {
    Fail 'Working tree is not clean. Commit, stash or discard local changes before running this script.'
}

Sync-Main

$branchPrefix = Get-BranchPrefix
$unlimited = ($TaskCount -eq 0)
$maxTasks = if ($unlimited) { $UnlimitedTaskCap } else { $TaskCount }

Write-Host "Agent: $($agentSettings.Name) ($($agentSettings.Command)). Scope: $(Get-Scope). Branch prefix: '$branchPrefix'. Model: $Model."

# --- Main loop ----------------------------------------------------------------

$completed = 0

for ($taskIndex = 1; $taskIndex -le $maxTasks; $taskIndex++) {
    if ($unlimited) {
        Write-Host "=== Task $taskIndex (running until no ready issue is left) ==="
    } else {
        Write-Host "=== Task $taskIndex of $TaskCount ==="
    }

    Assert-NoInFlightAutomatedPr

    $readyIssue = Get-ReadyIssue
    if (-not $readyIssue) {
        Write-Host "No ready issue found in $(Get-Scope)."
        break
    }

    $issueNumber = [int]$readyIssue.number
    $modelId = Resolve-ModelId -Issue $readyIssue -Settings $agentSettings

    Write-Host "Next ready issue: #$issueNumber - $($readyIssue.title)"

    $prompt = New-TaskPrompt -IssueNumber $issueNumber -BranchPrefix $branchPrefix -ContextFiles $agentSettings.ContextFiles

    Write-Host "Handing issue #$issueNumber to $($agentSettings.Command) ($modelId)..."
    # The settings file is expected to run the agent non-interactively and without
    # permission prompts: with no human review step, every command (git push,
    # gh pr create, gh pr merge) must run without approval that will never come.
    $agentExitCode = Invoke-Agent -Settings $agentSettings -Prompt $prompt -ModelId $modelId

    Sync-Main

    if ($agentExitCode -ne 0) {
        Fail "$($agentSettings.Command) exited with code $agentExitCode while working on issue #$issueNumber. Not continuing to further tasks."
    }

    $issueAfter = (gh issue view $issueNumber -R $Repo --json state) | ConvertFrom-Json
    $stillOpenPr = Get-InFlightAutomatedPrs

    if ($issueAfter.state -ne 'CLOSED' -or $stillOpenPr) {
        Write-Host "Issue #$issueNumber state after the run: $($issueAfter.state)."
        if ($stillOpenPr) {
            foreach ($pr in $stillOpenPr) {
                Write-Host "  Left-over open PR: #$($pr.number) $($pr.url)"
            }
        }
        Fail "Task on issue #$issueNumber did not complete cleanly (issue not closed and/or a PR was left open). Not continuing to further tasks."
    }

    Write-Host "Issue #$issueNumber closed via merged PR."
    $completed++
}

if ($unlimited -and $completed -eq $UnlimitedTaskCap) {
    Write-Host "Stopped at the $UnlimitedTaskCap-task safety cap with issues still ready. Run again to continue."
}

Write-Host "Done: completed $completed task(s)."
