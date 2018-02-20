#!/bin/bash
mypath=${PWD}
echo Building Opux2
dotnet build
cd Opux2
echo Publishing Opux 2 to $mypath/Release
rm -Rf $mypath/Release
dotnet publish -c Release --runtime linux-x64 --output $mypath/Release
rsync -avz --exclude='BuildEvents.dll' --exclude='BuildEvents.pdb' $mypath/Opux2/bin/Debug/netcoreapp2.0/Plugins/ $mypath/Release/Plugins/