# 🚀 Inicio Rápido - Seguridad Social API

## 📖 Acceso a Swagger (Documentación Interactiva)

### Ejecutar la Aplicación

```bash
dotnet run
```

### Abrir Swagger UI

Una vez que la aplicación esté corriendo, abre tu navegador en:

**En el mismo servidor:**
```
http://localhost:5000/swagger
```

**Desde otra máquina en la red:**
```
http://<IP-DEL-SERVIDOR>:5000/swagger
```

> ⚠️ **Importante**: Para acceder desde otras máquinas, debes **abrir el puerto 5000 en el firewall**.  
> Ver: [Guía de Acceso Remoto](docs/ACCESO-REMOTO.md)

**¡Eso es todo!** 🎉

---

## 🔓 Abrir Puerto en Firewall (Para Acceso Remoto)

Si `http://localhost:5000` funciona pero `http://<IP>:5000` NO:

### Solución Rápida

1. **Click derecho** en `abrir-puerto-firewall.bat`
2. **Ejecutar como administrador**
3. Listo! ✓

### Verificar Conexión

```cmd
verificar-conectividad.bat
```

📖 **Guía completa**: [docs/ACCESO-REMOTO.md](docs/ACCESO-REMOTO.md)

---

## 🔐 Guía Rápida de Autenticación

1. **Login** en `/api/auth/login`:
   ```json
   {
     "user_name": "tu_usuario",
     "password": "tu_contraseña"
   }
   ```

2. **Copiar token** del response

3. **Click en "Authorize"** (candado verde) en Swagger

4. **Pegar**: `Bearer <tu-token>`

5. **Probar endpoints** protegidos

---

## 📚 Documentación Completa

- **[Guía de Swagger](docs/SWAGGER.md)** - Documentación completa de Swagger
- **[Acceso Remoto](docs/ACCESO-REMOTO.md)** - Solucionar problemas de conectividad
- **[Guía de Deployment](docs/DEPLOYMENT.md)** - Cómo desplegar la API
- **[Configuración de Kestrel](docs/configuracion-kestrel.md)** - Configuración de puertos

---

## 🎯 URLs Útiles

| Ubicación | Swagger URL | Requiere Firewall |
|----------|-------------|-------------------|
| Mismo servidor | `http://localhost:5000/swagger` | ❌ No |
| Desde otra máquina | `http://<IP-SERVIDOR>:5000/swagger` | ✅ Sí |
| Producción | `http://<servidor>:5000/swagger` | ✅ Sí |

---

## 🆘 Problemas Comunes

- ❓ **Swagger no carga?** → Verifica que `dotnet run` esté ejecutándose
- ❓ **Error 401?** → Necesitas autenticarte primero (ver guía arriba)
- ❓ **Puerto ocupado?** → Cambia el puerto en `appsettings.json`
- ❓ **No accedo desde otra máquina?** → Abre el puerto en el firewall (ver [Acceso Remoto](docs/ACCESO-REMOTO.md))

---

## 🛠️ Scripts Útiles

- ✅ `abrir-puerto-firewall.bat` - Abre puerto 5000 en firewall
- ✅ `verificar-conectividad.bat` - Diagnóstico de conectividad
- ✅ `publish.bat` - Publicar para producción
- ✅ `fix-https-error.bat` - Solucionar error de HTTPS

---

## 📞 Ayuda Rápida

### Problema: No puedo acceder desde otra máquina

**Síntoma**: `http://localhost:5000/swagger` funciona, pero `http://192.168.x.x:5000/swagger` no.

**Solución**:
1. Ejecuta `abrir-puerto-firewall.bat` como Administrador
2. Ejecuta `verificar-conectividad.bat` para diagnosticar
3. Lee la [guía completa](docs/ACCESO-REMOTO.md)

### Problema: Error de certificado HTTPS en servidor

**Síntoma**: `Unable to configure HTTPS endpoint...`

**Solución**:
1. Ejecuta `fix-https-error.bat`
2. O lee la [guía de deployment](docs/DEPLOYMENT.md)

---

Lee la documentación completa para más detalles.
