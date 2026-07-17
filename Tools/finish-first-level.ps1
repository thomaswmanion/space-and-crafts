param(
    [string]$ProjectPath = 'C:\Users\thoma\git\space-and-crafts',
    [string]$UnityPath = 'C:\Program Files\Unity\Hub\Editor\6000.5.4f1\Editor\Unity.exe'
)

$ErrorActionPreference = 'Stop'
$logs = Join-Path $ProjectPath 'Logs'
$status = Join-Path $logs 'BackgroundFirstLevel.status.log'
$lockFile = Join-Path $ProjectPath 'Temp\UnityLockfile'
New-Item -ItemType Directory -Force -Path $logs | Out-Null

function Write-Status([string]$message) {
    $timestamp = Get-Date -Format 'yyyy-MM-dd HH:mm:ss'
    Add-Content -LiteralPath $status -Value "[$timestamp] $message"
}

Write-Status 'Waiting for the open Unity editor to release the project.'
while (Test-Path -LiteralPath $lockFile) {
    Start-Sleep -Seconds 10
}

Write-Status 'Project released. Generating the complete first level.'
$sceneLog = Join-Path $logs 'BackgroundCreateFirstLevel.log'
$sceneArguments = @(
    '-batchmode', '-nographics', '-quit',
    '-projectPath', $ProjectPath,
    '-executeMethod', 'SpaceAndCrafts.EditorTools.CreatePrototypeScene.Create',
    '-logFile', $sceneLog
)
$sceneProcess = Start-Process -FilePath $UnityPath -ArgumentList $sceneArguments -Wait -PassThru -WindowStyle Hidden
if ($sceneProcess.ExitCode -ne 0) {
    Write-Status "Scene generation failed with exit code $($sceneProcess.ExitCode). See $sceneLog"
    exit $sceneProcess.ExitCode
}

Write-Status 'Scene generated. Starting WebGL build.'
$buildLog = Join-Path $logs 'BackgroundBuildWebGL.log'
$buildArguments = @(
    '-batchmode', '-nographics', '-quit',
    '-projectPath', $ProjectPath,
    '-buildTarget', 'WebGL',
    '-executeMethod', 'SpaceAndCrafts.EditorTools.BuildWebGL.BuildDevelopment',
    '-logFile', $buildLog
)
$buildProcess = Start-Process -FilePath $UnityPath -ArgumentList $buildArguments -Wait -PassThru -WindowStyle Hidden
if ($buildProcess.ExitCode -ne 0) {
    Write-Status "WebGL build failed with exit code $($buildProcess.ExitCode). See $buildLog"
    exit $buildProcess.ExitCode
}

Write-Status 'Complete. First level regenerated and WebGL build succeeded.'
