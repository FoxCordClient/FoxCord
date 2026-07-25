@echo off
setlocal EnableDelayedExpansion

set DOTNET_CLI_TELEMETRY_OPTOUT=1

title Compilando C#

set CONFIG=Release
set FRAMEWORK=net10.0-windows

echo.
echo ===============================
echo Limpando projeto...
echo ===============================

dotnet clean

if exist bin rmdir /s /q bin
if exist obj rmdir /s /q obj

echo.
echo ===============================
echo Restaurando pacotes...
echo ===============================

dotnet restore

echo.
echo ===============================
echo Compilando x86...
echo ===============================

dotnet publish ^
-c %CONFIG% ^
-f %FRAMEWORK% ^
-r win-x86 ^
--self-contained false ^
-p:PublishSingleFile=true ^
-p:DebugType=None ^
-p:DebugSymbols=false ^
-p:ApplicationIcon=app.ico ^
-p:GenerateAssemblyInfo=true ^
-p:UseAppHost=true

echo.
echo ===============================
echo Compilando x64...
echo ===============================

dotnet publish ^
-c %CONFIG% ^
-f %FRAMEWORK% ^
-r win-x64 ^
--self-contained false ^
-p:PublishSingleFile=true ^
-p:DebugType=None ^
-p:DebugSymbols=false ^
-p:ApplicationIcon=app.ico ^
-p:GenerateAssemblyInfo=true ^
-p:UseAppHost=true

if not exist Release mkdir Release

echo.
echo ===============================
echo Copiando arquivos...
echo ===============================

for %%F in ("bin\%CONFIG%\%FRAMEWORK%\win-x86\publish\*.exe") do (
    copy /Y "%%F" "Release\%%~nF_x86.exe" >nul
)

for %%F in ("bin\%CONFIG%\%FRAMEWORK%\win-x64\publish\*.exe") do (
    copy /Y "%%F" "Release\%%~nF_x64.exe" >nul
)

echo.
echo ===============================
echo Compilacao concluida!
echo ===============================
echo.
echo Arquivos gerados:
echo.
dir /b Release
echo.

pause