param(
    [Parameter(Mandatory=$true, Position=0)][string]$RepositoryName, 
    [Parameter(Mandatory=$true, Position=1)][string]$Summary,
    [Parameter(Mandatory=$false)][string]$Path,
    [Parameter(Mandatory=$false)][string]$TemplatePath,
    [Parameter(Mandatory=$false)][string]$RemoteRepositoryUrl)

if(!$RemoteRepositoryUrl) {
    $RemoteRepositoryUrl = "https://github.com/NuGet/$RepositoryName"
}
if(!$Path) {
    $Path = Join-Path (Get-Location) $RepositoryName
}
if(!$TemplatePath) {
    $TemplatePath = Join-Path $PSScriptRoot "ServiceRepositoryTemplate"
}
$TemplatePath = Convert-Path $TemplatePath

if(!(Test-Path $TemplatePath)) {
    throw "Could not find template in $TemplatePath!"
}

Write-Host "Creating a NuGet Services Repository in $Path ..."

if((Test-Path $Path) -and (@(dir $Path).Length -gt 0)) {
    throw "$Path exists and is not empty!"
}

if(!(Test-Path $Path)) {
    mkdir $Path | Out-Null
}
$Path = Convert-Path $Path
pushd $Path
dir $TemplatePath | foreach {
    $content = [IO.File]::ReadAllText($_.FullName)
    $content = $content.Replace("`$repository`$", $RepositoryName).Replace("`$summary`$", $Summary)
    $Target = Join-Path $Path $_.Name
    
    Write-Verbose "Writing file: $($_.Name)"
    [IO.File]::WriteAllText($Target, $content)
}
git init
git add -A
git commit -am"Initial Repository Layout"
git remote set-url origin $RemoteRepositoryUrl

Write-Host "Repository created and initial commit made. The initial commit HAS NOT BEEN PUSHED to the origin, but the origin is configured."
popd