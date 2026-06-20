# PyStudio Desktop - versión C# WinForms

Esta carpeta reemplaza el `desktop-client` hecho en Python/Tkinter por un cliente de escritorio hecho en C# con Windows Forms.

## Qué incluye

- Crear y abrir proyectos locales.
- Crear, abrir, guardar y borrar scripts `.py`.
- Editor con resaltado básico de sintaxis de Python.
- Numeración de líneas básica.
- Bloqueo de pegado por teclado y evento `WM_PASTE`.
- Ejecución de scripts Python desde el IDE.
- Consola para ejecutar una línea de Python.
- Firma local de scripts para detectar cambios fuera de la aplicación.
- Integración local con Git: `git init`, commits automáticos e historial.
- Conexión con el backend PHP existente:
  - registrar estudiante,
  - iniciar sesión,
  - unirse a curso,
  - cargar cursos,
  - cargar actividades,
  - enviar el script actual como entrega.

## Requisitos

- Windows.
- Visual Studio 2022 o superior.
- Workload: `.NET desktop development`.
- .NET 8 SDK.
- Python instalado y agregado al PATH.
- Git instalado y agregado al PATH.
- Backend PHP corriendo, por defecto en `http://localhost:8000/api`.

## Cómo abrirlo

1. Abra Visual Studio.
2. Seleccione **Open a project or solution**.
3. Abra el archivo:

```text
PyStudioDesktopSharp/PyStudioDesktopSharp.csproj
```

4. Ejecute con el botón verde de Visual Studio.

También puede compilar desde terminal:

```powershell
cd PyStudioDesktopSharp
dotnet build
dotnet run
```

## Cómo probar con el backend

Desde la raíz del backend del proyecto original:

```powershell
cd backend
php -S localhost:8000 index.php
```

Luego, en el cliente C# deje la URL como:

```text
http://localhost:8000/api
```

## Nota importante sobre grupos

El documento de requerimientos pide formar grupos, pero el backend incluido no expone endpoints para crear grupo o unirse a grupo. El cliente C# mantiene la entrega individual igual que el cliente Python. Para entrega grupal haría falta agregar endpoints de `GroupController` en el backend o ingresar manualmente un `group_id`.
