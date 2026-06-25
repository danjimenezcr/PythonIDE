using System.Text.RegularExpressions;
using PyStudioDesktopSharp.Models;
using PyStudioDesktopSharp.Patterns;
using PyStudioDesktopSharp.Services;

namespace PyStudioDesktopSharp.UI;

public sealed class MainForm : Form, IStatusObserver
{
    private readonly DesktopFacade _facade = new();
    private readonly ApiClient _api = new();
    private readonly StatusSubject _statusSubject = new();
    private readonly ITerminalPrinter _terminalPrinter = new TimestampTerminalDecorator(new PlainTerminalPrinter());

    private readonly Dictionary<ListViewItem, CourseDto> _courseByItem = [];
    private readonly Dictionary<ListViewItem, ActivityDto> _activityByItem = [];
    private readonly Dictionary<ListViewItem, GroupDto> _groupByItem = [];
    private ListView _groupsView = null!;

    private TextBox _apiUrlBox = null!;
    private TextBox _emailBox = null!;
    private TextBox _passwordBox = null!;
    private ListBox _scriptList = null!;
    private TextBox _lineNumbers = null!;
    private NoPasteRichTextBox _editor = null!;
    private RichTextBox _terminal = null!;
    private TextBox _consoleLineBox = null!;
    private ListView _coursesView = null!;
    private ListView _activitiesView = null!;
    private Label _statusLabel = null!;

    private bool _highlighting;

    private static readonly string[] PythonKeywords =
    [
        "False", "None", "True", "and", "as", "assert", "async", "await", "break", "class", "continue",
        "def", "del", "elif", "else", "except", "finally", "for", "from", "global", "if", "import",
        "in", "is", "lambda", "nonlocal", "not", "or", "pass", "raise", "return", "try", "while", "with", "yield",
        "print", "input", "range", "len", "int", "float", "str", "list", "dict", "set", "tuple"
    ];

    public MainForm()
    {
        Text = "PyStudio Desktop - Cliente Estudiante C#";
        Width = 1280;
        Height = 820;
        MinimumSize = new Size(1100, 700);
        StartPosition = FormStartPosition.CenterScreen;

        _statusSubject.Attach(this);
        BuildUi();
        BindShortcuts();
        _statusSubject.Notify("Listo. Cree o abra un proyecto para comenzar.");
    }

    public void UpdateStatus(string message)
    {
        _statusLabel.Text = message;
        PrintTerminal(message);
    }

    private void BuildUi()
    {
        var root = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 2,
            RowCount = 2,
            Padding = new Padding(8)
        };
        root.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 280));
        root.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        root.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
        root.RowStyles.Add(new RowStyle(SizeType.Absolute, 30));
        Controls.Add(root);

        root.Controls.Add(BuildSidebar(), 0, 0);
        root.Controls.Add(BuildMainArea(), 1, 0);

        _statusLabel = new Label
        {
            Dock = DockStyle.Fill,
            TextAlign = ContentAlignment.MiddleLeft,
            BorderStyle = BorderStyle.Fixed3D
        };
        root.Controls.Add(_statusLabel, 0, 1);
        root.SetColumnSpan(_statusLabel, 2);
    }

    private Control BuildSidebar()
    {
        var sidebar = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 1,
            AutoScroll = true,
            Padding = new Padding(4)
        };
        sidebar.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));

        AddSectionLabel(sidebar, "PyStudio Desktop C#");
        AddSectionLabel(sidebar, "Proyecto local");
        AddButton(sidebar, "Crear proyecto", CreateProject);
        AddButton(sidebar, "Abrir proyecto", OpenProject);
        AddButton(sidebar, "Nuevo script", CreateScript);
        AddButton(sidebar, "Guardar script", SaveScript);
        AddButton(sidebar, "Borrar script", DeleteScript);
        AddButton(sidebar, "Firmar script", SignScript);
        AddButton(sidebar, "Verificar firma", VerifyScript);
        AddButton(sidebar, "Ver historial Git", ShowGitHistory);

        AddSectionLabel(sidebar, "Scripts");
        _scriptList = new ListBox { Dock = DockStyle.Fill, Height = 120 };
        _scriptList.SelectedIndexChanged += (_, _) => OnScriptSelected();
        sidebar.Controls.Add(_scriptList);

        AddSectionLabel(sidebar, "Backend");
        sidebar.Controls.Add(new Label { Text = "URL API", AutoSize = true });
        _apiUrlBox = new TextBox { Text = "http://localhost:8000/api", Dock = DockStyle.Top };
        sidebar.Controls.Add(_apiUrlBox);

        sidebar.Controls.Add(new Label { Text = "Correo", AutoSize = true });
        _emailBox = new TextBox { Text = "estudiante@tec.ac.cr", Dock = DockStyle.Top };
        sidebar.Controls.Add(_emailBox);

        sidebar.Controls.Add(new Label { Text = "Contraseña", AutoSize = true });
        _passwordBox = new TextBox { Text = "12345678", PasswordChar = '*', Dock = DockStyle.Top };
        sidebar.Controls.Add(_passwordBox);

        AddButton(sidebar, "Registrar estudiante", RegisterStudentAsync);
        AddButton(sidebar, "Iniciar sesión", LoginAsync);
        AddButton(sidebar, "Unirme a curso", EnrollCourseAsync);
        AddButton(sidebar, "Cargar cursos/tareas", LoadCoursesAsync);
        AddSectionLabel(sidebar, "Grupos");
        AddButton(sidebar, "Crear grupo", CreateGroupAsync);
        AddButton(sidebar, "Unirme a grupo", JoinGroupAsync);

        return sidebar;
    }

    private Control BuildMainArea()
    {
        var main = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 2,
            RowCount = 2
        };
        main.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 70));
        main.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 30));
        main.RowStyles.Add(new RowStyle(SizeType.Percent, 65));
        main.RowStyles.Add(new RowStyle(SizeType.Percent, 35));

        main.Controls.Add(BuildEditor(), 0, 0);
        main.Controls.Add(BuildRightPanel(), 1, 0);
        var terminalGroup = BuildTerminal();
        main.Controls.Add(terminalGroup, 0, 1);
        main.SetColumnSpan(terminalGroup, 2);

        return main;
    }

    private Control BuildEditor()
    {
        var group = new GroupBox { Text = "Editor de código", Dock = DockStyle.Fill };
        var panel = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 2 };
        panel.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 52));
        panel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        group.Controls.Add(panel);

        _lineNumbers = new TextBox
        {
            Dock = DockStyle.Fill,
            ReadOnly = true,
            Multiline = true,
            ScrollBars = ScrollBars.None,
            Font = new Font("Consolas", 10),
            BackColor = Color.FromArgb(243, 244, 246),
            ForeColor = Color.DimGray,
            TextAlign = HorizontalAlignment.Right
        };

        _editor = new NoPasteRichTextBox
        {
            Dock = DockStyle.Fill,
            Font = new Font("Consolas", 10.5f),
            BackColor = Color.FromArgb(17, 24, 39),
            ForeColor = Color.FromArgb(229, 231, 235),
            WordWrap = false,
            AcceptsTab = true,
            HideSelection = false
        };

        _editor.TextChanged += (_, _) =>
        {
            UpdateLineNumbers();
            HighlightPythonSyntax();
        };
        _editor.VScroll += (_, _) => UpdateLineNumbers();
        _editor.PasteBlocked += (_, _) => _statusSubject.Notify("Pegado bloqueado para cumplir la restricción del IDE.");
        _editor.KeyDown += EditorKeyDown;

        panel.Controls.Add(_lineNumbers, 0, 0);
        panel.Controls.Add(_editor, 1, 0);
        UpdateLineNumbers();
        return group;
    }

    private Control BuildRightPanel()
    {
        var panel = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            RowCount = 7,
            Padding = new Padding(8, 0, 0, 0)
        };
        panel.RowStyles.Add(new RowStyle(SizeType.Absolute, 22));
        panel.RowStyles.Add(new RowStyle(SizeType.Percent, 35));
        panel.RowStyles.Add(new RowStyle(SizeType.Absolute, 22));
        panel.RowStyles.Add(new RowStyle(SizeType.Percent, 35));
        panel.RowStyles.Add(new RowStyle(SizeType.Absolute, 22));
        panel.RowStyles.Add(new RowStyle(SizeType.Percent, 30));
        panel.RowStyles.Add(new RowStyle(SizeType.Absolute, 36));

        // Fila 0 - Label Cursos
        panel.Controls.Add(new Label { Text = "Cursos inscritos", Dock = DockStyle.Fill, Font = new Font(Font, FontStyle.Bold) }, 0, 0);

        // Fila 1 - Lista de cursos
        _coursesView = new ListView
        {
            Dock = DockStyle.Fill,
            View = View.Details,
            FullRowSelect = true,
            MultiSelect = false
        };
        _coursesView.Columns.Add("ID", 50);
        _coursesView.Columns.Add("Curso", 210);
        _coursesView.SelectedIndexChanged += (_, _) => OnCourseSelectedAsync();
        panel.Controls.Add(_coursesView, 0, 1);

        // Fila 2 - Label Tareas
        panel.Controls.Add(new Label { Text = "Tareas del curso", Dock = DockStyle.Fill, Font = new Font(Font, FontStyle.Bold) }, 0, 2);

        // Fila 3 - Lista de actividades
        _activitiesView = new ListView
        {
            Dock = DockStyle.Fill,
            View = View.Details,
            FullRowSelect = true,
            MultiSelect = false
        };
        _activitiesView.Columns.Add("ID", 50);
        _activitiesView.Columns.Add("Tarea", 150);
        _activitiesView.Columns.Add("Fecha límite", 120);
        panel.Controls.Add(_activitiesView, 0, 3);

        // Fila 4 - Label Grupos
        panel.Controls.Add(new Label { Text = "Grupos del curso", Dock = DockStyle.Fill, Font = new Font(Font, FontStyle.Bold) }, 0, 4);

        // Fila 5 - Lista de grupos
        _groupsView = new ListView
        {
            Dock = DockStyle.Fill,
            View = View.Details,
            FullRowSelect = true,
            MultiSelect = false
        };
        _groupsView.Columns.Add("ID", 50);
        _groupsView.Columns.Add("Grupo", 120);
        _groupsView.Columns.Add("Código", 90);
        _groupsView.Columns.Add("Miembros", 60);
        panel.Controls.Add(_groupsView, 0, 5);

        // Fila 6 - Botón enviar entrega
        var submit = new Button { Text = "Enviar script como entrega", Dock = DockStyle.Fill };
        submit.Click += async (_, _) => await SafeAsync(SubmitCurrentScriptAsync);
        panel.Controls.Add(submit, 0, 6);

        return panel;
    }

    private Control BuildTerminal()
    {
        var group = new GroupBox { Text = "Terminal integrada / Consola Python", Dock = DockStyle.Fill };
        var panel = new TableLayoutPanel { Dock = DockStyle.Fill, RowCount = 2 };
        panel.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
        panel.RowStyles.Add(new RowStyle(SizeType.Absolute, 36));
        group.Controls.Add(panel);

        _terminal = new RichTextBox
        {
            Dock = DockStyle.Fill,
            ReadOnly = true,
            Font = new Font("Consolas", 10),
            BackColor = Color.FromArgb(11, 16, 32),
            ForeColor = Color.FromArgb(209, 213, 219)
        };
        panel.Controls.Add(_terminal, 0, 0);

        var controls = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 3 };
        controls.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 120));
        controls.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        controls.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 120));

        var runButton = new Button { Text = "Ejecutar script", Dock = DockStyle.Fill };
        runButton.Click += async (_, _) => await SafeAsync(RunScriptAsync);
        controls.Controls.Add(runButton, 0, 0);

        _consoleLineBox = new TextBox { Dock = DockStyle.Fill };
        controls.Controls.Add(_consoleLineBox, 1, 0);

        var runLine = new Button { Text = "Ejecutar línea", Dock = DockStyle.Fill };
        runLine.Click += async (_, _) => await SafeAsync(RunConsoleLineAsync);
        controls.Controls.Add(runLine, 2, 0);

        panel.Controls.Add(controls, 0, 1);
        return group;
    }

    private void AddSectionLabel(TableLayoutPanel panel, string text)
    {
        panel.Controls.Add(new Label
        {
            Text = text,
            Dock = DockStyle.Top,
            AutoSize = true,
            Font = new Font(Font, FontStyle.Bold),
            Padding = new Padding(0, 10, 0, 4)
        });
    }

    private void AddButton(TableLayoutPanel panel, string text, Action action)
    {
        var button = new Button { Text = text, Dock = DockStyle.Top, Height = 30, Margin = new Padding(0, 2, 0, 2) };
        button.Click += (_, _) => Safe(action);
        panel.Controls.Add(button);
    }

    private void AddButton(TableLayoutPanel panel, string text, Func<Task> action)
    {
        var button = new Button { Text = text, Dock = DockStyle.Top, Height = 30, Margin = new Padding(0, 2, 0, 2) };
        button.Click += async (_, _) => await SafeAsync(action);
        panel.Controls.Add(button);
    }

    private void BindShortcuts()
    {
        KeyPreview = true;
        KeyDown += (_, e) =>
        {
            if (e.Control && e.KeyCode == Keys.S)
            {
                e.SuppressKeyPress = true;
                SaveScript();
            }
            else if (e.KeyCode == Keys.F5)
            {
                e.SuppressKeyPress = true;
                _ = SafeAsync(RunScriptAsync);
            }
        };
    }

    private void ConfigureApi()
    {
        _api.SetBaseUrl(_apiUrlBox.Text);
    }

    private string CurrentEditorContent() => _editor.Text;

    private void CreateProject()
    {
        using var dialog = new FolderBrowserDialog { Description = "Seleccione dónde crear el proyecto" };
        if (dialog.ShowDialog(this) != DialogResult.OK) return;

        string? name = InputDialog.Show("Nuevo proyecto", "Nombre del proyecto:", owner: this);
        if (string.IsNullOrWhiteSpace(name)) return;

        var project = _facade.CreateProject(dialog.SelectedPath, name.Trim());
        RefreshScripts();
        _statusSubject.Notify($"Proyecto creado: {project.Root}");
    }

    private void OpenProject()
    {
        using var dialog = new FolderBrowserDialog { Description = "Seleccione la carpeta del proyecto" };
        if (dialog.ShowDialog(this) != DialogResult.OK) return;

        var project = _facade.OpenProject(dialog.SelectedPath);
        RefreshScripts();
        _statusSubject.Notify($"Proyecto abierto: {project.Root}");
    }

    private void CreateScript()
    {
        string? name = InputDialog.Show("Nuevo script", "Nombre del script:", "main.py", this);
        if (string.IsNullOrWhiteSpace(name)) return;

        var script = _facade.CreateScript(name.Trim());
        _editor.Text = script.ReadText();
        RefreshScripts();
        _statusSubject.Notify($"Script creado: {script.Name}");
    }

    private void RefreshScripts()
    {
        _scriptList.Items.Clear();
        try
        {
            foreach (var script in _facade.ListScripts())
                _scriptList.Items.Add(script.Name);
        }
        catch
        {
            // No hay proyecto abierto todavía.
        }
    }

    private void OnScriptSelected()
    {
        if (_scriptList.SelectedItem is not string scriptName) return;

        var script = _facade.LoadScript(scriptName);
        _editor.Text = script.ReadText();
        _statusSubject.Notify($"Script abierto: {script.Name}");
    }

    private void SaveScript()
    {
        var gitResult = _facade.SaveCurrentScript(CurrentEditorContent());
        _statusSubject.Notify("Script guardado.");
        if (gitResult is not null) PrintTerminal(gitResult.Message);
        RefreshScripts();
    }

    private void DeleteScript()
    {
        if (MessageBox.Show(this, "¿Desea borrar el script seleccionado?", "Confirmar", MessageBoxButtons.YesNo, MessageBoxIcon.Question) != DialogResult.Yes)
            return;

        var gitResult = _facade.DeleteCurrentScript();
        _editor.Clear();
        RefreshScripts();
        _statusSubject.Notify("Script borrado.");
        PrintTerminal(gitResult.Message);
    }

    private void SignScript()
    {
        var (signature, gitResult) = _facade.SignCurrentScript(CurrentEditorContent());
        _statusSubject.Notify($"Script firmado. Firma: {signature[..Math.Min(16, signature.Length)]}...");
        PrintTerminal(gitResult.Message);
    }

    private void VerifyScript()
    {
        bool valid = _facade.VerifyCurrentScript(CurrentEditorContent());
        if (valid)
        {
            MessageBox.Show(this, "El script coincide con la firma guardada.", "Firma válida", MessageBoxButtons.OK, MessageBoxIcon.Information);
            _statusSubject.Notify("Firma válida.");
        }
        else
        {
            MessageBox.Show(this, "El script cambió o no tiene firma guardada.", "Firma inválida", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            _statusSubject.Notify("Firma inválida o inexistente.");
        }
    }

    private async Task RunScriptAsync()
    {
        var (result, gitResult) = await _facade.RunCurrentScriptAsync(CurrentEditorContent());
        PrintTerminal("--- Ejecutando script ---");
        if (!string.IsNullOrWhiteSpace(result.Stdout)) AppendTerminalRaw(result.Stdout + Environment.NewLine);
        if (!string.IsNullOrWhiteSpace(result.Stderr)) AppendTerminalRaw(result.Stderr + Environment.NewLine);
        PrintTerminal($"Código de salida: {result.ReturnCode}");
        if (gitResult is not null) PrintTerminal(gitResult.Message);
    }

    private async Task RunConsoleLineAsync()
    {
        string line = _consoleLineBox.Text;
        if (string.IsNullOrWhiteSpace(line)) return;

        var result = await _facade.RunInteractiveLineAsync(line);
        AppendTerminalRaw($">>> {line}{Environment.NewLine}");
        if (!string.IsNullOrWhiteSpace(result.Stdout)) AppendTerminalRaw(result.Stdout + Environment.NewLine);
        if (!string.IsNullOrWhiteSpace(result.Stderr)) AppendTerminalRaw(result.Stderr + Environment.NewLine);
        _consoleLineBox.Clear();
    }

    private void ShowGitHistory()
    {
        PrintTerminal("--- Historial Git local ---");
        AppendTerminalRaw(_facade.GitHistory() + Environment.NewLine);
    }

    private async Task RegisterStudentAsync()
    {
        ConfigureApi();
        string? fullName = InputDialog.Show("Registro", "Nombre completo:", owner: this);
        if (string.IsNullOrWhiteSpace(fullName)) return;

        var data = await _api.RegisterStudentAsync(fullName.Trim(), _emailBox.Text.Trim(), _passwordBox.Text);
        _statusSubject.Notify($"Estudiante registrado: {data.Email ?? _emailBox.Text.Trim()}");
    }

    private async Task LoginAsync()
    {
        ConfigureApi();
        var data = await _api.LoginAsync(_emailBox.Text.Trim(), _passwordBox.Text);
        string displayName = data.User?.FullName ?? data.User?.Email ?? "usuario";
        _statusSubject.Notify($"Sesión iniciada: {displayName}");
    }

    private async Task EnrollCourseAsync()
    {
        string? code = InputDialog.Show("Unirse a curso", "Código de acceso del curso:", owner: this);
        if (string.IsNullOrWhiteSpace(code)) return;

        ConfigureApi();
        await _api.EnrollCourseAsync(code.Trim());
        _statusSubject.Notify("Inscripción solicitada correctamente.");
        await LoadCoursesAsync();
    }

    private async Task LoadCoursesAsync()
    {
        ConfigureApi();
        var courses = await _api.GetCoursesAsync();
        _coursesView.Items.Clear();
        _activitiesView.Items.Clear();
        _courseByItem.Clear();
        _activityByItem.Clear();

        foreach (var course in courses)
        {
            var item = new ListViewItem(course.Id.ToString());
            item.SubItems.Add(course.Name ?? "Sin nombre");
            _coursesView.Items.Add(item);
            _courseByItem[item] = course;
        }

        _statusSubject.Notify($"Cursos cargados: {courses.Count}");
    }

    private async void OnCourseSelectedAsync()
    {
        if (_coursesView.SelectedItems.Count == 0) return;
        var item = _coursesView.SelectedItems[0];
        if (!_courseByItem.TryGetValue(item, out var course)) return;
        await SafeAsync(() => LoadActivitiesAsync(course.Id));
        await SafeAsync(() => LoadGroupsAsync(course.Id));
    }

    private async Task LoadActivitiesAsync(int courseId)
    {
        ConfigureApi();
        var activities = await _api.GetActivitiesAsync(courseId);
        _activitiesView.Items.Clear();
        _activityByItem.Clear();

        foreach (var activity in activities)
        {
            var item = new ListViewItem(activity.Id.ToString());
            item.SubItems.Add(activity.Title ?? "Sin título");
            item.SubItems.Add(activity.Deadline ?? "");
            _activitiesView.Items.Add(item);
            _activityByItem[item] = activity;
        }

        _statusSubject.Notify($"Tareas cargadas: {activities.Count}");
    }

    private ActivityDto? SelectedActivity()
    {
        if (_activitiesView.SelectedItems.Count == 0) return null;
        var item = _activitiesView.SelectedItems[0];
        return _activityByItem.TryGetValue(item, out var activity) ? activity : null;
    }

    private async Task SubmitCurrentScriptAsync()
    {
        var activity = SelectedActivity();
        if (activity is null)
        {
            MessageBox.Show(this, "Primero seleccione una tarea del curso.", "Seleccione una tarea", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            return;
        }

        _facade.SaveCurrentScript(CurrentEditorContent(), commit: false);
        var script = _facade.CurrentScript ?? throw new InvalidOperationException("Primero seleccione o cree un script.");

        ConfigureApi();
        var data = await _api.SubmitScriptAsync(activity.Id, script.Path);
        var gitResult = _facade.CommitSubmission();
        _statusSubject.Notify(data.Message ?? $"Entrega enviada correctamente. ID: {data.SubmissionId}");
        PrintTerminal(gitResult.Message);
    }

    private void PrintTerminal(string text)
    {
        AppendTerminalRaw(_terminalPrinter.Format(text) + Environment.NewLine);
    }

    private void AppendTerminalRaw(string text)
    {
        _terminal.SelectionStart = _terminal.TextLength;
        _terminal.SelectionLength = 0;
        _terminal.AppendText(text);
        _terminal.ScrollToCaret();
    }

    private void UpdateLineNumbers()
    {
        int lines = Math.Max(1, _editor.Lines.Length);
        _lineNumbers.Text = string.Join(Environment.NewLine, Enumerable.Range(1, lines));
    }

    private void HighlightPythonSyntax()
    {
        if (_highlighting) return;
        if (_editor.TextLength > 20000) return;

        _highlighting = true;
        int originalStart = _editor.SelectionStart;
        int originalLength = _editor.SelectionLength;

        _editor.SuspendLayout();
        _editor.SelectAll();
        _editor.SelectionColor = Color.FromArgb(229, 231, 235);

        HighlightRegex(@"#.*$", Color.FromArgb(107, 114, 128), RegexOptions.Multiline);
        HighlightRegex("'[^'\\r\\n]*(?:\\\\.[^'\\r\\n]*)*'|\"[^\"\\r\\n]*(?:\\\\.[^\"\\r\\n]*)*\"", Color.FromArgb(252, 211, 77));
        HighlightRegex(@"\b(" + string.Join("|", PythonKeywords.Select(Regex.Escape)) + @")\b", Color.FromArgb(96, 165, 250));

        _editor.SelectionStart = Math.Min(originalStart, _editor.TextLength);
        _editor.SelectionLength = Math.Min(originalLength, _editor.TextLength - _editor.SelectionStart);
        _editor.SelectionColor = Color.FromArgb(229, 231, 235);
        _editor.ResumeLayout();
        _highlighting = false;
    }

    private void HighlightRegex(string pattern, Color color, RegexOptions options = RegexOptions.None)
    {
        foreach (Match match in Regex.Matches(_editor.Text, pattern, options))
        {
            _editor.Select(match.Index, match.Length);
            _editor.SelectionColor = color;
        }
    }

    private void EditorKeyDown(object? sender, KeyEventArgs e)
    {
        if (e.KeyCode != Keys.Enter) return;

        int currentLineIndex = _editor.GetLineFromCharIndex(_editor.SelectionStart);
        string currentLine = currentLineIndex < _editor.Lines.Length ? _editor.Lines[currentLineIndex] : string.Empty;
        string indentation = Regex.Match(currentLine, @"^\s*").Value;
        if (currentLine.TrimEnd().EndsWith(':')) indentation += "    ";

        e.SuppressKeyPress = true;
        _editor.SelectedText = Environment.NewLine + indentation;
    }

    private void Safe(Action action)
    {
        try
        {
            action();
        }
        catch (Exception ex)
        {
            MessageBox.Show(this, ex.Message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
    }

    private async Task SafeAsync(Func<Task> action)
    {
        try
        {
            UseWaitCursor = true;
            await action();
        }
        catch (Exception ex)
        {
            MessageBox.Show(this, ex.Message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
        finally
        {
            UseWaitCursor = false;
        }
    }
    private async Task CreateGroupAsync()
    {
        var course = SelectedCourse();
        if (course is null)
        {
            MessageBox.Show(this, "Primero seleccione un curso.", "Seleccione un curso", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            return;
        }

        string? name = InputDialog.Show("Crear grupo", "Nombre del grupo:", owner: this);
        if (string.IsNullOrWhiteSpace(name)) return;

        ConfigureApi();
        var group = await _api.CreateGroupAsync(course.Id, name.Trim());
        _statusSubject.Notify($"Grupo creado: {group.Name}. Código de invitación: {group.InviteCode}");
        MessageBox.Show(this, $"Grupo creado exitosamente.\nCódigo de invitación: {group.InviteCode}", "Grupo creado", MessageBoxButtons.OK, MessageBoxIcon.Information);
        await LoadGroupsAsync(course.Id);
    }

    private async Task JoinGroupAsync()
    {
        string? code = InputDialog.Show("Unirse a grupo", "Código de invitación:", owner: this);
        if (string.IsNullOrWhiteSpace(code)) return;

        ConfigureApi();
        var group = await _api.JoinGroupAsync(code.Trim());
        _statusSubject.Notify($"Te uniste al grupo: {group.GroupName ?? group.Message}");
        
        var course = SelectedCourse();
        if (course is not null) await LoadGroupsAsync(course.Id);
    }

    private async Task LoadGroupsAsync(int courseId)
    {
        ConfigureApi();
        var groups = await _api.GetGroupsAsync(courseId);
        _groupsView.Items.Clear();
        _groupByItem.Clear();

        foreach (var group in groups)
        {
            var item = new ListViewItem(group.Id.ToString());
            item.SubItems.Add(group.Name ?? "Sin nombre");
            item.SubItems.Add(group.InviteCode ?? "");
            item.SubItems.Add(group.MemberCount.ToString());
            _groupsView.Items.Add(item);
            _groupByItem[item] = group;
        }

        _statusSubject.Notify($"Grupos cargados: {groups.Count}");
    }

    private CourseDto? SelectedCourse()
    {
        if (_coursesView.SelectedItems.Count == 0) return null;
        var item = _coursesView.SelectedItems[0];
        return _courseByItem.TryGetValue(item, out var course) ? course : null;
    }
}
