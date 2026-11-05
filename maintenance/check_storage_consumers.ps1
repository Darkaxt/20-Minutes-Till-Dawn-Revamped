$ErrorActionPreference = 'Stop'
$project = Split-Path -Parent $PSScriptRoot
$evidence = 'D:/Projects/20MTD-Modernization-evidence/storage-migration-20261105'
$legacyPattern = '20mtd-(branding-20261022|parity-acceptance-20261023|parity-assets-20261022-complete|achievement-api-20261023|pixel-api-20261023|droiddeck-20261022|storage-verification-20261105)'
$consumers = @(Get-CimInstance Win32_Process | Where-Object {
    $_.CommandLine -match $legacyPattern -and $_.Name -notin @('pwsh.exe','rg.exe') -and
    $_.CommandLine -notmatch 'invoke_transactional_cleanup.py|maintenance/storage_migration.py'
} | Select-Object ProcessId,Name,CommandLine)
$tasks = @(Get-ScheduledTask | Where-Object {
    ($_.Actions.Execute -join ' ') -match $legacyPattern -or ($_.Actions.Arguments -join ' ') -match $legacyPattern
} | Select-Object TaskName,TaskPath)
$tokens = $null
$parseErrors = $null
$null = [System.Management.Automation.Language.Parser]::ParseFile((Join-Path $project 'tools/run_selectors.ps1'),[ref]$tokens,[ref]$parseErrors)
if ($consumers.Count -or $tasks.Count -or $parseErrors.Count) { throw 'Active legacy consumer or PowerShell parse error; inspect before cleanup' }
$matches = @(& rg -l $legacyPattern (Join-Path $project 'tools') --glob '*.py' --glob '*.ps1')
if ($LASTEXITCODE -gt 1 -or $matches.Count) { throw 'Legacy helper references remain' }
$report = @{status='PASS'; process_consumers=$consumers; scheduled_consumers=$tasks; legacy_helper_references=$matches; powershell_parse='PASS'}
$report | ConvertTo-Json -Depth 4 | Set-Content -LiteralPath (Join-Path $evidence 'consumer-verification.json') -Encoding utf8
Write-Output 'PASS: no active legacy process/scheduled consumers, no stale shared-helper references; PowerShell parses'
