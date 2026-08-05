@echo off
setlocal EnableDelayedExpansion
title Git Auto Push
color 0A

where git >nul 2>&1
if errorlevel 1 (
    echo.
    echo [ERRO] Git nao encontrado!
    pause
    exit /b
)

cd /d "%~dp0"

echo.
echo ============================
echo        Git Auto Push
echo ============================
echo.

:: Cria repositorio se nao existir
if not exist ".git" (
    echo Nenhum repositorio Git encontrado.
    echo Criando...
    git init

    echo.
    set /p REPO=Digite a URL do repositorio GitHub:
    if "!REPO!"=="" (
        echo URL invalida.
        pause
        exit /b
    )

    git remote add origin "!REPO!"
)

:: Caso nao tenha origin
git remote get-url origin >nul 2>&1
if errorlevel 1 (
    echo.
    set /p REPO=Digite a URL do repositorio GitHub:
    git remote add origin "!REPO!"
)

:: Commit
echo.
set /p MSG=Mensagem do commit:

if "!MSG!"=="" (
    set MSG=Update
)

echo.
echo Adicionando arquivos...
git add .

echo.
echo Criando commit...
git commit -m "!MSG!"

echo.
echo Descobrindo branch...

for /f "delims=" %%i in ('git branch --show-current') do set BRANCH=%%i

if "!BRANCH!"=="" (
    set BRANCH=main
    git branch -M main
)

echo.
echo Enviando...
git push -u origin !BRANCH!

if errorlevel 1 (
    echo.
    echo Tentando criar a branch...
    git branch -M main
    git push -u origin main

    if errorlevel 1 (
        git branch -M master
        git push -u origin master
    )
)

echo.
echo ============================
echo        Concluido!
echo ============================
pause