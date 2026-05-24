from __future__ import annotations

import contextlib
import io
import subprocess
import sys
from code import InteractiveInterpreter
from dataclasses import dataclass
from pathlib import Path


@dataclass
class ExecutionResult:
    stdout: str
    stderr: str
    return_code: int


class PythonRunner:
    """Ejecuta scripts Python en un proceso local separado."""

    def run_script(self, script_path: Path, timeout_seconds: int = 10) -> ExecutionResult:
        try:
            completed = subprocess.run(
                [sys.executable, str(script_path)],
                cwd=str(script_path.parent),
                capture_output=True,
                text=True,
                timeout=timeout_seconds,
            )
            return ExecutionResult(completed.stdout, completed.stderr, completed.returncode)
        except subprocess.TimeoutExpired as exc:
            return ExecutionResult(exc.stdout or "", "Tiempo máximo de ejecución superado.", 124)
        except Exception as exc:
            return ExecutionResult("", f"Error al ejecutar el script: {exc}", 1)


class InteractivePythonConsole:
    """Consola interactiva: conserva variables entre líneas ejecutadas."""

    def __init__(self) -> None:
        self.interpreter = InteractiveInterpreter()

    def run_line(self, line: str) -> ExecutionResult:
        stdout_buffer = io.StringIO()
        stderr_buffer = io.StringIO()
        try:
            with contextlib.redirect_stdout(stdout_buffer), contextlib.redirect_stderr(stderr_buffer):
                more = self.interpreter.runsource(line)
            prompt = "... " if more else ">>> "
            return ExecutionResult(stdout_buffer.getvalue() + prompt, stderr_buffer.getvalue(), 0)
        except Exception as exc:
            return ExecutionResult(stdout_buffer.getvalue(), stderr_buffer.getvalue() + str(exc), 1)
