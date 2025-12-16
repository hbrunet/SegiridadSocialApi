# ✅ Checklist de Configuración de Code Style

## Para Desarrolladores Nuevos

### Configuración Inicial

- [ ] Clonar el repositorio
- [ ] Abrir el proyecto en tu IDE preferido (Visual Studio / VS Code)
- [ ] Verificar que los archivos de configuración estén presentes:
  - [ ] `.editorconfig`
  - [ ] `stylecop.json`
  - [ ] `.gitattributes`
  - [ ] `.vscode/settings.json` (para VS Code)

### Visual Studio 2022

- [ ] Ir a **Tools → Options → Text Editor → C# → Code Style → Formatting**
  - [ ] Activar "Format document on save"
  - [ ] Activar "Format on paste"
- [ ] Ir a **Tools → Options → Text Editor → C# → Advanced**
  - [ ] Activar "Run code analysis in background"
  - [ ] Activar "Show compiler errors and warnings for closed files"
  - [ ] Activar "Enable full solution analysis"
- [ ] Reiniciar Visual Studio para aplicar cambios

### VS Code

- [ ] Instalar extensiones requeridas:
  ```bash
  code --install-extension ms-dotnettools.csharp
  code --install-extension EditorConfig.EditorConfig
  ```
- [ ] Verificar que `.vscode/settings.json` exista
- [ ] Recargar window (Ctrl+Shift+P → "Reload Window")

### Verificación

- [ ] Ejecutar: `dotnet restore`
- [ ] Ejecutar: `dotnet build`
- [ ] Ejecutar: `dotnet format --verify-no-changes`
- [ ] Abrir un archivo .cs y verificar que el formato se aplica automáticamente al guardar

## Para Cada Pull Request

### Antes de Commit

- [ ] Ejecutar `dotnet format SeguridadSocialApi.csproj`
- [ ] Verificar que no hay errores de compilación
- [ ] Revisar warnings del analyzer (si hay muchos, considerar arreglar los relevantes)
- [ ] Asegurar que los nombres siguen las convenciones:
  - [ ] Clases/Interfaces: PascalCase
  - [ ] Métodos/Propiedades: PascalCase
  - [ ] Campos privados: _camelCase
  - [ ] Variables/Parámetros: camelCase

### Antes de Push

- [ ] Ejecutar `dotnet build /p:RunAnalyzersDuringBuild=true`
- [ ] Revisar que no haya introducido nuevos warnings críticos
- [ ] Verificar que el código está formateado correctamente

### Code Review

Revisor debe verificar:
- [ ] El código sigue las convenciones de nomenclatura
- [ ] Los métodos públicos tienen documentación XML (recomendado)
- [ ] No hay warnings innecesarios introducidos
- [ ] El formato es consistente con el resto del código

## Mantenimiento Periódico

### Mensual

- [ ] Ejecutar `dotnet format SeguridadSocialApi.csproj` en todo el proyecto
- [ ] Revisar y actualizar `.editorconfig` si hay nuevas necesidades
- [ ] Verificar si hay actualizaciones de StyleCop: `dotnet list package --outdated`

### Cuando se Actualiza .NET

- [ ] Actualizar `LangVersion` en `.csproj` si corresponde
- [ ] Revisar nuevas reglas de análisis en `.editorconfig`
- [ ] Ejecutar `dotnet format` y corregir issues

## Troubleshooting

### "Formato no se aplica automáticamente"
- [ ] Verificar que `.editorconfig` existe en la raíz
- [ ] Reiniciar el IDE
- [ ] Verificar configuración del IDE (ver arriba)

### "Muchos warnings de StyleCop"
- [ ] Ejecutar `dotnet format` para auto-corregir lo posible
- [ ] Revisar `.editorconfig` para ajustar severidades si es necesario
- [ ] Para warnings que no se pueden auto-corregir, ver documentación en `Docs/CODE_FORMATTING_TASKS.md`

### "Conflicto entre EditorConfig y StyleCop"
- [ ] EditorConfig tiene precedencia
- [ ] Verificar reglas en `.editorconfig` sección `#### StyleCop Analyzer Rules ####`
- [ ] Ajustar `dotnet_diagnostic.SAxxxx.severity` según necesidad

## Recursos Adicionales

- 📖 [CODE_STANDARDS.md](CODE_STANDARDS.md) - Resumen ejecutivo
- 📖 [Docs/CODE_STYLE_GUIDE.md](Docs/CODE_STYLE_GUIDE.md) - Guía completa
- 📖 [Docs/CODE_FORMATTING_TASKS.md](Docs/CODE_FORMATTING_TASKS.md) - Comandos y tareas

## Contacto

Para dudas sobre code style:
- Consultar documentación primero
- Revisar ejemplos en el código existente
- Contactar al equipo técnico

---

**¿Primera vez configurando?** Empieza por la sección "Para Desarrolladores Nuevos" 👆
