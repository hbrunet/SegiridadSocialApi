# 🔧 Solución: No Puedo Acceder desde Otras Máquinas

## 🎯 Problema

✅ **Funciona**: `http://localhost:5000/swagger` (en el servidor)  
❌ **NO funciona**: `http://192.168.x.x:5000/swagger` (desde cliente)

---

## 📋 Solución Paso a Paso

### ✅ Paso 1: Verificar Configuración de Kestrel

**Ya hecho** ✓ - Tu `appsettings.json` ya tiene:
```json
"Kestrel": {
  "Endpoints": {
    "Http": {
      "Url": "http://0.0.0.0:5000"  // ✓ Correcto
    }
  }
}
```

### ✅ Paso 2: Abrir Puerto en Firewall (MUY IMPORTANTE)

Este es el paso que probablemente te falta.

#### Opción A: Usar Script Automático (Recomendado)

1. **Click derecho** en `abrir-puerto-firewall.bat`
2. **Seleccionar**: "Ejecutar como administrador"
3. Listo! ✓

#### Opción B: Manual (PowerShell como Administrador)

```powershell
# Abrir PowerShell como Administrador y ejecutar:
New-NetFirewallRule -DisplayName "Seguridad Social API - Puerto 5000" `
  -Direction Inbound `
    -Protocol TCP `
    -LocalPort 5000 `
    -Action Allow `
    -Profile Any
```

#### Opción C: GUI de Windows

1. Abrir **"Firewall de Windows con seguridad avanzada"**
2. Click en **"Reglas de entrada"** → **"Nueva regla..."**
3. Tipo de regla: **Puerto** → Siguiente
4. TCP, Puerto local específico: **5000** → Siguiente
5. Acción: **Permitir la conexión** → Siguiente
6. Perfil: **Marcar todos** (Dominio, Privado, Público) → Siguiente
7. Nombre: **Seguridad Social API - Puerto 5000** → Finalizar

---

### ✅ Paso 3: Verificar que la App está Corriendo

```cmd
# Ejecutar en el servidor
dotnet run

# En otra terminal, verificar:
netstat -ano | findstr ":5000"
```

Deberías ver algo como:
```
TCP    0.0.0.0:5000    0.0.0.0:0  LISTENING    12345
```

---

### ✅ Paso 4: Encontrar la IP del Servidor

```cmd
ipconfig
```

Busca la línea **"Dirección IPv4"**, por ejemplo:
```
Dirección IPv4. . . . : 192.168.1.100
```

---

### ✅ Paso 5: Probar desde el Cliente

Desde otra máquina en la misma red:

```
http://192.168.1.100:5000/swagger
```

(Reemplaza `192.168.1.100` con la IP real del servidor)

---

## 🧪 Diagnóstico Rápido

Ejecuta el script de diagnóstico:

```cmd
verificar-conectividad.bat
```

Este script verifica:
- ✓ Si la app está corriendo
- ✓ IP del servidor
- ✓ Estado del firewall
- ✓ Reglas de firewall configuradas

---

## 🐛 Troubleshooting Avanzado

### ❓ Aún no funciona después de abrir el puerto?

#### 1. Verificar que el puerto está abierto

```cmd
# En el servidor
netsh advfirewall firewall show rule name="Seguridad Social API - Puerto 5000"
```

Debe mostrar:
```
Nombre de regla:          Seguridad Social API - Puerto 5000
Habilitado:      Sí
Dirección:     Entrada
Acción:    Permitir
```

#### 2. Probar conectividad de red desde el cliente

```cmd
# Desde el cliente, probar ping
ping 192.168.1.100

# Probar si el puerto está accesible (requiere telnet)
telnet 192.168.1.100 5000
```

Si telnet no está instalado:
```cmd
# Como administrador
dism /online /Enable-Feature /FeatureName:TelnetClient
```

#### 3. Verificar firewall del cliente

El firewall del **cliente** también podría estar bloqueando. Temporalmente desactívalo para probar:

```cmd
# SOLO PARA PRUEBA - En el cliente
netsh advfirewall set allprofiles state off

# Probar acceso

# VOLVER A ACTIVAR
netsh advfirewall set allprofiles state on
```

#### 4. Verificar firewall corporativo/router

Si estás en una red corporativa:
- El firewall de red podría estar bloqueando
- Consulta con tu administrador de red

#### 5. Verificar antivirus

Algunos antivirus bloquean conexiones entrantes. Temporalmente:
- Deshabilita el antivirus
- Prueba el acceso
- Si funciona, agrega excepción en el antivirus para el puerto 5000

---

## 📊 Tabla de Verificación

| ✓ | Paso | Comando/Acción |
|---|------|----------------|
| ☐ | Configuración Kestrel | `0.0.0.0:5000` en appsettings.json |
| ☐ | Aplicación corriendo | `dotnet run` |
| ☐ | Puerto escuchando | `netstat -ano \| findstr ":5000"` |
| ☐ | Firewall abierto | `abrir-puerto-firewall.bat` |
| ☐ | IP del servidor | `ipconfig` |
| ☐ | Ping funciona | `ping <IP-SERVIDOR>` |
| ☐ | Puerto accesible | `telnet <IP-SERVIDOR> 5000` |
| ☐ | Swagger accesible | `http://<IP-SERVIDOR>:5000/swagger` |

---

## 🔒 Seguridad - Recomendaciones

### Para Servidor de Prueba Interno

✅ **Permitir acceso desde toda la red local** (configuración actual)

### Para Producción Expuesta a Internet

❌ **NO exponer puerto 5000 directamente**

En su lugar:
1. Usar IIS/Nginx como reverse proxy
2. IIS/Nginx maneja HTTPS en puerto 443
3. IIS/Nginx hace proxy interno a `http://localhost:5000`
4. Firewall solo permite puerto 443 (HTTPS)

Ver: [docs/DEPLOYMENT.md](DEPLOYMENT.md#-configuración-con-reverse-proxy)

---

## 🚀 Comandos Útiles

### Verificar reglas de firewall activas
```cmd
netsh advfirewall firewall show rule name=all | findstr "5000"
```

### Ver todas las conexiones en puerto 5000
```cmd
netstat -ano | findstr ":5000"
```

### Reiniciar firewall (como Admin)
```cmd
netsh advfirewall reset
```

### Ver perfil activo del firewall
```cmd
netsh advfirewall show currentprofile
```

---

## 📞 Resumen Rápido

**Si localhost funciona pero la IP no:**

1. ✅ **Configuración**: Ya tienes `0.0.0.0:5000` ✓
2. 🔥 **Firewall**: Ejecuta `abrir-puerto-firewall.bat` como Admin
3. 🔍 **Verificar**: Ejecuta `verificar-conectividad.bat`
4. 🌐 **Probar**: `http://<IP-SERVIDOR>:5000/swagger`

**El problema más común es el Firewall de Windows bloqueando el puerto 5000.**

---

## 📚 Scripts Incluidos

- ✅ `abrir-puerto-firewall.bat` - Abre puerto 5000 (ejecutar como Admin)
- ✅ `verificar-conectividad.bat` - Diagnóstico completo
- ✅ `cerrar-puerto-firewall.bat` - Cierra puerto 5000 si es necesario

---

## 🎉 ¿Funcionó?

Si después de abrir el puerto en el firewall aún no funciona, revisa:
1. ¿El antivirus está bloqueando?
2. ¿Hay un firewall corporativo/router?
3. ¿Estás en la misma red/VLAN que el servidor?

Para más ayuda, ejecuta `verificar-conectividad.bat` y comparte el resultado.
