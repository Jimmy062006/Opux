 @ECHO OFF
 SET mypath=%~dp0
 ECHO Building Opux2
 dotnet build
 cd Opux2
 ECHO Publishing Opux 2 to %mypath:~0,-1%
 rmdir /q /S %mypath:~0,-1%\Release
 dotnet publish -c Release --runtime win-x64 --output %mypath:~0,-1%\Release
 xcopy %mypath:~0,-1%\Opux2\bin\Debug\netcoreapp2.0\Plugins %mypath:~0,-1%\Release\Plugins /q /e /s /i /y /Exclude:%mypath:~0,-1%\exclusion_list