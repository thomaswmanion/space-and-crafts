$required = @(
    'docs/ARCHITECTURE.md',
    'docs/ARRIVAL_PLAN.md',
    'Assets/SpaceAndCrafts/Scripts/Player',
    'Assets/SpaceAndCrafts/Scripts/Camera',
    'Assets/SpaceAndCrafts/Scripts/Crafting',
    'Assets/SpaceAndCrafts/Scenes/Development'
)

$missing = @()
foreach ($path in $required) {
    if (-not (Test-Path $path)) {
        $missing += $path
    }
}

if ($missing.Count -gt 0) {
    Write-Error ("Missing required foundation paths:`n - " + ($missing -join "`n - "))
    exit 1
}

Write-Host 'Foundation verification passed.'
exit 0
