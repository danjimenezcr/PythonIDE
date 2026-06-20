using PyStudioDesktopSharp.Models;

namespace PyStudioDesktopSharp.Services;

public sealed class DesktopFacade
{
    public Project? Project { get; private set; }
    public ScriptFile? CurrentScript { get; private set; }

    private readonly SignatureService _signatures = new();
    private readonly PythonRunner _pythonRunner = new();
    private readonly GitService _git = new();

    public Project CreateProject(string parentDirectory, string projectName)
    {
        Project = Project.Create(parentDirectory, projectName);
        _git.InitRepo(Project.Root);
        _git.CommitAll(Project.Root, AutoMessage("creacion de proyecto"));
        return Project;
    }

    public Project OpenProject(string root)
    {
        Project = Project.Load(root);
        _git.InitRepo(Project.Root);
        return Project;
    }

    public ScriptFile CreateScript(string scriptName)
    {
        var project = RequireProject();
        scriptName = scriptName.Trim();
        if (!scriptName.EndsWith(".py", StringComparison.OrdinalIgnoreCase))
            scriptName += ".py";

        string path = Path.Combine(project.Root, scriptName);
        if (File.Exists(path))
            throw new IOException($"El script {scriptName} ya existe.");

        File.WriteAllText(path, "# Nuevo script PyStudio\n\nprint('Hola desde PyStudio')\n");
        CurrentScript = new ScriptFile(scriptName, path);
        project.SaveManifest();
        _git.CommitAll(project.Root, AutoMessage($"crea {scriptName}"));
        return CurrentScript;
    }

    public ScriptFile LoadScript(string scriptName)
    {
        var project = RequireProject();
        string path = Path.Combine(project.Root, scriptName);
        if (!File.Exists(path))
            throw new FileNotFoundException($"No existe el archivo {scriptName}.");

        CurrentScript = new ScriptFile(scriptName, path);
        return CurrentScript;
    }

    public List<ScriptFile> ListScripts()
    {
        return RequireProject().RefreshScripts();
    }

    public GitResult? SaveCurrentScript(string content, bool commit = true)
    {
        var project = RequireProject();
        var script = RequireScript();
        script.WriteText(content);
        project.SaveManifest();
        return commit ? _git.CommitAll(project.Root, AutoMessage($"guarda {script.Name}")) : null;
    }

    public GitResult DeleteCurrentScript()
    {
        var project = RequireProject();
        var script = RequireScript();
        string deletedName = script.Name;
        if (File.Exists(script.Path)) File.Delete(script.Path);
        CurrentScript = null;
        project.SaveManifest();
        return _git.CommitAll(project.Root, AutoMessage($"borra {deletedName}"));
    }

    public (string Signature, GitResult GitResult) SignCurrentScript(string content)
    {
        var project = RequireProject();
        var script = RequireScript();
        SaveCurrentScript(content, commit: false);
        string signature = _signatures.SignFile(script.Path, project.SignaturePath);
        GitResult gitResult = _git.CommitAll(project.Root, AutoMessage($"firma {script.Name}"));
        return (signature, gitResult);
    }

    public bool VerifyCurrentScript(string content)
    {
        var project = RequireProject();
        var script = RequireScript();
        SaveCurrentScript(content, commit: false);
        return _signatures.VerifyFile(script.Path, project.SignaturePath);
    }

    public async Task<(ExecutionResult Result, GitResult? GitResult)> RunCurrentScriptAsync(string content)
    {
        var project = RequireProject();
        var script = RequireScript();
        SaveCurrentScript(content, commit: false);
        ExecutionResult result = await _pythonRunner.RunScriptAsync(script.Path);
        GitResult gitResult = _git.CommitAll(project.Root, AutoMessage($"ejecuta {script.Name}"));
        return (result, gitResult);
    }

    public async Task<ExecutionResult> RunInteractiveLineAsync(string line)
    {
        return await _pythonRunner.RunInteractiveLineAsync(line);
    }

    public string GitHistory()
    {
        return _git.History(RequireProject().Root);
    }

    public GitResult CommitSubmission()
    {
        var project = RequireProject();
        var script = RequireScript();
        return _git.CommitAll(project.Root, AutoMessage($"entrega {script.Name}"));
    }

    private Project RequireProject()
    {
        return Project ?? throw new InvalidOperationException("Primero debe crear o abrir un proyecto.");
    }

    private ScriptFile RequireScript()
    {
        return CurrentScript ?? throw new InvalidOperationException("Primero debe seleccionar o crear un script.");
    }

    private static string AutoMessage(string action)
    {
        return $"[auto] {action} {DateTime.Now:yyyy-MM-dd HH:mm:ss}";
    }
}
