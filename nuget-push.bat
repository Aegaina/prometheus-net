set SrcDir=%2
set DistDir=%2\.nuget

nuget push %SrcDir%\Prometheus.NetStandard\Bin\Release\*.nupkg -Source Aegaina
nuget push %DistDir%\*.nupkg -Source Aegaina

pause