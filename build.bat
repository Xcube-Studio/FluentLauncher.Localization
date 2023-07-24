@echo off
cd .\Scripts
dotnet build
cd ..\
.\Scripts\bin\Debug\net7.0\LocalizerScript.exe .\Views .\
pause