@echo off
setlocal
set "CNCPLASMAERP_PROFILE=Test"
set "PROJECT=%~dp0CNCPlasmaERP.UI\CNCPlasmaERP.UI.csproj"

dotnet build "%PROJECT%" -m:1
if errorlevel 1 exit /b %errorlevel%

dotnet run --no-build --no-restore --project "%PROJECT%"
exit /b %errorlevel%
