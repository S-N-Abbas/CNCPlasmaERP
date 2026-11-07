$previousProfile = $env:CNCPLASMAERP_PROFILE
$projectPath = Join-Path $PSScriptRoot 'CNCPlasmaERP.UI\CNCPlasmaERP.UI.csproj'

try {
    $env:CNCPLASMAERP_PROFILE = 'Test'
    dotnet build $projectPath -m:1
    if ($LASTEXITCODE -ne 0) {
        throw "Test-profile build failed with exit code $LASTEXITCODE."
    }

    dotnet run --no-build --no-restore --project $projectPath
}
finally {
    if ($null -eq $previousProfile) {
        Remove-Item Env:CNCPLASMAERP_PROFILE -ErrorAction SilentlyContinue
    }
    else {
        $env:CNCPLASMAERP_PROFILE = $previousProfile
    }
}
