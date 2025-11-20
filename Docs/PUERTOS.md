# Seguridad Social API - Configuración de Puertos

## Desarrollo

Por defecto, la API se ejecuta en:
- **HTTP**: http://localhost:5000
- **HTTPS**: https://localhost:5001

## Configuración para Producción

Podés cambiar los puertos mediante **variables de entorno**:

### Opción 1: Variable de entorno ASPNETCORE_URLS

```bash
# Linux/macOS
export ASPNETCORE_URLS="https://0.0.0.0:8443;http://0.0.0.0:8080"

# Windows (PowerShell)
$env:ASPNETCORE_URLS="https://0.0.0.0:8443;http://0.0.0.0:8080"

# Windows (CMD)
set ASPNETCORE_URLS=https://0.0.0.0:8443;http://0.0.0.0:8080
```

### Opción 2: Modificar appsettings.Production.json

Creá un archivo `appsettings.Production.json`:

```json
{
  "Kestrel": {
    "Endpoints": {
      "Http": {
        "Url": "http://0.0.0.0:8080"
      },
      "Https": {
        "Url": "https://0.0.0.0:8443"
      }
    }
  }
}
```

### Opción 3: Argumentos de línea de comandos

```bash
dotnet SeguridadSocialApi.dll --urls "https://0.0.0.0:8443;http://0.0.0.0:8080"
```

## Configuración Frontend

Actualizá la URL base del frontend para apuntar a:
- **Desarrollo**: `http://localhost:5000` o `https://localhost:5001`
- **Producción**: El puerto configurado en el servidor

## Notas
- En producción, usá `0.0.0.0` para escuchar en todas las interfaces de red
- Para HTTPS, asegurate de tener un certificado SSL configurado
- Los puertos menores a 1024 requieren permisos de administrador en Linux
