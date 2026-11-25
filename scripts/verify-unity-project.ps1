$required = @(
    'ProjectSettings/ProjectVersion.txt',
    'Packages/manifest.json',
    'Assets/SpaceAndCrafts/Scripts/Player/SplatMotor.cs',
    'Assets/SpaceAndCrafts/Input/SplatInputActions.inputactions',
    'Assets/Settings/RainbowURP.asset',
    'Assets/SpaceAndCrafts/Scenes/Development/PlayerPlayground.unity',
    'Assets/SpaceAndCrafts/Prefabs/Characters/Splat_TEMP.prefab'
)

$missing = @()
foreach ($path in $required) {
    if (-not (Test-Path $path)) {
        $missing += $path
    }
}

if ($missing.Count -gt 0) {
    Write-Error ("Missing Unity project paths:`n - " + ($missing -join "`n - "))
    exit 1
}

$manifest = Get-Content -Raw 'Packages/manifest.json'
if ($manifest -notmatch 'com\.unity\.render-pipelines\.universal') {
    Write-Error 'URP package missing from Packages/manifest.json'
    exit 1
}

if ($manifest -notmatch 'com\.unity\.inputsystem') {
    Write-Error 'Input System package missing from Packages/manifest.json'
    exit 1
}

Write-Host 'Unity project verification passed.'
exit 0
