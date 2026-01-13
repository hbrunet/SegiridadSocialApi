# 📘 Guía de Swagger - Seguridad Social API

## 🚀 Acceso a Swagger

### URLs de Acceso

Una vez que la aplicación esté corriendo, puedes acceder a Swagger en:

**Desarrollo Local:**
```
http://localhost:5000/swagger
```

**Si usas HTTPS:**
```
https://localhost:5001/swagger
```

**Servidor de Prueba/Producción:**
```
http://<ip-o-dominio-servidor>:5000/swagger
```

---

## 📖 Características de Swagger UI

Swagger proporciona:

✅ **Documentación interactiva** de todos los endpoints  
✅ **Prueba de endpoints** directamente desde el navegador  
✅ **Esquemas de datos** (Request/Response)  
✅ **Autenticación JWT** integrada  
✅ **Validaciones** y tipos de datos  

---

## 🔐 Autenticación en Swagger

### Paso 1: Obtener Token JWT

1. En Swagger UI, busca el endpoint **POST /api/auth/login**
2. Click en "Try it out"
3. Ingresa las credenciales:
   ```json
   {
     "user_name": "tu_usuario",
     "password": "tu_contraseña"
   }
   ```
4. Click en "Execute"
5. Copia el **token** del response (campo `data.token`)

### Paso 2: Autorizar Swagger

1. Click en el botón **"Authorize"** (candado) en la parte superior derecha
2. Ingresa: `Bearer <tu-token-jwt>`
   ```
   Ejemplo:
   Bearer eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9...
   ```
3. Click en **"Authorize"**
4. Click en **"Close"**

Ahora todos los endpoints protegidos se ejecutarán con tu token automáticamente.

---

## 🎯 Endpoints Principales

### Autenticación

| Método | Endpoint | Descripción | Requiere Auth |
|--------|----------|-------------|---------------|
| POST | `/api/auth/login` | Iniciar sesión | ❌ No |
| GET | `/api/auth/me` | Obtener usuario actual | ✅ Sí |

### Configuración

| Método | Endpoint | Descripción | Requiere Auth |
|--------|----------|-------------|---------------|
| GET | `/api/configuracion/tipos-novedad` | Listar tipos de novedad | ✅ Sí |
| GET | `/api/configuracion/tipos-liquidacion` | Listar tipos de liquidación | ✅ Sí |
| GET | `/api/configuracion/grupos-adicionales` | Listar grupos adicionales | ✅ Sí |
| GET | `/api/configuracion/reparticiones` | Listar reparticiones | ✅ Sí |

### Novedades (Gestión de Archivos y Hojas)

| Método | Endpoint | Descripción | Requiere Auth |
|--------|----------|-------------|---------------|
| POST | `/api/novedades/upload` | Cargar archivo | ✅ Sí |
| POST | `/api/novedades/validar-archivo/{id}` | Validar archivo cargado | ✅ Sí |
| POST | `/api/novedades/crear-hoja` | Crear hoja de liquidación | ✅ Sí |
| GET | `/api/novedades/listado-hojas` | Listar hojas | ✅ Sí |
| POST | `/api/novedades/procesar-hoja/{id}` | Procesar hoja | ✅ Sí |
| PUT | `/api/novedades/anular-hoja/{id}` | Anular hoja | ✅ Sí |
| GET | `/api/novedades/reglas-validacion` | Ver reglas de validación | ✅ Sí |

### DDJJ (Declaraciones Juradas)

| Método | Endpoint | Descripción | Requiere Auth |
|--------|----------|-------------|---------------|
| GET | `/api/ddjj/lista-trabajadores/{nroHoja}` | Listar trabajadores de una hoja | ✅ Sí |
| GET | `/api/ddjj/buscar-trabajador` | Buscar trabajador por CUIL | ✅ Sí |

---

## 🧪 Ejemplos de Uso

### Ejemplo 1: Login

**Request:**
```http
POST /api/auth/login
Content-Type: application/json

{
  "user_name": "admin",
  "password": "admin123"
}
```

**Response:**
```json
{
  "success": true,
  "message": "Login exitoso",
  "data": {
    "token": "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9...",
    "user": {
    "id": 1,
"name": "admin",
   "display_name": "Administrador"
    }
  }
}
```

### Ejemplo 2: Cargar Archivo (Multipart Form)

**Request:**
```http
POST /api/novedades/upload
Authorization: Bearer <tu-token>
Content-Type: multipart/form-data

file: [archivo.txt]
tipoNovedad: 1
autoValidar: true
```

**Response:**
```json
{
  "file_name": "archivo.txt",
  "cantidad_registros": 150,
  "error_ora": 0,
  "mensaje": "Archivo cargado exitosamente",
  "id_archivo": 12345,
  "sum_rem1": 1500000.50,
  "sum_rem2": 250000.00,
  "sum_rem3": 100000.00,
  "flow_id": "abc123def456",
  "validaciones": {
    // ... resultados de validación
  }
}
```

### Ejemplo 3: Crear Hoja

**Request:**
```http
POST /api/novedades/crear-hoja
Authorization: Bearer <tu-token>
Content-Type: application/json

{
  "id_archivo": 12345,
  "tipo_novedad": 1,
  "grupo_adicional": 2,
  "tipo_liquidacion": 3,
  "cantidad_registros": 150,
  "periodo": "2024-01-01T00:00:00",
  "id_rep": 5,
  "flow_id": "abc123def456"
}
```

---

## 🛠️ Configuración Avanzada

### Habilitar Comentarios XML (Opcional)

Para mostrar descripciones de métodos en Swagger:

1. **Editar `SeguridadSocialApi.csproj`:**
   ```xml
   <PropertyGroup>
     <GenerateDocumentationFile>true</GenerateDocumentationFile>
     <NoWarn>$(NoWarn);1591</NoWarn>
   </PropertyGroup>
   ```

2. **Descomentar en `Startup.cs`:**
   ```csharp
   var xmlFile = $"{Assembly.GetExecutingAssembly().GetName().Name}.xml";
   var xmlPath = Path.Combine(AppContext.BaseDirectory, xmlFile);
   if (File.Exists(xmlPath))
   {
       options.IncludeXmlComments(xmlPath);
   }
   ```

### Cambiar Ruta de Swagger

Por defecto: `/swagger`

Para cambiar a `/api-docs`:
```csharp
app.UseSwaggerUI(options =>
{
    options.RoutePrefix = "api-docs"; // Cambiar aquí
});
```

### Deshabilitar Swagger en Producción

En `Startup.cs`, agregar condición:
```csharp
public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
{
    // Solo habilitar Swagger en Development
    if (env.IsDevelopment())
    {
  app.UseSwagger();
        app.UseSwaggerUI(options => { /*...*/ });
    }
    
    // ...resto del código
}
```

---

## 📊 Esquemas de Datos

Swagger muestra automáticamente los esquemas de:

- **LoginRequest**: `user_name`, `password`
- **AuthResponse**: `success`, `message`, `data`, `timestamp`
- **CrearHojaRequest**: `id_archivo`, `tipo_novedad`, etc.
- **UploadResponse**: `file_name`, `cantidad_registros`, etc.
- **ValidacionArchivoDto**: Resultados de validaciones
- Y muchos más...

---

## 🐛 Troubleshooting

### ❓ Swagger no carga / Error 404

**Problema:** Accedes a `/swagger` y no carga.

**Soluciones:**
1. Verifica que la app esté corriendo:
   ```bash
   dotnet run
   ```
2. Verifica la URL correcta:
   - Desarrollo: `http://localhost:5000/swagger`
   - No uses `/swagger/index.html`, solo `/swagger`

3. Verifica que no esté deshabilitado en producción:
   ```csharp
   // En Startup.cs, debería estar SIN condición de environment
   app.UseSwagger();
   app.UseSwaggerUI(/*...*/);
   ```

### ❓ "401 Unauthorized" en endpoints protegidos

**Problema:** Al probar un endpoint, retorna 401.

**Solución:**
1. Primero ejecuta `/api/auth/login` y obtén el token
2. Click en **"Authorize"** y pega el token con formato:
   ```
 Bearer <tu-token-aqui>
 ```
3. Verifica que el token no haya expirado

### ❓ Error al subir archivos grandes

**Problema:** Error al probar `/api/novedades/upload` con archivos grandes.

**Solución:**
El límite está configurado en 100 MB (`RequestSizeLimit`). Si necesitas más:
```csharp
[RequestSizeLimit(200_000_000)] // 200 MB
```

---

## 📚 Referencias

- [Swagger UI](https://swagger.io/tools/swagger-ui/)
- [Swashbuckle.AspNetCore](https://github.com/domaindrivendev/Swashbuckle.AspNetCore)
- [OpenAPI Specification](https://spec.openapis.org/oas/latest.html)

---

## 🎉 ¡Listo!

Ya tienes Swagger configurado y funcionando. Accede a:

```
http://localhost:5000/swagger
```

Y comienza a explorar y probar tu API. 🚀
