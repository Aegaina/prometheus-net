set SrcDir=%2
set DistDir=%SrcDir%\.nuget

if not exist "%DistDir%" (
    md "%DistDir%"
) else (
    del %DistDir%\*.nupkg /f/s/q
)

nuget pack %SrcDir%\Prometheus.NetFramework.AspNet\Prometheus.NetFramework.AspNet.csproj -OutputDirectory %DistDir% -properties Configuration=Release

pause