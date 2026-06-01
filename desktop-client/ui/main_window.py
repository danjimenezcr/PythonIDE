from __future__ import annotations

import tkinter as tk
from tkinter import filedialog, messagebox, simpledialog, ttk
from pathlib import Path
from typing import Any

from pystudio.api_client import ApiClient, ApiError
from pystudio.desktop_facade import DesktopFacade
from pystudio.patterns import (
    PlainTerminalPrinter,
    StatusObserver,
    StatusSubject,
    TimestampTerminalDecorator,
)


class PyStudioDesktop(tk.Tk, StatusObserver):
    def __init__(self) -> None:
        super().__init__()
        self.title("PyStudio Desktop - Cliente Estudiante")
        self.geometry("1250x780")
        self.minsize(1080, 680)

        self.facade = DesktopFacade()
        self.api = ApiClient()
        self.status_subject = StatusSubject()
        self.status_subject.attach(self)
        self.terminal_printer = TimestampTerminalDecorator(PlainTerminalPrinter())

        self.course_by_tree_id: dict[str, dict[str, Any]] = {}
        self.activity_by_tree_id: dict[str, dict[str, Any]] = {}

        self._build_style()
        self._build_ui()
        self._bind_shortcuts()
        self.status_subject.notify("Listo. Cree o abra un proyecto para comenzar.")

    def _build_style(self) -> None:
        style = ttk.Style(self)
        try:
            style.theme_use("clam")
        except tk.TclError:
            pass
        style.configure("Title.TLabel", font=("Segoe UI", 13, "bold"))
        style.configure("Section.TLabel", font=("Segoe UI", 10, "bold"))
        style.configure("Status.TLabel", font=("Segoe UI", 9))

    def _build_ui(self) -> None:
        self.columnconfigure(1, weight=1)
        self.rowconfigure(0, weight=1)

        sidebar = ttk.Frame(self, padding=10)
        sidebar.grid(row=0, column=0, sticky="ns")
        sidebar.columnconfigure(0, weight=1)

        ttk.Label(sidebar, text="PyStudio Desktop", style="Title.TLabel").grid(row=0, column=0, sticky="w")
        ttk.Label(sidebar, text="Proyecto local", style="Section.TLabel").grid(row=1, column=0, sticky="w", pady=(14, 4))
        ttk.Button(sidebar, text="Crear proyecto", command=self.create_project).grid(row=2, column=0, sticky="ew", pady=2)
        ttk.Button(sidebar, text="Abrir proyecto", command=self.open_project).grid(row=3, column=0, sticky="ew", pady=2)
        ttk.Button(sidebar, text="Nuevo script", command=self.create_script).grid(row=4, column=0, sticky="ew", pady=2)
        ttk.Button(sidebar, text="Guardar script", command=self.save_script).grid(row=5, column=0, sticky="ew", pady=2)
        ttk.Button(sidebar, text="Borrar script", command=self.delete_script).grid(row=6, column=0, sticky="ew", pady=2)
        ttk.Button(sidebar, text="Firmar script", command=self.sign_script).grid(row=7, column=0, sticky="ew", pady=2)
        ttk.Button(sidebar, text="Verificar firma", command=self.verify_script).grid(row=8, column=0, sticky="ew", pady=2)
        ttk.Button(sidebar, text="Ver historial Git", command=self.show_git_history).grid(row=9, column=0, sticky="ew", pady=2)

        ttk.Label(sidebar, text="Scripts", style="Section.TLabel").grid(row=10, column=0, sticky="w", pady=(14, 4))
        self.script_list = tk.Listbox(sidebar, height=8, width=29)
        self.script_list.grid(row=11, column=0, sticky="nsew")
        self.script_list.bind("<<ListboxSelect>>", self.on_script_selected)

        ttk.Separator(sidebar).grid(row=12, column=0, sticky="ew", pady=12)
        ttk.Label(sidebar, text="Backend", style="Section.TLabel").grid(row=13, column=0, sticky="w")
        ttk.Label(sidebar, text="URL API").grid(row=14, column=0, sticky="w")
        self.api_url_var = tk.StringVar(value="http://localhost:8000/api")
        ttk.Entry(sidebar, textvariable=self.api_url_var).grid(row=15, column=0, sticky="ew", pady=2)

        ttk.Label(sidebar, text="Correo").grid(row=16, column=0, sticky="w")
        self.email_var = tk.StringVar(value="estudiante@tec.ac.cr")
        ttk.Entry(sidebar, textvariable=self.email_var).grid(row=17, column=0, sticky="ew", pady=2)

        ttk.Label(sidebar, text="Contraseña").grid(row=18, column=0, sticky="w")
        self.password_var = tk.StringVar(value="12345678")
        ttk.Entry(sidebar, textvariable=self.password_var, show="*").grid(row=19, column=0, sticky="ew", pady=2)

        ttk.Button(sidebar, text="Registrar estudiante", command=self.register_student).grid(row=20, column=0, sticky="ew", pady=2)
        ttk.Button(sidebar, text="Iniciar sesión", command=self.login).grid(row=21, column=0, sticky="ew", pady=2)
        ttk.Button(sidebar, text="Unirme a curso", command=self.enroll_course).grid(row=22, column=0, sticky="ew", pady=2)
        ttk.Button(sidebar, text="Cargar cursos/tareas", command=self.load_courses).grid(row=23, column=0, sticky="ew", pady=2)

        main = ttk.Frame(self, padding=(4, 10, 10, 10))
        main.grid(row=0, column=1, sticky="nsew")
        main.columnconfigure(0, weight=4)
        main.columnconfigure(1, weight=1)
        main.rowconfigure(0, weight=4)
        main.rowconfigure(1, weight=2)

        editor_frame = ttk.LabelFrame(main, text="Editor de código")
        editor_frame.grid(row=0, column=0, sticky="nsew", padx=(0, 8))
        editor_frame.rowconfigure(0, weight=1)
        editor_frame.columnconfigure(0, weight=1)
        self.editor = tk.Text(editor_frame, wrap="none", undo=True, font=("Consolas", 11), background="#111827", foreground="#E5E7EB", insertbackground="#E5E7EB")
        self.editor.grid(row=0, column=0, sticky="nsew")
        yscroll = ttk.Scrollbar(editor_frame, command=self.editor.yview)
        yscroll.grid(row=0, column=1, sticky="ns")
        self.editor.configure(yscrollcommand=yscroll.set)

        right_panel = ttk.Frame(main)
        right_panel.grid(row=0, column=1, sticky="nsew")
        right_panel.rowconfigure(1, weight=1)
        right_panel.rowconfigure(3, weight=1)
        right_panel.columnconfigure(0, weight=1)

        ttk.Label(right_panel, text="Cursos inscritos", style="Section.TLabel").grid(row=0, column=0, sticky="w")
        self.course_tree = ttk.Treeview(right_panel, columns=("id", "name"), show="headings", height=7)
        self.course_tree.heading("id", text="ID")
        self.course_tree.heading("name", text="Curso")
        self.course_tree.column("id", width=40, stretch=False)
        self.course_tree.grid(row=1, column=0, sticky="nsew", pady=(2, 8))
        self.course_tree.bind("<<TreeviewSelect>>", self.on_course_selected)

        ttk.Label(right_panel, text="Tareas del curso", style="Section.TLabel").grid(row=2, column=0, sticky="w")
        self.activity_tree = ttk.Treeview(right_panel, columns=("id", "title", "deadline"), show="headings", height=9)
        self.activity_tree.heading("id", text="ID")
        self.activity_tree.heading("title", text="Tarea")
        self.activity_tree.heading("deadline", text="Fecha límite")
        self.activity_tree.column("id", width=40, stretch=False)
        self.activity_tree.column("deadline", width=120, stretch=False)
        self.activity_tree.grid(row=3, column=0, sticky="nsew", pady=(2, 8))

        ttk.Button(right_panel, text="Enviar script como entrega", command=self.submit_current_script).grid(row=4, column=0, sticky="ew")

        terminal_frame = ttk.LabelFrame(main, text="Terminal integrada / Consola Python")
        terminal_frame.grid(row=1, column=0, columnspan=2, sticky="nsew", pady=(8, 0))
        terminal_frame.columnconfigure(0, weight=1)
        terminal_frame.rowconfigure(0, weight=1)
        self.terminal = tk.Text(terminal_frame, height=10, wrap="word", font=("Consolas", 10), background="#0B1020", foreground="#D1D5DB")
        self.terminal.grid(row=0, column=0, sticky="nsew")
        terminal_scroll = ttk.Scrollbar(terminal_frame, command=self.terminal.yview)
        terminal_scroll.grid(row=0, column=1, sticky="ns")
        self.terminal.configure(yscrollcommand=terminal_scroll.set)

        controls = ttk.Frame(terminal_frame)
        controls.grid(row=1, column=0, columnspan=2, sticky="ew", pady=(6, 0))
        controls.columnconfigure(1, weight=1)
        ttk.Button(controls, text="Ejecutar script", command=self.run_script).grid(row=0, column=0, padx=(0, 6))
        self.console_line_var = tk.StringVar()
        ttk.Entry(controls, textvariable=self.console_line_var).grid(row=0, column=1, sticky="ew", padx=(0, 6))
        ttk.Button(controls, text="Ejecutar línea", command=self.run_console_line).grid(row=0, column=2)

        self.status_var = tk.StringVar()
        ttk.Label(self, textvariable=self.status_var, style="Status.TLabel", anchor="w").grid(row=1, column=0, columnspan=2, sticky="ew", padx=10, pady=(0, 6))

    def _bind_shortcuts(self) -> None:
        self.bind("<Control-s>", lambda _event: self.save_script())
        self.bind("<F5>", lambda _event: self.run_script())

    def update_status(self, message: str) -> None:
        self.status_var.set(message)
        self.print_terminal(message)

    def print_terminal(self, text: str) -> None:
        self.terminal.insert("end", self.terminal_printer.format(text) + "\n")
        self.terminal.see("end")

    def create_project(self) -> None:
        parent = filedialog.askdirectory(title="Seleccione dónde crear el proyecto")
        if not parent:
            return
        name = simpledialog.askstring("Nuevo proyecto", "Nombre del proyecto:")
        if not name:
            return
        try:
            project = self.facade.create_project(Path(parent), name.strip())
            self.refresh_scripts()
            self.status_subject.notify(f"Proyecto creado: {project.root}")
        except Exception as exc:
            messagebox.showerror("Error", str(exc))

    def open_project(self) -> None:
        root = filedialog.askdirectory(title="Seleccione la carpeta del proyecto")
        if not root:
            return
        try:
            project = self.facade.open_project(Path(root))
            self.refresh_scripts()
            self.status_subject.notify(f"Proyecto abierto: {project.root}")
        except Exception as exc:
            messagebox.showerror("Error", str(exc))

    def create_script(self) -> None:
        name = simpledialog.askstring("Nuevo script", "Nombre del script:")
        if not name:
            return
        try:
            script = self.facade.create_script(name.strip())
            self.editor.delete("1.0", "end")
            self.editor.insert("1.0", script.read_text())
            self.refresh_scripts()
            self.status_subject.notify(f"Script creado: {script.name}")
        except Exception as exc:
            messagebox.showerror("Error", str(exc))

    def refresh_scripts(self) -> None:
        self.script_list.delete(0, "end")
        try:
            for script in self.facade.list_scripts():
                self.script_list.insert("end", script.name)
        except Exception:
            pass

    def on_script_selected(self, _event: object = None) -> None:
        selection = self.script_list.curselection()
        if not selection:
            return
        script_name = self.script_list.get(selection[0])
        try:
            script = self.facade.load_script(script_name)
            self.editor.delete("1.0", "end")
            self.editor.insert("1.0", script.read_text())
            self.status_subject.notify(f"Script abierto: {script.name}")
        except Exception as exc:
            messagebox.showerror("Error", str(exc))

    def current_editor_content(self) -> str:
        return self.editor.get("1.0", "end-1c")

    def save_script(self) -> None:
        try:
            git_result = self.facade.save_current_script(self.current_editor_content())
            self.status_subject.notify("Script guardado.")
            if git_result:
                self.print_terminal(git_result.message)
            self.refresh_scripts()
        except Exception as exc:
            messagebox.showerror("Error", str(exc))

    def delete_script(self) -> None:
        if not messagebox.askyesno("Confirmar", "¿Desea borrar el script seleccionado?"):
            return
        try:
            git_result = self.facade.delete_current_script()
            self.editor.delete("1.0", "end")
            self.refresh_scripts()
            self.status_subject.notify("Script borrado.")
            self.print_terminal(git_result.message)
        except Exception as exc:
            messagebox.showerror("Error", str(exc))

    def sign_script(self) -> None:
        try:
            signature, git_result = self.facade.sign_current_script(self.current_editor_content())
            self.status_subject.notify(f"Script firmado. Firma: {signature[:16]}...")
            self.print_terminal(git_result.message)
        except Exception as exc:
            messagebox.showerror("Error", str(exc))

    def verify_script(self) -> None:
        try:
            valid = self.facade.verify_current_script(self.current_editor_content())
            if valid:
                messagebox.showinfo("Firma válida", "El script coincide con la firma guardada.")
                self.status_subject.notify("Firma válida.")
            else:
                messagebox.showwarning("Firma inválida", "El script cambió o no tiene firma guardada.")
                self.status_subject.notify("Firma inválida o inexistente.")
        except Exception as exc:
            messagebox.showerror("Error", str(exc))

    def run_script(self) -> None:
        try:
            result, git_result = self.facade.run_current_script(self.current_editor_content())
            self.print_terminal("--- Ejecutando script ---")
            if result.stdout:
                self.terminal.insert("end", result.stdout + "\n")
            if result.stderr:
                self.terminal.insert("end", result.stderr + "\n")
            self.print_terminal(f"Código de salida: {result.return_code}")
            if git_result:
                self.print_terminal(git_result.message)
            self.terminal.see("end")
        except Exception as exc:
            messagebox.showerror("Error", str(exc))

    def run_console_line(self) -> None:
        line = self.console_line_var.get()
        if not line.strip():
            return
        result = self.facade.run_interactive_line(line)
        self.terminal.insert("end", f">>> {line}\n")
        if result.stdout:
            self.terminal.insert("end", result.stdout + "\n")
        if result.stderr:
            self.terminal.insert("end", result.stderr + "\n")
        self.terminal.see("end")
        self.console_line_var.set("")

    def show_git_history(self) -> None:
        try:
            history = self.facade.git_history()
            self.print_terminal("--- Historial Git local ---")
            self.terminal.insert("end", history + "\n")
            self.terminal.see("end")
        except Exception as exc:
            messagebox.showerror("Error", str(exc))

    def configure_api(self) -> None:
        self.api.set_base_url(self.api_url_var.get().strip())

    def register_student(self) -> None:
        self.configure_api()
        full_name = simpledialog.askstring("Registro", "Nombre completo:")
        if not full_name:
            return
        try:
            data = self.api.register_student(full_name.strip(), self.email_var.get().strip(), self.password_var.get())
            self.status_subject.notify(f"Estudiante registrado: {data.get('email', self.email_var.get())}")
        except ApiError as exc:
            messagebox.showerror("Backend", str(exc))

    def login(self) -> None:
        self.configure_api()
        try:
            data = self.api.login(self.email_var.get().strip(), self.password_var.get())
            user = data.get("user", {})
            self.status_subject.notify(f"Sesión iniciada: {user.get('full_name', user.get('email', 'usuario'))}")
        except ApiError as exc:
            messagebox.showerror("Backend", str(exc))

    def enroll_course(self) -> None:
        code = simpledialog.askstring("Unirse a curso", "Código de acceso del curso:")
        if not code:
            return
        self.configure_api()
        try:
            data = self.api.enroll_course(code.strip())
            self.status_subject.notify(data.get("message", "Inscripción exitosa."))
            self.load_courses()
        except ApiError as exc:
            messagebox.showerror("Backend", str(exc))

    def load_courses(self) -> None:
        self.configure_api()
        try:
            courses = self.api.get_courses()
            self.course_tree.delete(*self.course_tree.get_children())
            self.activity_tree.delete(*self.activity_tree.get_children())
            self.course_by_tree_id.clear()
            self.activity_by_tree_id.clear()
            for course in courses:
                item_id = self.course_tree.insert("", "end", values=(course.get("id"), course.get("name")))
                self.course_by_tree_id[item_id] = course
            self.status_subject.notify(f"Cursos cargados: {len(courses)}")
        except ApiError as exc:
            messagebox.showerror("Backend", str(exc))

    def on_course_selected(self, _event: object = None) -> None:
        selection = self.course_tree.selection()
        if not selection:
            return
        course = self.course_by_tree_id.get(selection[0])
        if not course:
            return
        self.load_activities(int(course["id"]))

    def load_activities(self, course_id: int) -> None:
        self.configure_api()
        try:
            activities = self.api.get_activities(course_id)
            self.activity_tree.delete(*self.activity_tree.get_children())
            self.activity_by_tree_id.clear()
            for activity in activities:
                item_id = self.activity_tree.insert("", "end", values=(activity.get("id"), activity.get("title"), activity.get("deadline")))
                self.activity_by_tree_id[item_id] = activity
            self.status_subject.notify(f"Tareas cargadas: {len(activities)}")
        except ApiError as exc:
            messagebox.showerror("Backend", str(exc))

    def selected_activity(self) -> dict[str, Any] | None:
        selection = self.activity_tree.selection()
        if not selection:
            return None
        return self.activity_by_tree_id.get(selection[0])

    def submit_current_script(self) -> None:
        activity = self.selected_activity()
        if not activity:
            messagebox.showwarning("Seleccione una tarea", "Primero seleccione una tarea del curso.")
            return
        try:
            self.facade.save_current_script(self.current_editor_content(), commit=False)
            script = self.facade.current_script
            if script is None:
                raise RuntimeError("Primero seleccione o cree un script.")
            data = self.api.submit_script(int(activity["id"]), script.path)
            git_result = self.facade.commit_submission()
            self.status_subject.notify(data.get("message", "Entrega enviada correctamente."))
            self.print_terminal(git_result.message)
        except (ApiError, Exception) as exc:
            messagebox.showerror("Error", str(exc))


def main() -> None:
    app = PyStudioDesktop()
    app.mainloop()
