# 🔑 Verificar y Obtener la Clave Secreta JWT Correcta

## ❌ **PROBLEMA ACTUAL**

La clave secreta en `appsettings.json` NO coincide con la que usa la API externa para firmar el token:

```json
{
  "Jwt": {
    "SecretKey": "23e2054bf1914841afb7344646446eef5e2bd454f0414da4bbb8d32bfdd34c9f"
  }
}
```

**Resultado**: Error `SecurityTokenSignatureKeyNotFoundException`

---

## ✅ **SOLUCIÓN**

Necesitas obtener la **clave secreta correcta** del equipo que mantiene la API de `http://svr-v-patri:85`.

---

## 📋 **PASO 1: Verificar si Tienes Acceso al Código de la API Externa**

### **Si tienes acceso al código fuente:**

1. Busca en el proyecto de `svr-v-patri:85` el archivo de configuración (usualmente `appsettings.json` o `web.config`)

2. Busca una sección similar a esta:
```json
{
  "Jwt": {
    "SecretKey": "LA_CLAVE_REAL",
    "Issuer": "DGS.Services.WebApi.Security",
    "Audience": "DGS.Services.Clients"
  }
}
```

3. **Copia exactamente** el valor de `SecretKey`

---

## 📋 **PASO 2: Contactar al Equipo de DGS**

Si no tienes acceso al código, contacta al equipo y proporciona esta información:

```
Asunto: Solicitud de Clave Secreta JWT para Integración

Hola,

Estoy integrando nuestra API de Seguridad Social con la API de autenticación ubicada en:
http://svr-v-patri:85/api/auth/login

Para validar los tokens JWT que genera su API, necesito la clave secreta (SecretKey) 
utilizada para firmar los tokens.

Información del token que recibo:
- Issuer: DGS.Services.WebApi.Security
- Audience: DGS.Services.Clients
- Algoritmo: HS256

¿Pueden compartirme la clave secreta JWT?

Gracias!
```

---

## 📋 **PASO 3: Verificar la Clave con jwt.io**

Una vez que tengas una posible clave:

1. Ve a https://jwt.io
2. Pega el token JWT que obtienes del login
3. En la sección "VERIFY SIGNATURE":
   - Selecciona "HS256"
   - Pega la clave secreta
4. Si aparece "✅ Signature Verified" - **La clave es correcta!**
5. Si aparece "❌ Invalid Signature" - La clave no coincide

---

## 📋 **PASO 4: Actualizar appsettings.json**

Una vez verificada la clave:

```json
{
  "Jwt": {
    "SecretKey": "LA_CLAVE_CORRECTA_AQUI"
  }
}
```

---

## 📋 **PASO 5: Reiniciar y Probar**

```sh
dotnet run
```

Deberías ver:
```
[INF] ✅ Autenticación JWT configurada correctamente
```

Luego prueba:
```sh
curl -X POST https://localhost:5001/api/auth/login ...
TOKEN="<token>"
curl -X GET https://localhost:5001/api/auth/me -H "Authorization: Bearer $TOKEN"
```

**Logs esperados:**
```
[INF] ✅ Token JWT validado correctamente. Usuario: h_dbrunet (ID: 105)
```

---

## 🔍 **ALTERNATIVA: Decodificar el Token para Investigar**

Si quieres investigar más, decodifica el token en https://jwt.io:

### **Header del token:**
```json
{
  "alg": "HS256",
  "typ": "JWT"
}
```

- `alg: HS256` confirma que usa HMAC-SHA256
- Esto significa que SÍ hay una clave secreta simétrica

### **Payload del token:**
```json
{
  "unique_name": "h_dbrunet",
  "user_id": "105",
  "iss": "DGS.Services.WebApi.Security",
  "aud": "DGS.Services.Clients",
  "exp": 1765901146
}
```

---

## ⚠️ **IMPORTANTE**

1. La clave secreta es **sensible** - NO la commitees al repositorio
2. En producción, usa **variables de entorno**:
```bash
# Linux/Mac
export JWT_SECRET_KEY="la_clave_real"

# Windows
set JWT_SECRET_KEY=la_clave_real
```

3. Actualiza `appsettings.json`:
```json
{
  "Jwt": {
  "SecretKey": "${JWT_SECRET_KEY}"
  }
}
```

---

## 🎯 **RESUMEN**

| Paso | Acción | Estado |
|------|--------|--------|
| 1 | ✅ JWT configurado correctamente | Completado |
| 2 | ⏳ Obtener clave secreta correcta | **PENDIENTE** |
| 3 | ⏳ Actualizar appsettings.json | Pendiente |
| 4 | ⏳ Verificar con jwt.io | Pendiente |
| 5 | ⏳ Probar autenticación | Pendiente |

---

## 📞 **Contactos Posibles**

- Administrador de `svr-v-patri` (servidor)
- Equipo de desarrollo de DGS
- Responsable de la API de autenticación
- Equipo de seguridad/infraestructura

---

**Una vez que tengas la clave correcta, la autenticación JWT funcionará perfectamente!** ✅
