# 🔐 Guía de Autenticación JWT - Seguridad Social API

## 📋 Resumen

Se ha implementado autenticación JWT consumiendo la API externa de DGS (`http://svr-v-patri:85`).

---

## 🎯 Características Implementadas

✅ **Login contra API externa** - `/api/auth/login`  
✅ **Validación local de JWT** - Sin latencia  
✅ **Extracción de información del usuario** - `/api/auth/me`  
✅ **Protección de endpoints** - Con `[Authorize]`  
✅ **Reintentos automáticos** - Con Polly (3 intentos)  
✅ **Logging completo** - Auditoría de autenticación  
✅ **Manejo de errores robusto** - Responses consistentes  

---

## 🔧 Configuración Requerida

### 1. **Obtener la Clave Secreta JWT**

⚠️ **IMPORTANTE**: Debes obtener la clave secreta compartida que usa la API de autenticación para firmar los tokens JWT.

Actualiza en `appsettings.json`:
```json
{
  "Jwt": {
    "SecretKey": "LA_CLAVE_SECRETA_REAL_AQUI_MINIMO_32_CARACTERES"
  }
}
```

### 2. **Verificar Conectividad**

Asegúrate de que la API puede acceder a `http://svr-v-patri:85`:
```bash
curl -X POST http://svr-v-patri:85/api/auth/login \
  -H "Content-Type: application/json" \
  -d '{"user_name":"test","password":"test","application_id":9}'
```

---

## 🚀 Uso de la API

### **1. Login**

**Endpoint**: `POST /api/auth/login`

**Request**:
```json
{
  "user_name": "h_dbrunet",
  "password": "dbrunet123#",
  "application_id": 9
}
```

**Response Exitoso** (200 OK):
```json
{
  "success": true,
  "message": "Login successful",
  "data": {
    "user": {
  "id": 105,
      "name": "H_DBRUNET",
      "display_name": "DBRUNET",
      "status": "OPEN",
      "roles": [
        {
    "id": 121,
   "name": "Procesador de Hoja",
          "application_id": 9,
  "application_name": "DGS Intranet"
        }
      ]
    },
    "token": "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9...",
    "token_type": "Bearer",
    "expires_in": 3600,
    "expires_at": "2025-12-16T16:05:46Z"
  },
  "timestamp": "2025-12-16T12:05:46Z"
}
```

**Response de Error** (401 Unauthorized):
```json
{
  "success": false,
  "message": "Credenciales inválidas",
  "timestamp": "2025-12-16T12:05:46Z"
}
```

---

### **2. Usar el Token en Requests**

Una vez obtenido el token, inclúyelo en el header `Authorization` de todas las requests:

```http
GET /api/novedades/listado-hojas
Authorization: Bearer eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9...
```

**Ejemplo con curl**:
```bash
curl -X GET https://localhost:5001/api/novedades/listado-hojas \
  -H "Authorization: Bearer eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9..."
```

**Ejemplo con JavaScript (Fetch)**:
```javascript
const token = localStorage.getItem('jwt_token');

fetch('https://localhost:5001/api/novedades/listado-hojas', {
  method: 'GET',
  headers: {
    'Authorization': `Bearer ${token}`,
    'Content-Type': 'application/json'
  }
})
.then(response => response.json())
.then(data => console.log(data));
```

---

### **3. Obtener Información del Usuario Actual**

**Endpoint**: `GET /api/auth/me`

**Headers**:
```
Authorization: Bearer {token}
```

**Response**:
```json
{
  "success": true,
  "data": {
    "id": 105,
    "name": "h_dbrunet",
"display_name": "DBRUNET"
  },
  "timestamp": "2025-12-16T12:05:46Z"
}
```

---

## 🛡️ Protección de Endpoints

### **Opción 1: Proteger todo el controlador**

```csharp
[Authorize]
[ApiController]
[Route("api/[controller]")]
public class DDJJController : ControllerBase
{
    // Todos los endpoints requieren autenticación
}
```

### **Opción 2: Endpoints individuales**

```csharp
[ApiController]
[Route("api/[controller]")]
public class NovedadesController : ControllerBase
{
    [HttpGet("public")]
    [AllowAnonymous]  // No requiere autenticación
    public IActionResult GetPublicData() { }
    
    [HttpPost("upload")]
    [Authorize]  // Requiere autenticación
    public IActionResult Upload() { }
}
```

### **Opción 3: Por roles específicos**

```csharp
[HttpDelete("{id}")]
[Authorize(Roles = "Procesador de Hoja")]
public IActionResult Delete(int id) { }
```

---

## 🔍 Códigos de Respuesta

| Código | Significado | Acción |
|--------|-------------|--------|
| **200** | OK | Token válido, request procesado |
| **401** | Unauthorized | Token inválido/expirado - Re-login necesario |
| **403** | Forbidden | Token válido pero sin permisos |
| **503** | Service Unavailable | API de auth no disponible |

---

## ⏰ Expiración del Token

- **Duración**: 1 hora (3600 segundos)
- **Clock Skew**: 5 minutos de tolerancia
- **¿Qué hacer al expirar?**: Re-login con `/api/auth/login`

**Ejemplo de manejo en frontend**:
```javascript
async function callApi(url) {
  let token = localStorage.getItem('jwt_token');
  let expiresAt = localStorage.getItem('token_expires_at');
  
  // Verificar si el token está próximo a expirar (5 min antes)
  if (new Date(expiresAt) - new Date() < 5 * 60 * 1000) {
    // Re-login
    const loginResponse = await fetch('/api/auth/login', {
      method: 'POST',
   headers: { 'Content-Type': 'application/json' },
      body: JSON.stringify({
user_name: '...',
        password: '...',
        application_id: 9
      })
    });
    
    const data = await loginResponse.json();
  token = data.data.token;
    localStorage.setItem('jwt_token', token);
    localStorage.setItem('token_expires_at', data.data.expires_at);
  }
  
  // Hacer request con token actualizado
  return fetch(url, {
    headers: { 'Authorization': `Bearer ${token}` }
  });
}
```

---

## 🐛 Troubleshooting

### **Error: "JWT SecretKey no configurada"**
**Solución**: Actualiza `appsettings.json` con la clave secreta real.

### **Error: "Servicio de autenticación no disponible"**
**Causa**: No se puede conectar a `http://svr-v-patri:85`  
**Solución**:
- Verificar conectividad de red
- Verificar que la API externa esté activa
- Revisar logs en `logs/log-{fecha}.txt`

### **Error: 401 Unauthorized en requests protegidos**
**Causas posibles**:
1. Token expirado (duración: 1 hora)
2. Token inválido
3. Clave secreta incorrecta en `appsettings.json`

**Solución**: Re-login para obtener un nuevo token.

### **Error: "Token inválido" al decodificar**
**Causa**: La clave secreta en `appsettings.json` no coincide con la usada por la API externa  
**Solución**: Contactar al equipo de DGS para obtener la clave correcta.

---

## 📝 Claims del JWT

El token incluye los siguientes claims:

| Claim | Descripción | Ejemplo |
|-------|-------------|---------|
| `unique_name` | Nombre de usuario | `h_dbrunet` |
| `sub` | Subject (nombre de usuario) | `h_dbrunet` |
| `user_id` | ID del usuario | `105` |
| `display_name` | Nombre para mostrar | `DBRUNET` |
| `application_id` | ID de aplicación | `9` |
| `exp` | Fecha de expiración (Unix timestamp) | `1765901146` |
| `iss` | Issuer | `DGS.Services.WebApi.Security` |
| `aud` | Audience | `DGS.Services.Clients` |

---

## 🔐 Seguridad

### **Buenas Prácticas**

✅ **NO exponer contraseñas en logs**  
✅ **NO guardar contraseñas en localStorage** (solo el token)  
✅ **Usar HTTPS en producción**  
✅ **Validar expiración del token en el frontend**  
✅ **Implementar logout para limpiar tokens**  
✅ **Rotar la clave secreta periódicamente**  

### **NO hacer**

❌ NO enviar tokens por URL query params  
❌ NO compartir la clave secreta en el código fuente  
❌ NO usar tokens expirados  
❌ NO guardar tokens en cookies sin HttpOnly flag  

---

## 📊 Logs y Auditoría

Los eventos de autenticación se registran en:
- `logs/log-{fecha}.txt`

**Eventos registrados**:
- ✅ Login exitoso
- ⚠️ Login fallido
- ✅ Token validado
- ⚠️ Autenticación fallida
- ⚠️ Token expirado

**Ejemplo de log**:
```
[12:05:46 INF] Intentando login para usuario h_dbrunet en aplicación 9
[12:05:46 INF] Login exitoso para usuario h_dbrunet. Token expira en 3600 segundos
[12:10:00 INF] Token validado exitosamente. Usuario: h_dbrunet (ID: 105)
```

---

## 🧪 Testing

### **Test manual con Postman/Insomnia**

1. **Login**:
```
POST https://localhost:5001/api/auth/login
Content-Type: application/json

{
  "user_name": "h_dbrunet",
  "password": "dbrunet123#",
  "application_id": 9
}
```

2. **Copiar el token** de `data.token`

3. **Usar en requests protegidos**:
```
GET https://localhost:5001/api/novedades/listado-hojas
Authorization: Bearer {token_copiado}
```

---

## 📞 Contacto

**Equipo de Soporte DGS**  
Para obtener la clave secreta JWT o reportar problemas con la API de autenticación.

**Logs de la API**  
Ubicación: `E:\source\DDJJ\backend-dotnet\SeguridadSocialApi\logs\`

---

## ✅ Checklist de Implementación

- [ ] Obtener la clave secreta JWT del equipo de DGS
- [ ] Actualizar `Jwt:SecretKey` en `appsettings.json`
- [ ] Restaurar paquetes NuGet (`dotnet restore`)
- [ ] Compilar el proyecto (`dotnet build`)
- [ ] Probar login en `/api/auth/login`
- [ ] Agregar `[Authorize]` a endpoints que lo requieran
- [ ] Actualizar frontend para manejar tokens
- [ ] Implementar manejo de expiración en frontend
- [ ] Revisar logs de autenticación
- [ ] Documentar para el equipo

---

## 🚀 Próximos Pasos (Opcional)

1. **Refresh Token**: Solicitar implementación en API externa
2. **Roles personalizados**: Usar claims de roles para autorización granular
3. **Rate Limiting**: Proteger endpoint de login contra fuerza bruta
4. **Logout**: Implementar blacklist de tokens (requiere Redis o DB)
5. **2FA**: Agregar segundo factor de autenticación

---

**Fecha de última actualización**: 16 de Diciembre de 2025
