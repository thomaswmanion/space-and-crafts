#Requires -Version 5.1
# Generate 10 critical Space & Crafts assets via Meshy (minimal credits).
# smart-topology meshy-t2: preview (~5 cr) + refine 2K (~10 cr) ~= 15 cr/asset.
# One iteration unless FAILED (then one retry).

$ErrorActionPreference = 'Stop'
if (-not $env:MESHY_API_KEY) {
  $env:MESHY_API_KEY = [Environment]::GetEnvironmentVariable('MESHY_API_KEY', 'User')
}
if (-not $env:MESHY_API_KEY) { throw 'MESHY_API_KEY not set' }

$Root = (Resolve-Path (Join-Path $PSScriptRoot '..')).Path
$OutRoot = Join-Path $Root 'Assets\SpaceAndCrafts\Art\Commissioned'
$ManifestPath = Join-Path $Root 'Logs\meshy-critical10-manifest.json'
$Api = 'https://api.meshy.ai/openapi/v2/text-to-3d'
$Headers = @{
  Authorization = "Bearer $($env:MESHY_API_KEY)"
  'Content-Type' = 'application/json'
}

$Assets = @(
  @{ Id='Splat_Hero'; Folder='Hero'; Prompt='Single game-ready character: Splat, cute teenage round soft purple alien mascot, big expressive eyes, short stubby limbs, tiny goggles on forehead, friendly inventor vibe, clean stylized plastic-toy materials, A-pose, isolated character only, no background props, readable silhouette for 3D platformer'; Poly=8000 }
  @{ Id='BoingOMatic'; Folder='Inventions'; Prompt='Single game prop: colorful springy junk invention launcher called Boing-O-Matic, metal dish base, big coil spring, crystal power core, cartoon stylized plastic and metal, compact readable silhouette, isolated object, no character'; Poly=6000 }
  @{ Id='ScrapMite'; Folder='Enemies'; Prompt='Single cute scrap enemy creature: ScrapMite, small beetle-like junk bug made of bolts and tin, googly eyes, purple and rust accents, stylized game enemy, A-pose, isolated character, readable silhouette'; Poly=5000 }
  @{ Id='ScrapFloater'; Folder='Enemies'; Prompt='Single cute hovering scrap enemy: ScrapFloater, round floating junk orb with a ring, cyan and orange accents, stylized game enemy, isolated character, readable silhouette'; Poly=5000 }
  @{ Id='ScrapRoller'; Folder='Enemies'; Prompt='Single scrap enemy: ScrapRoller, round rolling junk ball with a stripe, metallic scrap plates, stylized game enemy, isolated object creature, readable silhouette'; Poly=5000 }
  @{ Id='ScrapStormSentinel'; Folder='Enemies'; Prompt='Single mini-boss: ScrapStorm Sentinel, medium scrap claw robot made of junk metal, glowing weak-point crystal chest, stylized cartoon threat, A-pose, isolated character, readable silhouette'; Poly=10000 }
  @{ Id='RainbowGateway'; Folder='Landmarks'; Prompt='Single landmark prop: magical rainbow arch gateway portal, colorful stained-glass rainbow arch, crafty handmade look, stylized game landmark, isolated object, no characters'; Poly=8000 }
  @{ Id='RainbowCraftTree'; Folder='Environments'; Prompt='Single environment prop: stylized colorful craft-world tree with quirky rainbow leaves and twisted trunk, game-ready low-poly friendly, isolated tree, no characters'; Poly=5000 }
  @{ Id='ScrapRockCluster'; Folder='Environments'; Prompt='Single environment prop: cluster of scrap rocks and junk stones for a colorful scrap platformer, stylized painted rocks, isolated prop, no characters'; Poly=4000 }
  @{ Id='PowerCrystalPickup'; Folder='Inventions'; Prompt='Single collectible prop: glowing purple-pink power crystal gem for a craft adventure game, faceted crystal, stylized, isolated object, readable pickup silhouette'; Poly=3000 }
)

function Invoke-MeshyJson([string]$Method, [string]$Url, $Body = $null) {
  if ($null -eq $Body) {
    return Invoke-RestMethod -Method $Method -Uri $Url -Headers $Headers
  }
  $json = $Body | ConvertTo-Json -Depth 8 -Compress
  return Invoke-RestMethod -Method $Method -Uri $Url -Headers $Headers -Body $json
}

function Wait-MeshyTask([string]$TaskId, [int]$TimeoutSec = 900) {
  $deadline = (Get-Date).AddSeconds($TimeoutSec)
  do {
    Start-Sleep -Seconds 5
    $task = Invoke-MeshyJson -Method GET -Url "$Api/$TaskId"
    $prog = 0
    if ($null -ne $task.progress) { $prog = $task.progress }
    Write-Host ("  [{0}] {1} {2}%" -f $TaskId, $task.status, $prog)
    if ($task.status -eq 'SUCCEEDED' -or $task.status -eq 'FAILED' -or $task.status -eq 'CANCELED') {
      return $task
    }
  } while ((Get-Date) -lt $deadline)
  throw "Timeout waiting for task $TaskId"
}

function New-MeshyAsset($Spec, [int]$Attempt = 1) {
  $dir = Join-Path $OutRoot $Spec.Folder
  New-Item -ItemType Directory -Force -Path $dir | Out-Null
  $base = Join-Path $dir $Spec.Id

  Write-Host ""
  Write-Host ("=== {0} attempt {1} ===" -f $Spec.Id, $Attempt)

  $previewBody = @{
    mode = 'preview'
    prompt = $Spec.Prompt
    model_type = 'smart-topology'
    ai_model = 'meshy-t2'
    target_polycount = [int]$Spec.Poly
    should_remesh = $false
    target_formats = @('glb', 'fbx')
  }
  $previewCreate = Invoke-MeshyJson -Method POST -Url $Api -Body $previewBody
  # Create endpoints return { "result": "<task_id>" }; poll returns { "id": ... }.
  $previewId = $previewCreate.result
  if (-not $previewId) { $previewId = $previewCreate.id }
  if (-not $previewId) { throw "Preview create returned no task id for $($Spec.Id): $($previewCreate | ConvertTo-Json -Compress)" }
  Write-Host ("  preview task: {0}" -f $previewId)
  $preview = Wait-MeshyTask $previewId
  if ($preview.status -ne 'SUCCEEDED') {
    $msg = ''
    if ($preview.task_error) { $msg = $preview.task_error.message }
    throw "Preview failed for $($Spec.Id): $msg"
  }

  $refineBody = @{
    mode = 'refine'
    preview_task_id = $previewId
    enable_pbr = $true
    texture_resolution = '2k'
    target_formats = @('glb', 'fbx')
  }
  $refineCreate = Invoke-MeshyJson -Method POST -Url $Api -Body $refineBody
  $refineId = $refineCreate.result
  if (-not $refineId) { $refineId = $refineCreate.id }
  if (-not $refineId) { throw "Refine create returned no task id for $($Spec.Id): $($refineCreate | ConvertTo-Json -Compress)" }
  Write-Host ("  refine task: {0}" -f $refineId)
  $refine = Wait-MeshyTask $refineId
  if ($refine.status -ne 'SUCCEEDED') {
    $msg = ''
    if ($refine.task_error) { $msg = $refine.task_error.message }
    throw "Refine failed for $($Spec.Id): $msg"
  }

  $fbxUrl = $refine.model_urls.fbx
  $glbUrl = $refine.model_urls.glb
  $thumbUrl = $refine.thumbnail_url
  if (-not $fbxUrl -and -not $glbUrl) { throw "No model URLs for $($Spec.Id)" }

  if ($fbxUrl) {
    Invoke-WebRequest -Uri $fbxUrl -OutFile ($base + '.fbx') -UseBasicParsing
  }
  if ($glbUrl) {
    Invoke-WebRequest -Uri $glbUrl -OutFile ($base + '.glb') -UseBasicParsing
  }
  if ($thumbUrl) {
    Invoke-WebRequest -Uri $thumbUrl -OutFile ($base + '.thumb.png') -UseBasicParsing
  }

  # PBR maps (no extra credits) — required for URP Lit; FBX alone often looks gray/pink.
  $texDir = $base
  New-Item -ItemType Directory -Force -Path $texDir | Out-Null
  $maps = $refine.texture_urls
  if ($null -ne $maps -and $maps.Count -gt 0) {
    $t0 = $maps[0]
    foreach ($key in @('base_color', 'metallic', 'roughness', 'normal')) {
      $url = $t0.$key
      if (-not $url) { continue }
      Invoke-WebRequest -Uri $url -OutFile (Join-Path $texDir ($key + '.png')) -UseBasicParsing
    }
  }

  return [pscustomobject]@{
    id = $Spec.Id
    folder = $Spec.Folder
    previewId = $previewId
    refineId = $refineId
    fbx = $(if (Test-Path ($base + '.fbx')) { $base + '.fbx' } else { $null })
    glb = $(if (Test-Path ($base + '.glb')) { $base + '.glb' } else { $null })
    thumb = $(if (Test-Path ($base + '.thumb.png')) { $base + '.thumb.png' } else { $null })
    attempt = $Attempt
    skipped = $false
  }
}

$results = @()
New-Item -ItemType Directory -Force -Path (Join-Path $Root 'Logs') | Out-Null

foreach ($spec in $Assets) {
  $existingFbx = Join-Path (Join-Path $OutRoot $spec.Folder) ($spec.Id + '.fbx')
  $existingGlb = Join-Path (Join-Path $OutRoot $spec.Folder) ($spec.Id + '.glb')
  if ((Test-Path $existingFbx) -or (Test-Path $existingGlb)) {
    Write-Host ""
    Write-Host ("=== {0} SKIP (already on disk) ===" -f $spec.Id)
    $results += [pscustomobject]@{
      id = $spec.Id
      folder = $spec.Folder
      skipped = $true
      fbx = $(if (Test-Path $existingFbx) { $existingFbx } else { $null })
      glb = $(if (Test-Path $existingGlb) { $existingGlb } else { $null })
      thumb = Join-Path (Join-Path $OutRoot $spec.Folder) ($spec.Id + '.thumb.png')
      attempt = 0
    }
    continue
  }

  try {
    $results += New-MeshyAsset $spec -Attempt 1
  } catch {
    Write-Host ("  FAILED once: {0} - retrying once" -f $_.Exception.Message)
    $results += New-MeshyAsset $spec -Attempt 2
  }
}

$results | ConvertTo-Json -Depth 6 | Set-Content -Path $ManifestPath -Encoding UTF8
Write-Host ""
Write-Host ("Manifest written: {0}" -f $ManifestPath)
Write-Host ("Done. Assets: {0}" -f $results.Count)
$results | Format-Table id, folder, attempt, skipped -AutoSize
