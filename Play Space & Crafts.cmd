@echo off
setlocal
set "GAME_EXE=%~dp0Builds\Windows\SpaceAndCrafts.exe"
if not exist "%GAME_EXE%" (
    echo The Windows review player is missing.
    echo Open the project in Unity 6000.5.4f1 and choose:
    echo Space ^& Crafts ^> Build Windows Player ^(Windowed Review^)
    pause
    exit /b 1
)
start "Space and Crafts" "%GAME_EXE%"
endlocal
