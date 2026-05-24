# PyStudio Desktop

Cliente de escritorio para estudiantes. Se agrega como una carpeta independiente `desktop-client`, sin modificar `backend` ni `web-app`.

## Funcionalidades incluidas

- Crear proyecto local.
- Abrir proyecto local.
- Crear script `.py`.
- Modificar y guardar script.
- Borrar script.
- Firmar script localmente.
- Verificar si el script cambió después de firmarse.
- Ejecutar script y mostrar salida en terminal integrada.
- Ejecutar líneas sueltas de Python en una consola interactiva.
- Inicializar repositorio Git local dentro del proyecto.
- Crear commits automáticos al guardar, ejecutar, firmar y entregar.
- Ver historial Git local.
- Conectarse al backend PHP existente.
- Registrar estudiante.
- Iniciar sesión.
- Unirse a curso mediante código.
- Ver cursos inscritos.
- Ver tareas/actividades del curso.
- Enviar el script actual como entrega.

## Cómo ejecutarlo en Windows

Desde PowerShell, dentro de la carpeta raíz del repositorio:

```powershell
cd desktop-client
python main.py
```

## Requisitos

- Python 3.11 o superior.
- Git instalado para que funcione el historial local. Si Git no está instalado, el IDE funciona igual, pero mostrará un aviso cuando intente crear commits.

## URL del backend

Por defecto se usa:

```text
http://localhost:8000/api
```

Si el backend está en una IP pública o en otra ruta, cámbielo desde el campo `URL API` dentro de la interfaz.

## Generar ejecutable Windows

```powershell
cd desktop-client
.\build_windows.bat
```

El ejecutable queda en:

```text
desktop-client\dist\PyStudioDesktop.exe
```

## Patrones aplicados en desktop

- Facade: `DesktopFacade` centraliza operaciones de proyecto, scripts, Python, firma y Git.
- Decorator: `TimestampTerminalDecorator` agrega hora a los mensajes de terminal sin modificar el componente base.
- Observer: `StatusSubject` notifica a la ventana cuando cambia el estado del sistema.

