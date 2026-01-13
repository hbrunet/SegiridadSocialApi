@echo off
REM ========================================
REM Script para cerrar puerto 5000 en Firewall
REM ========================================

echo.
echo ========================================
echo Cerrando puerto 5000 en Firewall
echo ========================================
echo.

REM Verificar si se está ejecutando como administrador
net session >nul 2>&1
if %errorLevel% neq 0 (
    echo ERROR: Este script debe ejecutarse como Administrador
    echo.
    pause
    exit /b 1
)

netsh advfirewall firewall delete rule name="Seguridad Social API - Puerto 5000"

if %errorLevel% equ 0 (
    echo ✓ Puerto 5000 cerrado exitosamente
) else (
    echo No se encontró la regla o ya estaba cerrada
)

echo.
pause
