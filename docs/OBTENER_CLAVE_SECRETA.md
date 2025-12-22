# 🔑 Cómo Obtener la Clave Secreta JWT

## 📋 PASO 1: Decodificar el Token

Toma el token que obtuviste del login y decodifícalo en https://jwt.io

El token se ve así:
```
eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9.eyJ1bmlxdWVfbmFtZSI6ImhfZGJydW5ldCIsInN1YiI6ImhfZGJydW5ldCIsImp0aSI6IjZlYjMzNDM1LWUxZjgtNDA5Yy1iMTA1LTZjNzUxNjUyZjIzZiIsImFwcGxpY2F0aW9uX2lkIjoiOSIsImlhdCI6MTc2NTg5NzU0NiwidXNlcl9pZCI6IjEwNSIsImRpc3BsYXlfbmFtZSI6IkRCUlVORVQiLCJuYmYiOjE3NjU4OTc1NDYsImV4cCI6MTc2NTkwMTE0NiwiaXNzIjoiREdTLlNlcnZpY2VzLldlYkFwaS5TZWN1cml0eSIsImF1ZCI6IkRHUy5TZXJ2aWNlcy5DbGllbnRzIn0.BSfM5vQREL3pr-k43kJUFxwNUig-AM9c-nZfRX8MXjk
```

---

## 📋 PASO 2: Contactar al Equipo de DGS

Necesitas pedir la **clave secreta (secret key)** que usan para firmar el JWT.

### **Información para el equipo de DGS:**

```
API: http://svr-v-patri:85
Endpoint: /api/auth/login
Algoritmo: HS256 (HMAC SHA-256)
Issuer: DGS.Services.WebApi.Security
Audience: DGS.Services.Clients

Necesito: La clave secreta (string) usada para firmar los tokens JWT
Propósito: Validar los tokens en nuestra API de Seguridad Social
```

### **Contactos posibles:**
- Administrador del servidor `svr-v-patri`
- Equipo de desarrollo de DGS
- Responsable de seguridad de aplicaciones

---

## 📋 PASO 3: Verificar la Clave (Una vez que la tengas)

Usa jwt.io para verificar:
1. Pega el token en jwt.io
2. En la sección "Verify Signature", pega la clave secreta
3. Si aparece "Signature Verified" ✅ - La clave es correcta

---

## 📋 PASO 4: Configurar en nuestra API

Una vez que tengas la clave, actualiza `appsettings.json`:

```json
{
  "Jwt": {
    "SecretKey": "LA_CLAVE_SECRETA_AQUI"
  }
}
```

---

## ⚠️ IMPORTANTE - SEGURIDAD

1. **NO commitear la clave al repositorio**
2. Usar variables de entorno en producción:
```json
{
  "Jwt": {
  "SecretKey": "${JWT_SECRET_KEY}"
  }
}
```

3. Agregar al `.gitignore`:
```
appsettings.Production.json
appsettings.*.json
```

---

## 🔄 ALTERNATIVA: Si NO puedes obtener la clave

Si el equipo de DGS no puede compartir la clave, hay 2 opciones:

### **Opción A: Validación Remota**
Crear un endpoint en la API de DGS:
```
POST http://svr-v-patri:85/api/auth/validate
Authorization: Bearer {token}

Response:
{
  "valid": true,
  "user_id": "105",
  "expires_at": "2025-12-16T16:05:46Z"
}
```

### **Opción B: Middleware Sin Validación de Firma**
Confiar en la red interna y solo verificar que el token esté bien formado (no recomendado para producción).

---

## ✅ PRÓXIMOS PASOS

1. **[ ] Contactar al equipo de DGS**
2. **[ ] Solicitar la clave secreta**
3. **[ ] Verificar la clave con jwt.io**
4. **[ ] Actualizar appsettings.json**
5. **[ ] Configurar Startup.cs** (yo lo hago)
6. **[ ] Probar login + endpoints protegidos**

---

**¿Tienes acceso al código fuente de la API de DGS (`svr-v-patri:85`)?**
Si tienes acceso al código, podría indicarte dónde buscar la clave en el código (típicamente en appsettings.json o configuración).
