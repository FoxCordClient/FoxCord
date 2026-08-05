@echo off
title Git Auto Push
color 0A

echo ===========================
echo      Git Auto Push
echo ===========================
echo.

set /p COMMIT=Mensagem do commit: 

if "%COMMIT%"=="" (
    echo Digite uma mensagem valida.
    pause
    exit
)

git add .

git commit -m "%COMMIT%"

git branch -M main

git remote remove origin >nul 2>&1
git remote add origin https://github.com/FoxCordClient/FoxCord.git

echo.
echo Enviando...
git push -u origin main --force

echo.
echo ===========================
echo Finalizado!
echo ===========================
pause