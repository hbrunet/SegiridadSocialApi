@echo off
REM ========================================
REM Script para verificar conectividad
REM ========================================

echo.
echo ========================================
echo Diagnóstico de Conectividad
echo ========================================
echo.

echo 1. Verificando si la aplicación está corriendo en puerto 5000...
echo.
netstat -ano | findstr ":5000"
if %errorLevel% equ 0 (
    echo ✓ La aplicación está escuchando en puerto 5000
) else (
    echo ✗ La aplicación NO está corriendo en puerto 5000
    echo   Ejecuta: dotnet run
)

echo.
echo 2. IP del servidor:
echo.
ipconfig | findstr /i "IPv4"

echo.
echo 3. Estado de la regla de firewall:
echo.
netsh advfirewall firewall show rule name="Seguridad Social API - Puerto 5000" >nul 2>&1
if %errorLevel% equ 0 (
    echo ✓ Regla de firewall configurada
    netsh advfirewall firewall show rule name="Seguridad Social API - Puerto 5000"
) else (
    echo ✗ NO existe regla de firewall para el puerto 5000
 echo   Ejecuta: abrir-puerto-firewall.bat (como Administrador)
)

echo.
echo 4. Verificando si el firewall está activo:
echo.
netsh advfirewall show currentprofile state

echo.
echo ========================================
echo Pruebas desde cliente:
echo ========================================
echo.
echo Desde otra máquina, prueba:
echo   1. ping ^<IP-SERVIDOR^>
echo   2. telnet ^<IP-SERVIDOR^> 5000
echo   3. http://^<IP-SERVIDOR^>:5000/swagger
echo.

pause
