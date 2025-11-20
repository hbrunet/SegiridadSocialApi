# ?? Ejemplos de Respuestas API - Sistema de Validaciones

## ?? Upload con Auto-validación

### Request

```http
POST /api/novedades/upload HTTP/1.1
Host: localhost:5000
Content-Type: multipart/form-data; boundary=----WebKitFormBoundary7MA4YWxkTrZu0gW

------WebKitFormBoundary7MA4YWxkTrZu0gW
Content-Disposition: form-data; name="file"; filename="novedades_enero_2024.txt"
Content-Type: text/plain

[contenido del archivo]
------WebKitFormBoundary7MA4YWxkTrZu0gW
Content-Disposition: form-data; name="tipoNovedad"

1
------WebKitFormBoundary7MA4YWxkTrZu0gW
Content-Disposition: form-data; name="autoValidar"

true
------WebKitFormBoundary7MA4YWxkTrZu0gW--
```

### Response - Archivo Válido

```json
{
  "file_name": "novedades_enero_2024.txt",
  "cantidad_registros": 1000,
  "error_ora": 0,
  "mensaje": "",
  "id_archivo": 12345,
  "sum_rem_1": 15000000.50,
  "sum_rem_2": 8500000.75,
  "sum_rem_3": 2000000.00,
  "flow_id": "550e8400-e29b-41d4-a716-446655440000",
  "validaciones": {
    "registros_validos": 1000,
  "registros_advertencias": 0,
    "registros_errores": 0,
    "errores": [],
    "advertencias": []
  }
}
```

### Response - Archivo con Errores

```json
{
  "file_name": "novedades_enero_2024.txt",
  "cantidad_registros": 1000,
  "error_ora": 0,
  "mensaje": "",
  "id_archivo": 12345,
  "sum_rem_1": 15000000.50,
  "sum_rem_2": 8500000.75,
  "sum_rem_3": 2000000.00,
  "flow_id": "550e8400-e29b-41d4-a716-446655440000",
  "validaciones": {
    "registros_validos": 950,
    "registros_advertencias": 3,
    "registros_errores": 47,
    "errores": [
      {
        "mensaje": "[FORMATO_CUIL] CUIL inválido: '2012345' (debe tener 11 dígitos numéricos)",
   "linea": 5,
        "columna": 1
      },
      {
        "mensaje": "[CUIL_DUPLICADO] CUIL 20123456789 está duplicado 2 veces en el archivo",
        "linea": 15,
 "columna": 1
      },
      {
        "mensaje": "[CAMPOS_OBLIGATORIOS] CUIL 20987654321: Campo PERIODO es obligatorio",
        "linea": 28,
        "columna": 3
   },
      {
    "mensaje": "[REMUNERACION_POSITIVA] CUIL 20111222333: REMUNIMPONIBLE1 tiene valor negativo (-1000)",
        "linea": 42,
        "columna": 5
      },
      {
        "mensaje": "[REMUNERACION_POSITIVA] CUIL 20444555666: REMUNIMPONIBLE2 tiene valor negativo (-500.50)",
        "linea": 67,
        "columna": 6
      },
      {
        "mensaje": "[TIPO_EMPRESA_VALIDO] CUIL 20123456789: TIPOEMPRESA = 'X' no es válido (valores permitidos: '3', 'G')",
        "linea": 30,
        "columna": 38
      },
      {
        "mensaje": "[CODIGO_ACTIVIDAD_VALIDO] CUIL 20123456789: CODACTIVIDAD = 99 no es válido (valores permitidos: 19, 32, 45, 52, 65, 76, 77, 85, 913)",
        "linea": 25,
        "columna": 8
      },
      {
        "mensaje": "[CODIGO_CONDICION_VALIDO] CUIL 20123456789: CODCONDICION = 3 no es válido (valores permitidos: 1, 2, 5)",
        "linea": 18,
        "columna": 7
      },
      {
        "mensaje": "[OBRA_SOCIAL_NACIONAL_REQUERIDA] CUIL 20123456789: CODACTIVIDAD 46 requiere REMUNIMPONIBLE4 (Obra Social Nacional) con valor positivo (actual: 0)",
        "linea": 35,
        "columna": 24
      }
    ],
    "advertencias": [
   {
        "mensaje": "[PERIODO_FUTURO] CUIL 20777888999: Período 2025-06 es futuro",
        "linea": 100,
        "columna": 3
  },
      {
      "mensaje": "[CONSISTENCIA_TIPO_LIQUIDACION] CUIL 20333444555: Aguinaldo declarado en 2024-03 (debe ser Jun o Dic)",
        "linea": 234,
        "columna": 4
      },
      {
        "mensaje": "[CONSISTENCIA_TIPO_LIQUIDACION] CUIL 20666777888: Salario mensual duplicado en período 2024-01 (2 veces)",
  "linea": 456,
        "columna": 4
      }
    ]
  }
}
```

---

## ?? Validar Archivo (con FlowId)

### Request

```http
POST /api/novedades/validar-archivo/12345?flowId=550e8400-e29b-41d4-a716-446655440000 HTTP/1.1
Host: localhost:5000
```

### Response - Sin Errores

```json
{
  "registros_validos": 1000,
  "registros_advertencias": 0,
  "registros_errores": 0,
  "errores": [],
  "advertencias": []
}
```

### Response - Con Errores y Advertencias

```json
{
  "registros_validos": 985,
  "registros_advertencias": 2,
  "registros_errores": 13,
  "errores": [
    {
      "mensaje": "[FORMATO_CUIL] CUIL inválido: '123' (debe tener 11 dígitos numéricos)",
      "linea": 10,
      "columna": 1
    },
    {
      "mensaje": "[CUIL_DUPLICADO] CUIL 20123456789 está duplicado 3 veces en el archivo",
 "linea": 25,
      "columna": 1
    },
  {
      "mensaje": "[CAMPOS_OBLIGATORIOS] CUIL N/A: Campo CUIL es obligatorio",
      "linea": 50,
      "columna": 1
    },
    {
      "mensaje": "[CAMPOS_OBLIGATORIOS] CUIL 20999888777: Todas las remuneraciones están vacías o en cero",
      "linea": 75,
   "columna": 5
    },
    {
      "mensaje": "[REMUNERACION_POSITIVA] CUIL 20555666777: REMUNIMPONIBLE3 tiene valor negativo (-250)",
      "linea": 120,
      "columna": 7
    }
  ],
  "advertencias": [
    {
      "mensaje": "[PERIODO_FUTURO] CUIL 20123123123: Período 2025-12 es futuro",
    "linea": 200,
      "columna": 3
    },
    {
   "mensaje": "[CONSISTENCIA_TIPO_LIQUIDACION] CUIL 20456456456: Bonus en 2024-05 (inusual, verificar)",
      "linea": 350,
"columna": 4
    }
  ]
}
```

---

## ?? Listar Reglas de Validación

### Request

```http
GET /api/novedades/reglas-validacion HTTP/1.1
Host: localhost:5000
```

### Response

```json
{
  "total": 6,
  "reglas": [
    {
      "nombre": "FORMATO_CUIL",
      "descripcion": "Verifica que el CUIL tenga exactamente 11 dígitos numéricos",
      "tipo": "Error",
      "orden": 5
    },
    {
      "nombre": "CAMPOS_OBLIGATORIOS",
    "descripcion": "Verifica que los campos obligatorios (CUIL, Período, Remuneraciones) tengan valores",
   "tipo": "Error",
      "orden": 8
    },
    {
      "nombre": "CUIL_DUPLICADO",
"descripcion": "Verifica que no existan CUILs duplicados dentro del mismo archivo",
      "tipo": "Error",
      "orden": 10
    },
    {
      "nombre": "REMUNERACION_POSITIVA",
   "descripcion": "Verifica que todas las remuneraciones sean valores mayores o iguales a cero",
      "tipo": "Error",
      "orden": 20
    },
    {
  "nombre": "PERIODO_FUTURO",
      "descripcion": "Advierte si hay registros con períodos futuros a la fecha actual",
      "tipo": "Advertencia",
 "orden": 30
    },
    {
      "nombre": "CONSISTENCIA_TIPO_LIQUIDACION",
      "descripcion": "Verifica que el tipo de liquidación sea consistente con el período (ej: aguinaldo en Jun/Dic)",
      "tipo": "Advertencia",
      "orden": 35
    }
  ]
}
```

---

## ? Errores Comunes

### Error: FlowId Expirado

```http
POST /api/novedades/validar-archivo/12345?flowId=expired-flow-id
```

```json
{
  "error": "El flujo 'expired-flow-id' expiró o es inválido. La tabla temporal no está disponible."
}
```

### Error: Sin FlowId (GTT Vacía)

```http
POST /api/novedades/validar-archivo/12345
```

```json
{
  "error": "No se encontraron registros en la tabla temporal. Verifique que el archivo fue cargado correctamente."
}
```

### Error: Archivo No Encontrado

```http
POST /api/novedades/upload
```

```json
{
  "error": "No se recibió archivo."
}
```

### Error: Tipo Novedad Inválido

```http
POST /api/novedades/upload
Content-Type: multipart/form-data

file: archivo.txt
tipoNovedad: 0
```

```json
{
  "error": "No se recibió el tipo de novedad."
}
```

---

## ? Crear Hoja (con FlowId)

### Request

```http
POST /api/novedades/crear-hoja HTTP/1.1
Host: localhost:5000
Content-Type: application/json

{
  "idArchivo": 12345,
  "flowId": "550e8400-e29b-41d4-a716-446655440000",
  "tipoNovedad": 1,
  "grupoAdicional": 0,
  "tipoLiquidacion": 1,
  "cantidadRegistros": 1000,
  "periodo": "2024-01-01T00:00:00",
  "idRep": 5
}
```

### Response - Éxito

```json
{
  "id_archivo": 12345,
  "tipo_novedad": 1,
  "grupo_adicional": 0,
  "tipo_liquidacion": 1,
  "nro_hoja": 789
}
```

### Response - Error (FlowId Expirado)

```json
{
  "error": "El flujo indicado expiró o es inválido. Vuelva a cargar el archivo."
}
```

---

## ?? Flujo Completo con Respuestas

### Paso 1: Upload con Auto-validación

**Request:**
```http
POST /api/novedades/upload
file: novedades.txt
tipoNovedad: 1
autoValidar: true
```

**Response:**
```json
{
  "id_archivo": 12345,
  "flow_id": "550e8400-...",
  "cantidad_registros": 1000,
  "validaciones": {
    "registros_validos": 995,
    "registros_errores": 5,
  "errores": [...]
  }
}
```

### Paso 2: Análisis en Frontend

```javascript
if (data.validaciones.registros_errores > 0) {
  // Mostrar tabla de errores
  console.table(data.validaciones.errores);
  alert('Corrija los errores antes de continuar');
  return;
}

if (data.validaciones.registros_advertencias > 0) {
  // Mostrar advertencias pero permitir continuar
  const continuar = confirm(
    `Hay ${data.validaciones.registros_advertencias} advertencias. ¿Continuar?`
  );
  if (!continuar) return;
}
```

### Paso 3: Crear Hoja

**Request:**
```http
POST /api/novedades/crear-hoja
{
  "idArchivo": 12345,
  "flowId": "550e8400-...",
  ...
}
```

**Response:**
```json
{
  "nro_hoja": 789,
  "id_archivo": 12345,
  ...
}
```

---

## ?? Formato de Errores por Regla

### FORMATO_CUIL
```json
{
  "mensaje": "[FORMATO_CUIL] CUIL inválido: '123' (debe tener 11 dígitos numéricos)",
  "linea": 10,
  "columna": 1
}
```

### CUIL_DUPLICADO
```json
{
  "mensaje": "[CUIL_DUPLICADO] CUIL 20123456789 está duplicado 2 veces en el archivo",
  "linea": 15,
  "columna": 1
}
```

### CAMPOS_OBLIGATORIOS
```json
{
  "mensaje": "[CAMPOS_OBLIGATORIOS] CUIL 20987654321: Campo PERIODO es obligatorio",
  "linea": 28,
  "columna": 3
}
```

### REMUNERACION_POSITIVA
```json
{
  "mensaje": "[REMUNERACION_POSITIVA] CUIL 20111222333: REMUNIMPONIBLE1 tiene valor negativo (-1000)",
  "linea": 42,
  "columna": 5
}
```

### PERIODO_FUTURO
```json
{
  "mensaje": "[PERIODO_FUTURO] CUIL 20777888999: Período 2025-06 es futuro",
  "linea": 100,
  "columna": 3
}
```

### CONSISTENCIA_TIPO_LIQUIDACION
```json
{
"mensaje": "[CONSISTENCIA_TIPO_LIQUIDACION] CUIL 20333444555: Aguinaldo declarado en 2024-03 (debe ser Jun o Dic)",
  "linea": 234,
  "columna": 4
}
```

### CODIGO_ACTIVIDAD_VALIDO
```json
{
  "mensaje": "[CODIGO_ACTIVIDAD_VALIDO] CUIL 20123456789: CODACTIVIDAD = 99 no es válido (valores permitidos: 19, 32, 45, 52, 65, 76, 77, 85, 913)",
  "linea": 25,
  "columna": 8
}
```

### CODIGO_CONDICION_VALIDO
```json
{
  "mensaje": "[CODIGO_CONDICION_VALIDO] CUIL 20123456789: CODCONDICION = 3 no es válido (valores permitidos: 1, 2, 5)",
  "linea": 18,
  "columna": 7
}
```

### TIPO_EMPRESA_VALIDO
```json
{
  "mensaje": "[TIPO_EMPRESA_VALIDO] CUIL 20123456789: TIPOEMPRESA = 'X' no es válido (valores permitidos: '3', 'G')",
  "linea": 30,
  "columna": 38
}
```

### OBRA_SOCIAL_NACIONAL_REQUERIDA
```json
{
  "mensaje": "[OBRA_SOCIAL_NACIONAL_REQUERIDA] CUIL 20123456789: CODACTIVIDAD 46 requiere REMUNIMPONIBLE4 (Obra Social Nacional) con valor positivo (actual: 0)",
  "linea": 35,
  "columna": 24
}
```

---

## ?? Testing con Curl

### Upload con Auto-validación

```bash
curl -X POST http://localhost:5000/api/novedades/upload \
  -F "file=@novedades.txt" \
  -F "tipoNovedad=1" \
-F "autoValidar=true" \
  | jq .
```

### Validar con FlowId

```bash
FLOW_ID="550e8400-e29b-41d4-a716-446655440000"

curl -X POST "http://localhost:5000/api/novedades/validar-archivo/12345?flowId=${FLOW_ID}" \
  | jq .
```

### Listar Reglas

```bash
curl http://localhost:5000/api/novedades/reglas-validacion \
  | jq '.reglas | sort_by(.orden)'
```

### Crear Hoja

```bash
curl -X POST http://localhost:5000/api/novedades/crear-hoja \
  -H "Content-Type: application/json" \
  -d '{
    "idArchivo": 12345,
    "flowId": "550e8400-e29b-41d4-a716-446655440000",
    "tipoNovedad": 1,
    "grupoAdicional": 0,
    "tipoLiquidacion": 1,
    "cantidadRegistros": 1000,
    "periodo": "2024-01-01T00:00:00",
    "idRep": 5
  }' \
  | jq .
```

---

**?? Tip:** Usar `| jq .` para formatear JSON en terminal
