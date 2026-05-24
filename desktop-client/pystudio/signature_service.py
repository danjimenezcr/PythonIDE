from __future__ import annotations

import hashlib
import hmac
import json
from datetime import datetime
from pathlib import Path


class SignatureService:
    """Firma local de integridad para el script del estudiante.

    Esta firma local permite demostrar en el frontend que el archivo no fue alterado
    después de firmarse. Al enviar una tarea, el backend mantiene su propia firma.
    """

    def __init__(self, secret: str = "PYSTUDIO_LOCAL_SIGNATURE_2026") -> None:
        self._secret = secret.encode("utf-8")

    def sign_file(self, script_path: Path, signature_path: Path) -> str:
        signature = self._hash(script_path.read_bytes())
        data = self._load(signature_path)
        data[script_path.name] = {
            "signature": signature,
            "signed_at": datetime.now().isoformat(timespec="seconds"),
            "algorithm": "HMAC-SHA256",
        }
        self._save(signature_path, data)
        return signature

    def verify_file(self, script_path: Path, signature_path: Path) -> bool:
        data = self._load(signature_path)
        saved = data.get(script_path.name, {}).get("signature")
        if not saved:
            return False
        return hmac.compare_digest(saved, self._hash(script_path.read_bytes()))

    def _hash(self, content: bytes) -> str:
        return hmac.new(self._secret, content, hashlib.sha256).hexdigest()

    def _load(self, signature_path: Path) -> dict:
        signature_path.parent.mkdir(parents=True, exist_ok=True)
        if not signature_path.exists():
            return {}
        return json.loads(signature_path.read_text(encoding="utf-8"))

    def _save(self, signature_path: Path, data: dict) -> None:
        signature_path.write_text(json.dumps(data, indent=2, ensure_ascii=False), encoding="utf-8")
