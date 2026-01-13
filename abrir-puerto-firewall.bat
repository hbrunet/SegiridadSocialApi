@echo off
REM ========================================
REM Script para abrir puerto 5000 en Firewall de Windows
REM Debe ejecutarse como Administrador
REM ========================================

echo.
echo ========================================
echo Configurando Firewall de Windows
echo ========================================
echo.

REM Verificar si se está ejecutando como administrador
net session >nul 2>&1
if %errorLevel% neq 0 (
    echo ERROR: Este script debe ejecutarse como Administrador
    echo.
    echo Haz click derecho en el archivo y selecciona "Ejecutar como administrador"
    echo.
    pause
exit /b 1
)

echo Abriendo puerto 5000 TCP para la API...
echo.

REM Eliminar regla anterior si existe
netsh advfirewall firewall delete rule name="Seguridad Social API - Puerto 5000" >nul 2>&1

REM Agregar nueva regla para TCP
netsh advfirewall firewall add rule ^
    name="Seguridad Social API - Puerto 5000" ^
    dir=in ^
    action=allow ^
    protocol=TCP ^
    localport=5000 ^
    profile=any ^
    description="Permite acceso a la API de Seguridad Social en puerto 5000"

if %errorLevel% equ 0 (
    echo.
    echo ========================================
    echo ✓ Puerto 5000 abierto exitosamente!
    echo ========================================
    echo.
    echo La API ahora es accesible desde:
    echo   - http://localhost:5000/swagger (mismo servidor)
    echo   - http://^<IP-SERVIDOR^>:5000/swagger (desde clientes)
    echo.
    echo Para encontrar la IP del servidor, ejecuta: ipconfig
    echo.
) else (
    echo.
    echo ERROR: No se pudo abrir el puerto
    echo.
)

echo Para verificar la regla:
echo   netsh advfirewall firewall show rule name="Seguridad Social API - Puerto 5000"
echo.

pause
