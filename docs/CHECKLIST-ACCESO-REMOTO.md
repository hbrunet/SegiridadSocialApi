# ✅ Checklist: Habilitar Acceso Remoto a la API

## 📋 Lista de Verificación

Sigue estos pasos en orden:

### 1. ✅ Configuración de Kestrel
- [ ] Verificar `appsettings.json` tiene:
  ```json
  "Kestrel": {
    "Endpoints": {
   "Http": {
     "Url": "http://0.0.0.0:5000"  // NO usar "localhost"
      }
    }
  }
  ```
- [ ] **Importante**: Debe ser `0.0.0.0`, NO `localhost`

### 2. ✅ Iniciar la Aplicación
- [ ] Ejecutar: `dotnet run`
- [ ] Verificar que NO hay errores
- [ ] Verificar mensaje: "Now listening on: http://0.0.0.0:5000"

### 3. ✅ Verificar Puerto Escuchando
- [ ] Abrir otra terminal/CMD
- [ ] Ejecutar: `netstat -ano | findstr ":5000"`
- [ ] Debe mostrar: `TCP    0.0.0.0:5000  0.0.0.0:0  LISTENING`

### 4. ✅ Probar Acceso Local
- [ ] Abrir navegador en el servidor
- [ ] Ir a: `http://localhost:5000/swagger`
- [ ] Debe cargar Swagger UI correctamente

### 5. ✅ Abrir Puerto en Firewall ⚠️ MUY IMPORTANTE
- [ ] Click derecho en `abrir-puerto-firewall.bat`
- [ ] Seleccionar **"Ejecutar como administrador"**
- [ ] Verificar mensaje: "✓ Puerto 5000 abierto exitosamente"

### 6. ✅ Verificar Regla de Firewall
- [ ] Ejecutar: `verificar-conectividad.bat`
- [ ] Debe mostrar: "✓ Regla de firewall configurada"

### 7. ✅ Obtener IP del Servidor
- [ ] Ejecutar: `ipconfig`
- [ ] Anotar la **"Dirección IPv4"**
  - Ejemplo: `192.168.1.100`

### 8. ✅ Probar desde Cliente
- [ ] Desde **otra máquina** en la misma red
- [ ] Abrir navegador
- [ ] Ir a: `http://192.168.1.100:5000/swagger`
  - (Reemplazar con la IP real del servidor)
- [ ] Debe cargar Swagger UI

---

## 🎯 Si Algo NO Funciona

### ❌ localhost:5000 NO funciona

**Problema**: La aplicación no está corriendo o hay error de configuración.

**Solución**:
```cmd
# Ver errores
dotnet run

# Ver si hay algo en puerto 5000
netstat -ano | findstr ":5000"
```

### ❌ IP:5000 NO funciona (pero localhost SÍ)

**Problema**: Firewall bloqueando.

**Solución**:
1. Ejecutar `abrir-puerto-firewall.bat` como Admin
2. Ejecutar `verificar-conectividad.bat`
3. Ver [docs/ACCESO-REMOTO.md](ACCESO-REMOTO.md)

### ❌ Firewall abierto pero aún NO funciona

**Posibles causas**:
- [ ] Antivirus bloqueando → Temporalmente deshabilitar y probar
- [ ] Firewall corporativo → Consultar con IT
- [ ] Red diferente/VLAN → Verificar conectividad de red
- [ ] Router bloqueando → Verificar configuración de router

**Diagnóstico**:
```cmd
# Desde el cliente, probar ping
ping 192.168.1.100

# Probar telnet (requiere instalación)
telnet 192.168.1.100 5000
```

---

## 📊 Tabla de Diagnóstico Rápido

| Síntoma | Causa Probable | Solución |
|---------|----------------|----------|
| localhost:5000 ✅ funciona<br>IP:5000 ❌ NO funciona | Firewall bloqueando | `abrir-puerto-firewall.bat` como Admin |
| localhost:5000 ❌ NO funciona | App no corriendo o error | Verificar `dotnet run` |
| "Connection refused" | Puerto no abierto | Verificar `netstat -ano \| findstr ":5000"` |
| "Timeout" o "No route to host" | Red/firewall de red | Verificar ping, consultar IT |
| Carga pero error 404 en /swagger | Ruta incorrecta | Usar `/swagger` no `/swagger/` |

---

## 🔍 Comandos de Diagnóstico

### En el Servidor

```cmd
# 1. Ver si la app está corriendo
netstat -ano | findstr ":5000"

# 2. Ver IP del servidor
ipconfig

# 3. Ver reglas de firewall
netsh advfirewall firewall show rule name="Seguridad Social API - Puerto 5000"

# 4. Diagnóstico completo
verificar-conectividad.bat
```

### En el Cliente

```cmd
# 1. Probar conectividad
ping <IP-SERVIDOR>

# 2. Probar puerto (requiere telnet)
telnet <IP-SERVIDOR> 5000

# 3. Con PowerShell (alternativa a telnet)
Test-NetConnection -ComputerName <IP-SERVIDOR> -Port 5000
```

---

## ✅ Cuando Todo Funciona

Deberías poder acceder a:

- ✅ `http://localhost:5000/swagger` (desde el servidor)
- ✅ `http://192.168.1.100:5000/swagger` (desde cualquier máquina en la red)
- ✅ `http://192.168.1.100:5000/api/auth/login` (endpoints de la API)

---

## 📞 Notas Finales

- **Firewall de Windows** es la causa #1 de problemas de acceso remoto
- Ejecuta `abrir-puerto-firewall.bat` **como Administrador**
- Usa `verificar-conectividad.bat` para diagnosticar problemas
- Lee [docs/ACCESO-REMOTO.md](ACCESO-REMOTO.md) para troubleshooting detallado

---

## 🎉 ¡Listo!

Si completaste todos los pasos, tu API debería ser accesible desde cualquier máquina en la red.

**¿Aún tienes problemas?** Consulta la [documentación completa](ACCESO-REMOTO.md).
