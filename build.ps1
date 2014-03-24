$nuget = (Get-Command nuget -ErrorAction SilentlyContinue).Definition
if(!$nuget) {
    # Download nuget
    $nuget = Join-Path ([System.IO.Path]::GetTempPath()) "nuget.exe"
    if(Test-Path $nuget) {
        del $nuget -for
    }
    Write-Host "Downloading NuGet.exe. To avoid this in the future, put a copy in your PATH."
    Invoke-WebRequest "https://nuget.org/nuget.exe" -OutFile $nuget
}
pushd $PSScriptRoot
Write-Host -ForegroundColor Green "*** Restoring Packages ***"
&$nuget restore
Write-Host -ForegroundColor Green "*** Building ***"
$first = dir packages\NuGet.Services.Build* | sort -desc Name | select -first 1
if(!$first) {
    throw "Unable to find NuGet.Services.Build package!"
}
msbuild "$first\tools\NuGet.Services.FullBuild.msbuild" @args
popd