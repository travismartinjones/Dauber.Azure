@ECHO OFF
FOR /F "delims=|" %%I IN ('DIR "*.csproj" /B /O:D') DO SET ProjectFile=%%I
nuget restore .\%ProjectFile% -PackagesDirectory ".\packages" -Source "http://dauber-nuget.azurewebsites.net/nuget/" -FallbackSource "https://api.nuget.org/v3/index.json"
FOR /F "delims=|" %%I IN ('DIR "*.nupkg" /S /B /O:D') DO move %%I "./packages/"
ECHO ON
nuget pack .\%ProjectFile% -Prop Configuration=Release -Build
@ECHO OFF
FOR /F "delims=|" %%I IN ('DIR "*.nupkg" /B /O:D') DO SET NugetPackage=%%I
ECHO ON
nuget push .\%NugetPackage% -s http://dauber-nuget.azurewebsites.net/ eOtwTHovFqWXxPVuxKk0