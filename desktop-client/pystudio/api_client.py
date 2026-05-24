from __future__ import annotations

import base64
import json
import urllib.error
import urllib.request
from pathlib import Path
from typing import Any


class ApiError(Exception):
    pass


class ApiClient:
    """Cliente HTTP del desktop hacia el backend PHP existente."""

    def __init__(self, base_url: str = "http://localhost:8000/api") -> None:
        self.base_url = base_url.rstrip("/")
        self.token: str | None = None
        self.user: dict[str, Any] | None = None

    def set_base_url(self, base_url: str) -> None:
        self.base_url = base_url.rstrip("/")

    def register_student(self, full_name: str, email: str, password: str) -> dict[str, Any]:
        return self._request("POST", "/auth/register", {
            "full_name": full_name,
            "email": email,
            "password": password,
            "role": "student",
        })["data"]

    def login(self, email: str, password: str) -> dict[str, Any]:
        data = self._request("POST", "/auth/login", {"email": email, "password": password})["data"]
        self.token = data.get("token")
        self.user = data.get("user")
        return data

    def get_courses(self) -> list[dict[str, Any]]:
        return self._request("GET", "/courses")["data"]

    def enroll_course(self, access_code: str) -> dict[str, Any]:
        return self._request("POST", "/courses/enroll", {"access_code": access_code})["data"]

    def get_activities(self, course_id: int) -> list[dict[str, Any]]:
        return self._request("GET", f"/courses/{course_id}/activities")["data"]

    def submit_script(self, activity_id: int, script_path: Path) -> dict[str, Any]:
        encoded = base64.b64encode(script_path.read_bytes()).decode("ascii")
        return self._request("POST", "/submissions", {
            "activity_id": activity_id,
            "files": [{
                "file_name": script_path.name,
                "file_content_base64": encoded,
            }],
        })["data"]

    def _request(self, method: str, endpoint: str, body: dict[str, Any] | None = None) -> dict[str, Any]:
        url = self.base_url + endpoint
        headers = {"Content-Type": "application/json"}
        if self.token:
            headers["Authorization"] = f"Bearer {self.token}"

        payload = None if body is None else json.dumps(body).encode("utf-8")
        request = urllib.request.Request(url, data=payload, headers=headers, method=method)

        try:
            with urllib.request.urlopen(request, timeout=15) as response:
                raw = response.read().decode("utf-8")
        except urllib.error.HTTPError as exc:
            raw_error = exc.read().decode("utf-8")
            try:
                parsed_error = json.loads(raw_error)
                message = parsed_error.get("message", raw_error)
            except json.JSONDecodeError:
                message = raw_error
            raise ApiError(message) from exc
        except urllib.error.URLError as exc:
            raise ApiError(f"No se pudo conectar al backend: {exc.reason}") from exc

        try:
            parsed = json.loads(raw)
        except json.JSONDecodeError as exc:
            raise ApiError(f"El backend respondió algo que no es JSON: {raw[:120]}") from exc

        if not parsed.get("success", False):
            raise ApiError(parsed.get("message", "Error desconocido del backend"))
        return parsed
