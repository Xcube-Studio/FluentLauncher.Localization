@echo off
cd .\Scripts
dotnet build --configuration Release /p:Platform="Any CPU"
cd ..\
.\Scripts\bin\Release\net8.0\LocalizerScript.exe .\Views .\
pause