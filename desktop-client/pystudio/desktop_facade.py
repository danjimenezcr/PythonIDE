from __future__ import annotations

from datetime import datetime
from pathlib import Path

from .git_service import GitResult, GitService
from .models import Project, ScriptFile
from .python_runner import ExecutionResult, InteractivePythonConsole, PythonRunner
from .signature_service import SignatureService


class DesktopFacade:
    """Facade: punto único de entrada para operaciones del IDE desktop."""

    def __init__(self) -> None:
        self.project: Project | None = None
        self.current_script: ScriptFile | None = None
        self.signatures = SignatureService()
        self.python_runner = PythonRunner()
        self.interactive_console = InteractivePythonConsole()
        self.git = GitService()

    def create_project(self, parent_dir: Path, project_name: str) -> Project:
        root = parent_dir / project_name
        root.mkdir(parents=True, exist_ok=True)
        project = Project(project_name, root)
        self.project = project
        project.save_manifest()
        self.git.init_repo(project.root)
        self.git.commit_all(project.root, self._auto_message("creacion de proyecto"))
        return project

    def open_project(self, root: Path) -> Project:
        self.project = Project.load(root)
        self.git.init_repo(self.project.root)
        return self.project

    def create_script(self, script_name: str) -> ScriptFile:
        project = self._require_project()
        if not script_name.endswith(".py"):
            script_name += ".py"
        path = project.root / script_name
        if path.exists():
            raise FileExistsError(f"El script {script_name} ya existe.")
        path.write_text("# Nuevo script PyStudio\n\nprint('Hola desde PyStudio')\n", encoding="utf-8")
        self.current_script = ScriptFile(script_name, path)
        project.save_manifest()
        self.git.commit_all(project.root, self._auto_message(f"crea {script_name}"))
        return self.current_script

    def load_script(self, script_name: str) -> ScriptFile:
        project = self._require_project()
        path = project.root / script_name
        if not path.exists():
            raise FileNotFoundError(f"No existe el archivo {script_name}.")
        self.current_script = ScriptFile(script_name, path)
        return self.current_script

    def list_scripts(self) -> list[ScriptFile]:
        project = self._require_project()
        return project.refresh_scripts()

    def save_current_script(self, content: str, commit: bool = True) -> GitResult | None:
        project = self._require_project()
        script = self._require_script()
        script.write_text(content)
        project.save_manifest()
        if commit:
            return self.git.commit_all(project.root, self._auto_message(f"guarda {script.name}"))
        return None

    def delete_current_script(self) -> GitResult:
        project = self._require_project()
        script = self._require_script()
        deleted_name = script.name
        script.path.unlink(missing_ok=True)
        self.current_script = None
        project.save_manifest()
        return self.git.commit_all(project.root, self._auto_message(f"borra {deleted_name}"))

    def sign_current_script(self, content: str) -> tuple[str, GitResult]:
        project = self._require_project()
        script = self._require_script()
        self.save_current_script(content, commit=False)
        signature = self.signatures.sign_file(script.path, project.signature_path)
        git_result = self.git.commit_all(project.root, self._auto_message(f"firma {script.name}"))
        return signature, git_result

    def verify_current_script(self, content: str) -> bool:
        project = self._require_project()
        script = self._require_script()
        self.save_current_script(content, commit=False)
        return self.signatures.verify_file(script.path, project.signature_path)

    def run_current_script(self, content: str) -> tuple[ExecutionResult, GitResult | None]:
        project = self._require_project()
        script = self._require_script()
        self.save_current_script(content, commit=False)
        result = self.python_runner.run_script(script.path)
        git_result = self.git.commit_all(project.root, self._auto_message(f"ejecuta {script.name}"))
        return result, git_result

    def run_interactive_line(self, line: str) -> ExecutionResult:
        return self.interactive_console.run_line(line)

    def git_history(self) -> str:
        project = self._require_project()
        return self.git.history(project.root)

    def commit_submission(self) -> GitResult:
        project = self._require_project()
        script = self._require_script()
        return self.git.commit_all(project.root, self._auto_message(f"entrega {script.name}"))

    def _require_project(self) -> Project:
        if not self.project:
            raise RuntimeError("Primero debe crear o abrir un proyecto.")
        return self.project

    def _require_script(self) -> ScriptFile:
        if not self.current_script:
            raise RuntimeError("Primero debe seleccionar o crear un script.")
        return self.current_script

    def _auto_message(self, action: str) -> str:
        return f"[auto] {action} {datetime.now().strftime('%Y-%m-%d %H:%M:%S')}"
