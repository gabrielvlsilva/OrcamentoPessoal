
@echo off
title Servico de Arquivos - Orcamento Pessoal

cd /d "%~dp0"

if not exist ".venv\Scripts\python.exe" (
    echo ERRO: Ambiente virtual nao encontrado.
    pause
    exit /b 1
)

echo Iniciando FastAPI...

".venv\Scripts\python.exe" -m uvicorn app.main:app --host 127.0.0.1 --port 8001 --reload

pause
