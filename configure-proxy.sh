#!/bin/bash
# Script para configurar el proxy de NuGet

echo "=== Configuración de Proxy para NuGet ==="
echo ""
echo "Proxy detectado: proxy.mecontuc.gov.ar:3128"
echo ""

# Solicitar usuario
read -p "Ingresa tu usuario del proxy (ejemplo: hbrunet o hbrunet.MECONTUC): " PROXY_USER

# Solicitar contraseña (sin mostrar en pantalla)
read -sp "Ingresa tu contraseña del proxy: " PROXY_PASS
echo ""

# Codificar caracteres especiales en la contraseña
ENCODED_PASS=$(echo -n "$PROXY_PASS" | jq -sRr @uri)

# Crear la URL del proxy
PROXY_URL="http://${PROXY_USER}:${ENCODED_PASS}@proxy.mecontuc.gov.ar:3128/"

echo ""
echo "Configurando NuGet.Config..."

# Backup del archivo original
cp "$USERPROFILE/AppData/Roaming/NuGet/NuGet.Config" "$USERPROFILE/AppData/Roaming/NuGet/NuGet.Config.backup"

# Crear nuevo archivo de configuración
cat > "$USERPROFILE/AppData/Roaming/NuGet/NuGet.Config" << EOF
<?xml version="1.0" encoding="utf-8"?>
<configuration>
  <config>
    <add key="http_proxy" value="${PROXY_URL}" />
    <add key="https_proxy" value="${PROXY_URL}" />
  </config>
  <packageSources>
    <add key="nuget.org" value="https://api.nuget.org/v3/index.json" protocolVersion="3" />
  </packageSources>
  <packageRestore>
    <add key="enabled" value="True" />
    <add key="automatic" value="True" />
  </packageRestore>
  <bindingRedirects>
    <add key="skip" value="False" />
  </bindingRedirects>
  <packageManagement>
    <add key="format" value="0" />
    <add key="disabled" value="True" />
  </packageManagement>
</configuration>
EOF

echo "✓ Configuración completada!"
echo ""
echo "Probando conexión a NuGet..."
dotnet nuget list source

echo ""
echo "Intentando restaurar paquetes..."
cd /e/source/DDJJ/backend-dotnet/SeguridadSocialApi
dotnet restore
