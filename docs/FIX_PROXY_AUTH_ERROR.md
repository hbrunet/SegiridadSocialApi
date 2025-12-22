# 🔧 Solución - Error de Proxy en Autenticación

## ❌ **Problema Detectado**

```
ERROR: The requested URL could not be retrieved
Access Denied.
Access control configuration prevents your request from being allowed at this time.
```

**Causa**: El proxy corporativo (Squid) está bloqueando la conexión a `http://svr-v-patri:85`.

---

## ✅ **Solución Implementada**

Se ha configurado el `HttpClient` para manejar correctamente el proxy corporativo.

---

## 🎯 **3 Opciones de Configuración**

### **Opción 1: Sin Proxy (Red Interna Directa)** ⭐ RECOMENDADA si `svr-v-patri` es servidor interno

Si `svr-v-patri:85` es un servidor **dentro de la red interna** y no necesitas proxy para acceder:

```json
{
  "Proxy": {
    "Url": "",
    "Username": "",
  "Password": "",
    "BypassForLocalAddresses": true
  }
}
```

**¿Cuándo usar?**
- El servidor está en la misma red interna
- No necesitas proxy para acceder a servidores internos
- Tu configuración de red permite conexiones directas

---

### **Opción 2: Proxy con Credenciales de Windows (SSO)**

Si el proxy acepta las credenciales de tu sesión de Windows:

```json
{
  "Proxy": {
    "Url": "http://proxy.mecontuc.gov.ar:3128",
    "Username": "",
    "Password": "",
  "BypassForLocalAddresses": true
  }
}
```

**Cómo funciona:**
- Usa `CredentialCache.DefaultNetworkCredentials`
- Automáticamente usa tu usuario de Windows
- No necesitas poner usuario/contraseña explícitamente

---

### **Opción 3: Proxy con Credenciales Explícitas**

Si necesitas especificar usuario y contraseña del proxy:

```json
{
  "Proxy": {
    "Url": "http://proxy.mecontuc.gov.ar:3128",
    "Username": "tu_usuario",
    "Password": "tu_contraseña",
    "BypassForLocalAddresses": true
  }
}
```

**Ejemplo real** (basado en tu `configure-proxy.sh`):
```json
{
  "Proxy": {
    "Url": "http://proxy.mecontuc.gov.ar:3128",
    "Username": "hbrunet",
"Password": "tu_contraseña_del_proxy",
    "BypassForLocalAddresses": true
  }
}
```

⚠️ **IMPORTANTE**: 
- **NO commitear contraseñas** al repositorio
- Usar variables de entorno en producción
- Considerar usar secretos de Azure/Docker

---

## 🧪 **Cómo Saber Cuál Opción Usar**

### **Test 1: ¿Puedes acceder directamente desde tu máquina?**

```bash
curl http://svr-v-patri:85/api/auth/login
```

- **✅ Si funciona**: Usa **Opción 1** (sin proxy)
- **❌ Si falla**: Necesitas proxy (Opción 2 o 3)

---

### **Test 2: ¿Necesitas proxy?**

```bash
# Con proxy y credenciales de Windows
curl -x http://proxy.mecontuc.gov.ar:3128 http://svr-v-patri:85/api/auth/login

# Con proxy y credenciales explícitas
curl -x http://usuario:password@proxy.mecontuc.gov.ar:3128 http://svr-v-patri:85/api/auth/login
```

- **✅ Primera funciona**: Usa **Opción 2** (SSO)
- **✅ Segunda funciona**: Usa **Opción 3** (credenciales explícitas)

---

## 📝 **Configuración Actual del Código**

El código ahora soporta las 3 opciones automáticamente:

```csharp
// Si Proxy:Url está vacío → No usa proxy (conexión directa)
// Si Proxy:Url existe y Username/Password vacíos → Usa credenciales de Windows
// Si Proxy:Url existe y Username/Password llenos → Usa credenciales explícitas
```

---

## 🚀 **Pasos para Configurar**

### **1. Determinar si `svr-v-patri` es interno o externo**

```bash
# Desde tu máquina donde corre la API
ping svr-v-patri
```

- **Si resuelve a IP interna** (10.x.x.x, 192.168.x.x): Probablemente no necesitas proxy
- **Si no resuelve o es externa**: Necesitas proxy

---

### **2. Actualizar `appsettings.json`**

**Caso A: Servidor Interno (Recomendado probar primero)**
```json
{
  "Proxy": {
    "Url": "",
    "BypassForLocalAddresses": true
  }
}
```

**Caso B: Necesitas Proxy con SSO de Windows**
```json
{
  "Proxy": {
    "Url": "http://proxy.mecontuc.gov.ar:3128",
    "BypassForLocalAddresses": true
  }
}
```

**Caso C: Necesitas Proxy con Credenciales**
```json
{
  "Proxy": {
    "Url": "http://proxy.mecontuc.gov.ar:3128",
    "Username": "hbrunet",
    "Password": "tu_password",
    "BypassForLocalAddresses": true
  }
}
```

---

### **3. Probar la API**

```bash
# Reiniciar la API
dotnet run

# En otra terminal, probar login
curl -X POST https://localhost:5001/api/auth/login \
  -H "Content-Type: application/json" \
  -k \
  -d '{
    "user_name": "h_dbrunet",
    "password": "dbrunet123#",
 "application_id": 9
  }'
```

---

## 🔍 **Diagnóstico de Logs**

Los logs en `logs/log-{fecha}.txt` mostrarán:

**✅ Conexión exitosa:**
```
[INF] Intentando login para usuario h_dbrunet en aplicación 9
[INF] Login exitoso para usuario h_dbrunet. Token expira en 3600 segundos
```

**❌ Error de proxy:**
```
[ERR] Error de red al intentar login para usuario h_dbrunet
System.Net.Http.HttpRequestException: Access Denied
```

**❌ Error de credenciales de proxy:**
```
[ERR] Error de red al intentar login para usuario h_dbrunet
System.Net.Http.HttpRequestException: Proxy Authentication Required (407)
```

---

## 🛠️ **Soluciones Alternativas**

### **Alternativa 1: Agregar `svr-v-patri` a las exclusiones del proxy**

Si tienes acceso al servidor donde corre la API, agrega `svr-v-patri` a las exclusiones del proxy en variables de entorno:

**Windows:**
```powershell
# Variables de entorno del sistema
NO_PROXY=svr-v-patri,localhost,127.0.0.1,.local
```

**Linux:**
```bash
export NO_PROXY=svr-v-patri,localhost,127.0.0.1,.local
```

---

### **Alternativa 2: Usar la IP directa en lugar del hostname**

Si conoces la IP de `svr-v-patri`:

```json
{
  "AuthApi": {
    "BaseUrl": "http://10.6.46.XXX:85",
    "LoginEndpoint": "/api/auth/login"
  }
}
```

Y configurar bypass por IP:
```json
{
  "Proxy": {
    "Url": "",
    "BypassForLocalAddresses": true
  }
}
```

---

### **Alternativa 3: Solicitar excepción en el proxy corporativo**

Contactar al equipo de IT para agregar una regla en Squid que permita:
```
# Permitir conexión de la API a svr-v-patri:85
acl auth_api src 10.6.46.115
acl auth_server dst svr-v-patri
http_access allow auth_api auth_server
```

---

## 📊 **Matriz de Decisión**

| Escenario | Configuración | Archivo |
|-----------|---------------|---------|
| `svr-v-patri` es interno | `Proxy:Url = ""` | `appsettings.json` |
| Proxy con SSO Windows | `Proxy:Url = "http://proxy:3128"` | `appsettings.json` |
| Proxy con credenciales | `Proxy:Url = "http://proxy:3128"` + Username/Password | `appsettings.json` |
| Bypass específico | Agregar a `NO_PROXY` | Variables de entorno |

---

## ✅ **Checklist de Verificación**

- [ ] Determinar si `svr-v-patri` es servidor interno o externo
- [ ] Hacer ping a `svr-v-patri` desde el servidor de la API
- [ ] Probar con `curl` directamente
- [ ] Elegir configuración de proxy adecuada
- [ ] Actualizar `appsettings.json`
- [ ] Reiniciar la API
- [ ] Probar endpoint `/api/auth/login`
- [ ] Verificar logs en `logs/log-{fecha}.txt`
- [ ] Si usa credenciales, NO commitearlas al repo

---

## 🎯 **Recomendación Final**

**PRUEBA EN ESTE ORDEN:**

1. **Primero**: Sin proxy (`Proxy:Url = ""`)
   - Más rápido y seguro
   - Funciona si `svr-v-patri` es interno

2. **Segundo**: Proxy con SSO de Windows
   - No necesitas poner contraseñas
   - Más seguro

3. **Último recurso**: Proxy con credenciales explícitas
   - Solo si las anteriores no funcionan
   - Usar variables de entorno en producción

---

## 📞 **Contacto**

Si ninguna opción funciona, contactar a:
- **Equipo de IT**: Para excepción en el proxy
- **Administrador de `svr-v-patri`**: Para verificar conectividad

---

**Fecha**: 17 de Diciembre de 2025  
**Estado**: ✅ Código actualizado - Pendiente configuración
