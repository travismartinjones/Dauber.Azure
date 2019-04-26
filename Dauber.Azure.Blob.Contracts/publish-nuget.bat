@ECHO OFF
msbuild /p:Configuration=Release
msbuild /t:pack /p:Configuration=Release
@ECHO OFF
CD bin/Release
FOR /F "delims=|" %%I IN ('DIR "*.nupkg" /B /O:D') DO SET NugetPackage=%%I
@ECHO ON
nuget push .\%NugetPackage% -Source http://nuget.dauberapp.com/ eOtwTHovFqWXxPVuxKk0
@ECHO OFF
CD ..\..
@ECHO ON