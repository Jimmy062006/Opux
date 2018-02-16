#!/bin/bash
mypath=${PWD}
echo Building Opux2
dotnet build
cd Opux2
echo Publishing Opux 2 to $mypath/Release
rmdir /S $mypath/Release
dotnet publish -c Release --output $%mypath\Release
xcopy $mypath/Opux2/bin/Debug/netcoreapp2.0/Plugins $mypath/Release/Plugins /e /s /i /y /Exclude:$mypath/exclusion_list