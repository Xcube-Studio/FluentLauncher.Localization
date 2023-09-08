@echo off
cd .\Scripts
dotnet build /p:Platform="Any CPU"
cd ..\
.\Scripts\bin\Debug\net7.0\LocalizerScript.exe .\Views .\
pause