# Configuración de Kestrel

## Configuración de URLs y Puertos

La aplicación utiliza la configuración de Kestrel definida en `appsettings.json` para determinar las URLs y puertos en los que escucha.

### Configuración Actual

```json
{
  "Kestrel": {
    "Endpoints": {
      "Http": {
   "Url": "http://localhost:5000"
   },
      "Https": {
        "Url": "https://localhost:5001"
 }
    }
  }
}
```

### URLs Configuradas

- **HTTP**: `http://localhost:5000`
- **HTTPS**: `https://localhost:5001`

### Cómo Modificar las URLs

Para cambiar los puertos o URLs en los que la aplicación escucha:

1. Edita el archivo `appsettings.json`
2. Modifica los valores en `Kestrel:Endpoints:Http:Url` y/o `Kestrel:Endpoints:Https:Url`
3. Reinicia la aplicación

### Variables de Entorno (Opcional)

También puedes sobrescribir la configuración usando variables de entorno:

```bash
# Linux/Mac
export ASPNETCORE_URLS="http://localhost:8080;https://localhost:8443"

# Windows PowerShell
$env:ASPNETCORE_URLS="http://localhost:8080;https://localhost:8443"

# Windows CMD
set ASPNETCORE_URLS=http://localhost:8080;https://localhost:8443
```

**Nota**: Si se define la variable de entorno `ASPNETCORE_URLS`, esta tiene prioridad sobre la configuración en `appsettings.json`.

### Configuración por Ambiente

Puedes crear archivos específicos por ambiente:

- `appsettings.Development.json` - Para desarrollo
- `appsettings.Production.json` - Para producción
- `appsettings.Staging.json` - Para staging

Ejemplo de `appsettings.Production.json`:

```json
{
  "Kestrel": {
    "Endpoints": {
      "Http": {
        "Url": "http://0.0.0.0:80"
      },
 "Https": {
        "Url": "https://0.0.0.0:443"
      }
    }
  }
}
```

### Referencias

- [Documentación oficial de Kestrel en .NET 8](https://learn.microsoft.com/en-us/aspnet/core/fundamentals/servers/kestrel)
- [Configuración de Endpoints en Kestrel](https://learn.microsoft.com/en-us/aspnet/core/fundamentals/servers/kestrel/endpoints)
