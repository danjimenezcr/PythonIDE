from __future__ import annotations

import json
from dataclasses import dataclass, field
from datetime import datetime
from pathlib import Path


@dataclass
class ScriptFile:
    name: str
    path: Path

    def read_text(self) -> str:
        if not self.path.exists():
            return ""
        return self.path.read_text(encoding="utf-8")

    def write_text(self, content: str) -> None:
        self.path.write_text(content, encoding="utf-8")


@dataclass
class Project:
    name: str
    root: Path
    scripts: list[ScriptFile] = field(default_factory=list)

    @property
    def pystudio_dir(self) -> Path:
        return self.root / ".pystudio"

    @property
    def manifest_path(self) -> Path:
        return self.pystudio_dir / "project.json"

    @property
    def signature_path(self) -> Path:
        return self.pystudio_dir / "signatures.json"

    def refresh_scripts(self) -> list[ScriptFile]:
        self.scripts = [ScriptFile(path.name, path) for path in sorted(self.root.glob("*.py"))]
        return self.scripts

    def save_manifest(self) -> None:
        self.pystudio_dir.mkdir(parents=True, exist_ok=True)
        self.refresh_scripts()
        payload = {
            "name": self.name,
            "root": str(self.root),
            "updated_at": datetime.now().isoformat(timespec="seconds"),
            "scripts": [script.name for script in self.scripts],
        }
        self.manifest_path.write_text(json.dumps(payload, indent=2, ensure_ascii=False), encoding="utf-8")

    @staticmethod
    def load(root: Path) -> "Project":
        manifest = root / ".pystudio" / "project.json"
        if manifest.exists():
            data = json.loads(manifest.read_text(encoding="utf-8"))
            project = Project(name=data.get("name", root.name), root=root)
        else:
            project = Project(name=root.name, root=root)
        project.refresh_scripts()
        return project
