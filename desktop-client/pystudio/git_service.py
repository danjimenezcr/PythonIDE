from __future__ import annotations

import shutil
import subprocess
from dataclasses import dataclass
from pathlib import Path


@dataclass
class GitResult:
    ok: bool
    message: str


class GitService:
    """Conexión Git local del IDE.

    No modifica el backend. El repositorio Git se crea dentro del proyecto local
    del estudiante para guardar historial de cambios del código.
    """

    def is_git_available(self) -> bool:
        return shutil.which("git") is not None

    def init_repo(self, project_root: Path) -> GitResult:
        if not self.is_git_available():
            return GitResult(False, "Git no está instalado o no está en PATH.")
        if (project_root / ".git").exists():
            return GitResult(True, "El repositorio Git local ya existe.")
        result = self._run(project_root, ["git", "init"])
        if not result.ok:
            return result
        self._run(project_root, ["git", "config", "user.email", "pystudio.local@desktop"])
        self._run(project_root, ["git", "config", "user.name", "PyStudio Desktop"])
        return GitResult(True, "Repositorio Git local inicializado.")

    def commit_all(self, project_root: Path, message: str) -> GitResult:
        init = self.init_repo(project_root)
        if not init.ok:
            return init
        self._run(project_root, ["git", "add", "."])
        status = self._run(project_root, ["git", "status", "--porcelain"])
        if not status.message.strip():
            return GitResult(True, "No hay cambios nuevos para commitear.")
        result = self._run(project_root, ["git", "commit", "-m", message])
        if result.ok:
            return GitResult(True, f"Commit creado: {message}")
        return result

    def history(self, project_root: Path) -> str:
        if not (project_root / ".git").exists():
            return "Este proyecto todavía no tiene repositorio Git local."
        result = self._run(project_root, ["git", "log", "--oneline", "--decorate", "-10"])
        return result.message if result.message.strip() else "Aún no hay commits."

    def _run(self, cwd: Path, command: list[str]) -> GitResult:
        try:
            completed = subprocess.run(command, cwd=str(cwd), capture_output=True, text=True)
            output = (completed.stdout + completed.stderr).strip()
            return GitResult(completed.returncode == 0, output)
        except Exception as exc:
            return GitResult(False, f"Error ejecutando Git: {exc}")
