# Flow Session Management

## Problema Resuelto

Las **tablas temporales globales (GTT)** de Oracle son por sesión. Cuando un procedimiento almacenado valida datos contra una GTT, debe ejecutarse en la **misma sesión** donde se cargaron los datos temporales.

Anteriormente, el flujo estaba dividido en **dos requests HTTP independientes**:
1. **Upload**: Carga el archivo, crea la external table, y popula la GTT.
2. **Crear Hoja**: Ejecuta el stored procedure que valida contra la GTT.

Como cada request tiene su propia sesión de Oracle (via `UnitOfWork` scoped), el SP en el segundo request ve la GTT vacía y falla.

## Solución Implementada

Implementamos un **FlowSessionManager** que mantiene una conexión Oracle abierta entre los dos pasos, identificada por un `flowId`:

- Al cargar el archivo, el backend crea una sesión pinned, ejecuta las operaciones sobre la GTT, y devuelve un `flowId` al frontend.
- El frontend incluye ese `flowId` en el request de creación de hoja.
- El backend reutiliza la misma conexión Oracle, el SP ve los datos temporales, y la validación funciona.
- Después de crear la hoja (o en caso de error), la sesión se cierra y limpia automáticamente.

## Configuración

En `appsettings.json`:

```json
"FlowSession": {
  "ExpirationMinutes": 10
}
```

La sesión expira después de 10 minutos de inactividad (sliding expiration) o máximo 20 minutos (absolute expiration).

## Uso desde el Frontend

### Paso 1: Upload

```http
POST /api/novedades/upload
Content-Type: multipart/form-data

file: <archivo>
tipoNovedad: 156
```

**Respuesta**:
```json
{
  "file_name": "archivo.txt",
  "cantidad_registros": 100,
  "error_ora": 0,
  "mensaje": "",
  "id_archivo": 12345,
  "sum_rem1": 1000.00,
  "sum_rem2": 2000.00,
  "sum_rem3": 3000.00,
  "flow_id": "a3f2c1b5d4e3f2a1b5c4d3e2f1a0b9c8"
}
```

**Importante**: Guarda el `flow_id` para el siguiente paso.

### Paso 2: Crear Hoja

Dentro de los **10 minutos** siguientes, envía el request de creación incluyendo el `flow_id`:

```http
POST /api/novedades/crear-hoja
Content-Type: application/json

{
  "id_archivo": 12345,
  "tipo_novedad": 156,
  "grupo_adicional": 1,
  "tipo_liquidacion": 2,
  "cantidad_registros": 100,
  "periodo": "2025-07-01T00:00:00",
  "id_rep": 999,
  "flow_id": "a3f2c1b5d4e3f2a1b5c4d3e2f1a0b9c8"
}
```

**Respuesta exitosa**:
```json
{
  "id_archivo": 12345,
  "tipo_novedad": 156,
  "grupo_adicional": 1,
  "tipo_liquidacion": 2,
  "nro_hoja": 5001
}
```

## Manejo de Errores

### FlowId expirado o inválido

Si el `flow_id` no existe o expiró:

```json
{
  "error": "El flujo indicado expiró o es inválido. Vuelva a cargar el archivo."
}
```

**Solución**: Repetir el upload para obtener un nuevo `flow_id`.

### Error durante el upload

Si falla en el paso de upload, la sesión se limpia automáticamente. El frontend puede reintentar.

### Error durante crear-hoja

Si falla al crear la hoja (validación PL/SQL, etc.), la sesión se cierra y debe repetir el proceso completo desde upload.

## Consideraciones Técnicas

- **Pooling deshabilitado**: Las conexiones del flow usan `Pooling=false` para garantizar que se reutilice exactamente la misma sesión Oracle.
- **Memory Cache**: Usa `IMemoryCache` con sliding expiration para limpieza automática.
- **Thread-safe**: Diseñado para múltiples flujos concurrentes.
- **Compatibilidad**: El campo `flow_id` es opcional en `crear-hoja`. Si se omite, funciona con la sesión del request actual (útil para testing o migraciones graduales).

## Limpieza Automática

- Las sesiones expiran automáticamente si no se usan dentro del período configurado.
- Al completar o fallar `crear-hoja`, la sesión se elimina explícitamente.
- La memoria caché de .NET también limpia entradas expiradas en background.
