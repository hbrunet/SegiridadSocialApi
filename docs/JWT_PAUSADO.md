# ⏸️ Implementación JWT Pausada Temporalmente

## 📊 ESTADO ACTUAL

La implementación de autenticación JWT se ha **pausado temporalmente** debido a problemas de compatibilidad con .NET 8 y la validación de firma.

---

## ❌ PROBLEMA ENCONTRADO

Al intentar deshabilitar la validación de firma JWT en .NET 8, encontramos el siguiente error recurrente:

```
IDX10506: Signature validation failed. The user defined 'Delegate' specified on TokenValidationParameters 
did not return a 'Microsoft.IdentityModel.JsonWebTokens.JsonWebToken', 
but returned a 'System.IdentityModel.Tokens.Jwt.JwtSecurityToken'
```

### **Causa Raíz**

ASP.NET Core 8 cambió la implementación interna de validación de JWT:
- **Antes (.NET 6/7)**: Usaba `JwtSecurityToken` y `JwtSecurityTokenHandler`
- **Ahora (.NET 8)**: Usa `JsonWebToken` y `JsonWebTokenHandler`

Los métodos para deshabilitar la validación de firma que funcionaban en versiones anteriores ya no son compatibles.

---

## 🔧 INTENTOS REALIZADOS

### **Intent 1: SignatureValidator con JwtSecurityToken**
```csharp
SignatureValidator = (token, parameters) => new JwtSecurityToken(token)
```
**Resultado**: Error IDX10506

### **Intento 2: TokenHandlers.Clear() + JsonWebTokenHandler**
```csharp
options.TokenHandlers.Clear();
options.TokenHandlers.Add(new JsonWebTokenHandler());
```
**Resultado**: Error IDX10500 (No security keys provided)

### **Intento 3: Clave Dummy + RequireSignedTokens = false**
```csharp
ValidateIssuerSigningKey = false,
RequireSignedTokens = false,
IssuerSigningKey = dummyKey,
```
**Resultado**: Error IDX10506

---

## 📝 ARCHIVOS CREADOS

Todos los archivos de autenticación están listos y funcionando correctamente, **excepto** la configuración en `Startup.cs`:

### **✅ Funcionales:**
- `Services/AuthService.cs` - Consume API externa OK
- `Services/Interfaces/IAuthService.cs`
- `Services/DTOs/Auth/*.cs` - Todos los DTOs
- `Services/Options/AuthOptions.cs`
- `Services/Options/JwtOptions.cs`
- `Controllers/AuthController.cs` - Endpoints de login
- `appsettings.json` - Configuración

### **❌ Pendiente:**
- `Startup.cs` - Configuración JWT (problemas con .NET 8)

---

## 🔄 SOLUCIONES ALTERNATIVAS

### **Opción A: Obtener la Clave Secreta Real** ⭐ RECOMENDADA

Si obtienes la clave secreta real de la API de `http://svr-v-patri:85`, la validación de firma funcionará sin problemas:

```csharp
// En Startup.cs
services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
{
   options.TokenValidationParameters = new TokenValidationParameters
      {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true, // ✅ Habilitado
        ValidIssuer = "DGS.Services.WebApi.Security",
            ValidAudience = "DGS.Services.Clients",
  IssuerSigningKey = new SymmetricSecurityKey(
   Encoding.UTF8.GetBytes("LA_CLAVE_SECRETA_REAL"))
  };
    });
```

**Pasos:**
1. Contactar al equipo de DGS que mantiene `svr-v-patri:85`
2. Solicitar la clave secreta usada para firmar JWT
3. Actualizar `appsettings.json`:
```json
{
  "Jwt": {
    "SecretKey": "LA_CLAVE_SECRETA_REAL"
  }
}
```
4. Agregar configuración JWT en `Startup.cs`

---

### **Opción B: Usar Middleware Personalizado**

Crear un middleware simple que extraiga y valide manualmente el token sin usar `JwtBearer`:

```csharp
// Middleware/JwtMiddleware.cs
public class JwtMiddleware
{
    private readonly RequestDelegate _next;
    
    public async Task Invoke(HttpContext context)
    {
        var token = context.Request.Headers["Authorization"]
     .ToString()
  .Replace("Bearer ", string.Empty);
        
        if (!string.IsNullOrEmpty(token))
        {
    var handler = new JwtSecurityTokenHandler();
   var jwtToken = handler.ReadJwtToken(token);
 
       // Validar issuer, audience, lifetime manualmente
 // Crear ClaimsPrincipal y asignarlo al contexto
        }
        
        await _next(context);
    }
}
```

**Pros**: Control total, sin dependencia de .NET 8  
**Contras**: Más código custom, más mantenimiento

---

### **Opción C: Downgrade a .NET 7**

Volver a .NET 7 donde la validación de JWT era más flexible.

**Pros**: Solución inmediata  
**Contras**: Perder características de .NET 8

---

### **Opción D: No Usar Autenticación (Temporalmente)**

Para desarrollo interno, confiar en la red interna y no implementar autenticación aún.

**Pros**: Simplicidad, enfoque en features  
**Contras**: Sin seguridad (solo OK para desarrollo interno)

---

## 📚 DOCUMENTACIÓN COMPLETA

Toda la documentación está lista y actualizada:

- `docs/AUTENTICACION_JWT.md` - Guía completa de uso
- `docs/IMPLEMENTACION_AUTH_JWT.md` - Resumen técnico
- `docs/FIX_PROXY_AUTH_ERROR.md` - Solución de proxy
- `docs/JWT_SIN_VALIDACION_FIRMA.md` - Intentos de deshabilitación
- `docs/JWT_PAUSADO.md` - Este documento

---

## 🚀 PLAN DE RETOMADA

Cuando decidas retomar la implementación:

### **Paso 1: Decidir enfoque**
- [ ] ¿Vas a obtener la clave secreta? → Opción A
- [ ] ¿Prefieres middleware custom? → Opción B
- [ ] ¿Downgrade a .NET 7? → Opción C

### **Paso 2: Implementar configuración en Startup.cs**
- [ ] Agregar usings necesarios
- [ ] Configurar `AddAuthentication` y `AddJwtBearer`
- [ ] Agregar `UseAuthentication()` y `UseAuthorization()`

### **Paso 3: Probar**
- [ ] Login en `/api/auth/login`
- [ ] Endpoint protegido `/api/auth/me`
- [ ] Verificar logs

### **Paso 4: Proteger endpoints**
- [ ] Agregar `[Authorize]` a controladores que lo requieran
- [ ] Probar acceso con y sin token

---

## 💡 RECOMENDACIÓN FINAL

Para **aplicaciones internas** en red corporativa:

1. **Corto plazo**: Obtener la clave secreta (Opción A)
   - Es la solución más limpia y estándar
   - Aprovecha todo el stack de seguridad de ASP.NET Core
   - Solo requiere una configuración simple

2. **Si no es posible obtener la clave**: Middleware custom (Opción B)
   - Control total sobre la validación
   - Compatible con cualquier versión de .NET
   - Más trabajo pero más flexible

---

## 📞 CONTACTO

Para obtener la clave secreta JWT:
- **Equipo**: DGS - Mantenimiento de `svr-v-patri:85`
- **API**: http://svr-v-patri:85/api/auth/login
- **Issuer**: `DGS.Services.WebApi.Security`
- **Audience**: `DGS.Services.Clients`

---

## ✅ LO QUE SÍ FUNCIONA

- ✅ Login contra API externa (`/api/auth/login`)
- ✅ Obtención de token JWT
- ✅ Extracción de información del usuario
- ✅ HttpClient con soporte de proxy
- ✅ Logging completo
- ✅ Manejo de errores

## ❌ LO QUE FALTA

- ❌ Validación automática del token en endpoints protegidos
- ❌ Middleware de autenticación configurado
- ❌ Attribute `[Authorize]` funcionando

---

**Fecha**: 17 de Diciembre de 2025  
**Estado**: ⏸️ Pausado temporalmente  
**Decisión**: Retomar cuando se defina el enfoque (clave real vs middleware custom)  
**Prioridad**: Media (no bloqueante para desarrollo)

---

## 🔗 REFERENCIAS

- [ASP.NET Core 8 Breaking Changes - Security Token](https://learn.microsoft.com/en-us/dotnet/core/compatibility/aspnet-core/8.0/securitytoken-events)
- [Microsoft.IdentityModel.JsonWebTokens](https://learn.microsoft.com/en-us/dotnet/api/microsoft.identitymodel.jsonwebtokens)
- [JWT Bearer Authentication](https://learn.microsoft.com/en-us/aspnet/core/security/authentication/)
