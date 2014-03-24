$first = dir packages\NuGet.Services.Build* | sort -desc Name | select -first 1
if(!$first) {
    throw "Unable to find NuGet.Services.Build package!"
}
msbuild "$first\tools\NuGet.Services.FullBuild.msbuild"