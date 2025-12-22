# 🔐 Guía de Uso - Atributo [Authorize] Personalizado

## 📋 **CÓMO FUNCIONA**

El sistema de autenticación JWT funciona automáticamente con el middleware personalizado:

```
1. Request → Middleware JWT intercepta
2. Si tiene header "Authorization: Bearer {token}" → Valida el token
3. Si es válido → Asigna User.Identity al contexto
4. Controller → Si tiene [Authorize] → Verifica User.Identity.IsAuthenticated
5. Si está autenticado → Ejecuta el endpoint
6. Si NO está autenticado → Retorna 401 Unauthorized
```

---

## ✅ **ENDPOINTS PÚBLICOS (Sin Autenticación)**

No usar `[Authorize]`, el endpoint será **accesible sin token**:

```csharp
using Microsoft.AspNetCore.Mvc;
using SeguridadSocialApi.Attributes; // ← Importante: usar nuestro namespace

[ApiController]
[Route("api/[controller]")]
public class PublicController : ControllerBase
{
    // ✅ Público - Accesible sin token
    [HttpGet("health")]
    public IActionResult HealthCheck()
    {
        return Ok(new { status = "OK", timestamp = DateTime.UtcNow });
}

    // ✅ Público - Login no requiere autenticación
    [HttpPost("login")]
    public async Task<IActionResult> Login([FromBody] LoginRequest request)
    {
     // Cualquiera puede hacer login
      return Ok();
    }
}
```

---

## 🔒 **ENDPOINTS PROTEGIDOS (Con Autenticación)**

Usar `[Authorize]` para requerir token JWT:

### **Opción 1: Proteger TODO el Controlador**

```csharp
using Microsoft.AspNetCore.Mvc;
using SeguridadSocialApi.Attributes; // ← Importante

[Authorize] // ← Todo el controlador requiere autenticación
[ApiController]
[Route("api/[controller]")]
public class DDJJController : ControllerBase
{
    // 🔒 Protegido - Requiere token
    [HttpGet]
    public async Task<IActionResult> GetAll()
    {
     // Acceso a información del usuario autenticado
        var userId = User.FindFirst("user_id")?.Value;
        var userName = User.FindFirst("unique_name")?.Value;
        
        return Ok(new { message = $"Hola {userName}" });
    }

    // 🔒 Protegido - Requiere token
    [HttpPost("fusionar-datos")]
    public async Task<IActionResult> FusionarDatos([FromBody] FusionDatosRequest request)
    {
        return Ok();
  }
}
```

### **Opción 2: Proteger Endpoints Individuales**

```csharp
using Microsoft.AspNetCore.Mvc;
using SeguridadSocialApi.Attributes;

[ApiController]
[Route("api/[controller]")]
public class HojaController : ControllerBase
{
    // ✅ Público
  [HttpGet("tipos-liquidacion")]
    public IActionResult GetTiposLiquidacion()
    {
   return Ok(new[] { "Mensual", "Anual" });
}

    // 🔒 Protegido
    [HttpGet]
    [Authorize]
    public async Task<IActionResult> GetHojas()
    {
        var userName = User.FindFirst("unique_name")?.Value;
        // Solo usuarios autenticados pueden ver las hojas
        return Ok();
    }

    // 🔒 Protegido
    [HttpPost]
    [Authorize]
  public async Task<IActionResult> CrearHoja([FromBody] CrearHojaRequest request)
    {
     return Ok();
    }
}
```

---

## 👤 **ACCEDER A INFORMACIÓN DEL USUARIO**

Dentro de cualquier endpoint protegido con `[Authorize]`:

```csharp
[HttpPost("procesar")]
[Authorize]
public async Task<IActionResult> Procesar([FromBody] ProcesarRequest request)
{
    // Obtener claims del token JWT
    var userId = User.FindFirst("user_id")?.Value;
    var userName = User.FindFirst("unique_name")?.Value;
    var displayName = User.FindFirst("display_name")?.Value;

    _logger.LogInformation(
     "Usuario {UserName} (ID: {UserId}) procesando solicitud",
        userName,
     userId);

    // Usar la información del usuario en tu lógica
  var resultado = await _service.ProcesarDatos(request, int.Parse(userId!));

    return Ok(resultado);
}
```

### **Claims Disponibles:**

| Claim | Tipo | Descripción |
|-------|------|-------------|
| `user_id` | string | ID numérico del usuario |
| `unique_name` | string | Nombre de usuario (username) |
| `display_name` | string | Nombre para mostrar |
| `application_id` | string | ID de la aplicación (9) |
| `iss` | string | Issuer del token |
| `aud` | string | Audience del token |
| `exp` | long | Timestamp de expiración |

---

## 🧪 **TESTING**

### **1. Endpoint Público (Sin Token)**

```sh
curl -X GET https://localhost:5001/api/public/health -k
```

**Response esperado: 200 OK**
```json
{
  "status": "OK",
  "timestamp": "2025-12-17T15:30:00Z"
}
```

### **2. Endpoint Protegido Sin Token**

```sh
curl -X GET https://localhost:5001/api/auth/me -k
```

**Response esperado: 401 Unauthorized**
```json
{
  "success": false,
  "message": "No autorizado. Se requiere autenticación.",
  "timestamp": "2025-12-17T15:30:00Z"
}
```

### **3. Endpoint Protegido Con Token**

```sh
# Primero hacer login
curl -X POST https://localhost:5001/api/auth/login \
  -H "Content-Type: application/json" \
  -k \
  -d '{
    "user_name": "h_dbrunet",
    "password": "dbrunet123#",
    "application_id": 9
  }'

# Copiar el token de la respuesta
TOKEN="eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9..."

# Usar el token
curl -X GET https://localhost:5001/api/auth/me \
  -H "Authorization: Bearer $TOKEN" \
  -k
```

**Response esperado: 200 OK**
```json
{
  "success": true,
  "data": {
    "id": 105,
    "name": "h_dbrunet",
    "display_name": "DBRUNET"
  }
}
```

---

## 📝 **LOGS ESPERADOS**

### **Endpoint Protegido - Sin Token:**
```
[WRN] Token rechazado: No se proporcionó token
[INF] HTTP GET /api/auth/me responded 401 in 5 ms
```

### **Endpoint Protegido - Con Token Válido:**
```
[INF] ✅ Token validado. Usuario: h_dbrunet (ID: 105)
[INF] HTTP GET /api/auth/me responded 200 in 45 ms
```

### **Endpoint Protegido - Con Token Expirado:**
```
[WRN] Token rechazado: Token expirado
[INF] HTTP GET /api/auth/me responded 401 in 8 ms
```

---

## ⚠️ **IMPORTANTE: USAR EL NAMESPACE CORRECTO**

```csharp
// ✅ CORRECTO - Usar nuestro atributo personalizado
using SeguridadSocialApi.Attributes;

[Authorize] // ← Este es nuestro atributo personalizado
public class MyController : ControllerBase { }
```

```csharp
// ❌ INCORRECTO - NO usar el de Microsoft
using Microsoft.AspNetCore.Authorization;

[Authorize] // ← Este NO funciona (es el de ASP.NET Core estándar)
public class MyController : ControllerBase { }
```

---

## 🎯 **EJEMPLOS PRÁCTICOS**

### **Ejemplo 1: Controller de DDJJ**

```csharp
using Microsoft.AspNetCore.Mvc;
using SeguridadSocialApi.Attributes;

[Authorize] // Todo el controller requiere autenticación
[ApiController]
[Route("api/[controller]")]
public class DDJJController : ControllerBase
{
 private readonly IDDJJRepository _ddjjRepo;
    private readonly ILogger<DDJJController> _logger;

    [HttpGet]
    public async Task<IActionResult> GetDDJJ()
    {
        var userName = User.FindFirst("unique_name")?.Value;
        _logger.LogInformation("Usuario {UserName} consultando DDJJs", userName);
     
        var ddjjs = await _ddjjRepo.GetAllAsync();
        return Ok(ddjjs);
    }

    [HttpPost("fusionar-datos")]
    public async Task<IActionResult> FusionarDatos([FromBody] FusionDatosRequest request)
    {
     var userId = int.Parse(User.FindFirst("user_id")?.Value!);
        
  var resultado = await _ddjjRepo.FusionarDatosAsync(request, userId);
     return Ok(resultado);
    }
}
```

### **Ejemplo 2: Controller Mixto (Público + Protegido)**

```csharp
using Microsoft.AspNetCore.Mvc;
using SeguridadSocialApi.Attributes;

[ApiController]
[Route("api/[controller]")]
public class ConfiguracionController : ControllerBase
{
    // ✅ Público - Cualquiera puede obtener configuración básica
    [HttpGet("public")]
    public IActionResult GetPublicConfig()
    {
   return Ok(new { version = "1.0", environment = "production" });
    }

    // 🔒 Protegido - Solo usuarios autenticados
    [HttpGet("admin")]
    [Authorize]
    public IActionResult GetAdminConfig()
    {
    return Ok(new { secret = "confidential-data" });
    }

    // 🔒 Protegido - Solo usuarios autenticados
    [HttpPut]
 [Authorize]
    public async Task<IActionResult> UpdateConfig([FromBody] ConfigRequest request)
    {
     var userName = User.FindFirst("unique_name")?.Value;
        _logger.LogInformation("Usuario {UserName} actualizando configuración", userName);
        
        return Ok();
    }
}
```

---

## 📊 **RESUMEN DE AUTORIZACIONES**

| Endpoint | Autenticación | Uso |
|----------|---------------|-----|
| `/api/auth/login` | ❌ No requiere | Login público |
| `/api/auth/me` | ✅ Requiere `[Authorize]` | Info del usuario |
| `/api/ddjj/*` | ✅ Requiere `[Authorize]` (todo el controller) | Operaciones de DDJJ |
| `/api/config/public` | ❌ No requiere | Config pública |
| `/api/config/admin` | ✅ Requiere `[Authorize]` | Config admin |

---

## ✅ **CHECKLIST PARA NUEVOS ENDPOINTS**

Cuando crees un nuevo endpoint, pregúntate:

- [ ] **¿Requiere autenticación?**
  - ✅ Sí → Agregar `[Authorize]`
  - ❌ No → No agregar nada

- [ ] **¿Necesito información del usuario?**
  - ✅ Sí → Usar `User.FindFirst("claim_name")`
  - ❌ No → Ignorar

- [ ] **¿Importé el namespace correcto?**
  - ✅ `using SeguridadSocialApi.Attributes;`
  - ❌ NO usar `Microsoft.AspNetCore.Authorization`

---

## 🎉 **LISTO PARA USAR**

El sistema de autenticación está completamente configurado. Solo necesitas:

1. ✅ Agregar `[Authorize]` a los endpoints que lo requieren
2. ✅ Usar `User.FindFirst("claim")` para obtener info del usuario
3. ✅ Importar `using SeguridadSocialApi.Attributes;`

**¡Eso es todo!** 🚀
