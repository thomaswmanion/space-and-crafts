$ErrorActionPreference = 'Stop'

function Assert-Contains([string]$Text, [string]$Pattern, [string]$Label) {
    if ($Text -notmatch $Pattern) {
        throw "FAIL: $Label"
    }
    Write-Output "PASS: $Label"
}

& (Join-Path $PSScriptRoot 'verify-unity-project.ps1')
& (Join-Path $PSScriptRoot 'verify-foundation.ps1')

$version = Get-Content -Raw 'ProjectSettings/ProjectVersion.txt'
Assert-Contains $version '6000\.5\.4f1' 'Pinned Unity version is 6000.5.4f1'

$buildSettings = Get-Content -Raw 'ProjectSettings/EditorBuildSettings.asset'
Assert-Contains $buildSettings '(?s)enabled: 1\s+path: Assets/SpaceAndCrafts/Scenes/Menus/HomeMenu\.unity' 'Home Menu is enabled in build settings'
Assert-Contains $buildSettings '(?s)enabled: 1\s+path: Assets/SpaceAndCrafts/Scenes/RainbowWorld/RainbowWorld_Arrival\.unity' 'Rainbow Arrival is enabled in build settings'

$arrival = Get-Content -Raw 'Assets/SpaceAndCrafts/Scenes/RainbowWorld/RainbowWorld_Arrival.unity'
Assert-Contains $arrival 'm_TagString: MainCamera' 'Arrival scene has a MainCamera-tagged camera'
Assert-Contains $arrival 'SpaceAndCrafts\.Cameras\.PlatformingCameraRig' 'Arrival scene has the platforming camera rig'
Assert-Contains $arrival '(?s)m_Name: Splat_TEMP\s+m_TagString: Player' 'Arrival scene contains the Player-tagged Splat root'
Assert-Contains $arrival 'SpaceAndCrafts\.Art\.SplatMeshyVisualMode' 'Arrival opts into the commissioned Meshy Splat visual'
Assert-Contains $arrival '(?s)m_Name: ART_MeshySplatVisual\s+m_TagString: Untagged' 'Arrival serializes the promoted skinned Splat visual under the player'
Assert-Contains $arrival 'm_Controller: \{fileID: 9100000, guid: afb6d81ed36d1f545ad82f6e7ae76963, type: 2\}' 'Arrival visual uses the Iteration 1858 playable controller'

$playerPrefab = Get-Content -Raw 'Assets/SpaceAndCrafts/Prefabs/Characters/Splat_TEMP.prefab'
Assert-Contains $playerPrefab 'guid: afb6d81ed36d1f545ad82f6e7ae76963' 'Splat prefab points to the Iteration 1858 playable controller'

$controller = Get-Content -Raw 'Assets/SpaceAndCrafts/Art/Characters/Splat/Meshy_Iter1858/Splat_Iter1858_Playable.controller'
foreach ($state in @('Locomotion', 'Jump', 'Land', 'Pickup', 'Hit', 'Personality', 'Victory', 'UseInvention', 'Oops', 'Discover', 'Listen')) {
    Assert-Contains $controller "m_Name: $state(?:\r?\n|$)" "Active Splat controller includes $state state"
}

Write-Output 'GAP: Oops, Discover, and Listen currently reuse the imported Alert clip; distinct authored gestures still need runtime review.'
Write-Output 'LIMIT: Static scene/controller inspection cannot prove rendered appearance, animation playback, feel, or human play.'
