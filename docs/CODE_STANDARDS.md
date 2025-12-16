# 📐 Estilo y Formato de Código - Seguridad Social API

Este proyecto utiliza un conjunto estándar de herramientas para mantener la calidad y consistencia del código.

## 🛠️ Herramientas Configuradas

### ✅ EditorConfig
- **Archivo**: `.editorconfig`
- **Propósito**: Configuración universal de estilo de código
- **Compatible con**: Visual Studio, VS Code, Rider, etc.

### ✅ StyleCop Analyzers
- **Paquete**: `StyleCop.Analyzers` v1.2.0-beta.556
- **Configuración**: `stylecop.json`
- **Propósito**: Análisis estático de código C#

### ✅ .NET Analyzers
- **Incluido en**: .NET 8 SDK
- **Configuración**: Habilitado en `.csproj`
- **Propósito**: Análisis de código integrado de Microsoft

## 🚀 Inicio Rápido

### Formatear todo el código
```powershell
dotnet format SeguridadSocialApi.csproj
```

### Verificar formato
```powershell
dotnet format SeguridadSocialApi.csproj --verify-no-changes
```

### Build con análisis
```powershell
dotnet build /p:RunAnalyzersDuringBuild=true
```

## 📚 Convenciones Principales

| Elemento | Convención | Ejemplo |
|----------|-----------|---------|
| Clases/Interfaces | PascalCase | `OrderService`, `IOrderRepository` |
| Métodos/Propiedades | PascalCase | `ProcessOrder()`, `CustomerName` |
| Campos privados | _camelCase | `_logger`, `_dbContext` |
| Parámetros/Variables | camelCase | `orderId`, `totalAmount` |
| Constantes | PascalCase | `MaxRetries`, `DefaultTimeout` |

### Características Clave
- ✅ **Indentación**: 4 espacios
- ✅ **Namespaces**: File-scoped (`namespace App.Services;`)
- ✅ **Llaves**: Nueva línea (Allman style)
- ✅ **Usings**: System primero, ordenados alfabéticamente
- ✅ **Modificadores**: Siempre explícitos
- ✅ **Final de línea**: CRLF (Windows)

## 📖 Documentación

- **[CODE_STYLE_GUIDE.md](Docs/CODE_STYLE_GUIDE.md)**: Guía completa de estilo
- **[CODE_FORMATTING_TASKS.md](Docs/CODE_FORMATTING_TASKS.md)**: Tareas y comandos comunes

## ⚙️ Configuración del IDE

### Visual Studio 2022
1. **Format on save**: Tools → Options → Text Editor → C# → Code Style → Formatting
2. **Enable analyzers**: Tools → Options → Text Editor → C# → Advanced

### VS Code
1. Instalar extensiones:
   - C# Dev Kit
   - EditorConfig for VS Code
2. Configuración incluida en `.vscode/settings.json`

## 🔍 Reglas Personalizadas

Algunas reglas están configuradas como **suggestion** o **none** para flexibilidad:

| Regla | Severidad | Razón |
|-------|-----------|-------|
| SA1101 (this.) | none | Prefijo `this.` es opcional |
| SA1600 (Docs) | none | Documentación XML es recomendada, no obligatoria |
| SA1633 (Header) | none | Sin header de copyright obligatorio |
| CA1031 (Catch) | none | Catch genérico permitido en algunos contextos |

Ver `.editorconfig` para todas las reglas.

## 🎯 Integración Continua

Para CI/CD, agregar estos pasos:

```yaml
# Verificar formato
- run: dotnet format --verify-no-changes

# Build con análisis
- run: dotnet build /p:RunAnalyzersDuringBuild=true
```

Ver `Docs/CODE_FORMATTING_TASKS.md` para ejemplos completos de GitHub Actions y Azure DevOps.

## ❓ Soporte

Para problemas o preguntas sobre el estilo de código:
1. Consultar [CODE_STYLE_GUIDE.md](Docs/CODE_STYLE_GUIDE.md)
2. Revisar [Troubleshooting](Docs/CODE_FORMATTING_TASKS.md#solución-de-problemas)
3. Contactar al equipo de desarrollo

## 📝 Contribuir

Al contribuir código:
1. ✅ Ejecutar `dotnet format` antes de commit
2. ✅ Verificar que no hay warnings con `dotnet build`
3. ✅ Seguir las convenciones de nomenclatura
4. ✅ Documentar métodos públicos con XML comments (recomendado)

---

**Última actualización**: Diciembre 2024  
**Versión .NET**: 8.0  
**C# Version**: 12.0
