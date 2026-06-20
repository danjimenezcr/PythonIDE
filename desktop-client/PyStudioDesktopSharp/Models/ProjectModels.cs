using System.Text.Json;

namespace PyStudioDesktopSharp.Models;

public sealed class Project
{
    public string Name { get; }
    public string Root { get; }

    public string InternalFolder => Path.Combine(Root, ".pystudio");
    public string ManifestPath => Path.Combine(InternalFolder, "manifest.json");
    public string SignaturePath => Path.Combine(InternalFolder, "signatures.json");

    private Project(string name, string root)
    {
        Name = name;
        Root = root;
        Directory.CreateDirectory(InternalFolder);
    }

    public static Project Create(string parentDirectory, string projectName)
    {
        if (string.IsNullOrWhiteSpace(projectName))
            throw new ArgumentException("El nombre del proyecto no puede estar vacío.");

        string root = Path.Combine(parentDirectory, projectName.Trim());
        Directory.CreateDirectory(root);
        var project = new Project(projectName.Trim(), root);
        project.SaveManifest();
        return project;
    }

    public static Project Load(string root)
    {
        if (!Directory.Exists(root))
            throw new DirectoryNotFoundException("La carpeta del proyecto no existe.");

        string name = new DirectoryInfo(root).Name;
        string manifestPath = Path.Combine(root, ".pystudio", "manifest.json");

        if (File.Exists(manifestPath))
        {
            using FileStream stream = File.OpenRead(manifestPath);
            var manifest = JsonSerializer.Deserialize<ProjectManifest>(stream);
            if (!string.IsNullOrWhiteSpace(manifest?.Name))
                name = manifest.Name;
        }

        var project = new Project(name, root);
        project.SaveManifest();
        return project;
    }

    public List<ScriptFile> RefreshScripts()
    {
        Directory.CreateDirectory(Root);
        return Directory
            .EnumerateFiles(Root, "*.py", SearchOption.TopDirectoryOnly)
            .OrderBy(Path.GetFileName)
            .Select(path => new ScriptFile(Path.GetFileName(path), path))
            .ToList();
    }

    public void SaveManifest()
    {
        Directory.CreateDirectory(InternalFolder);
        var manifest = new ProjectManifest
        {
            Name = Name,
            Root = Root,
            Scripts = RefreshScripts().Select(s => s.Name).ToList(),
            UpdatedAt = DateTime.Now
        };

        var options = new JsonSerializerOptions { WriteIndented = true };
        File.WriteAllText(ManifestPath, JsonSerializer.Serialize(manifest, options));
    }
}

public sealed class ProjectManifest
{
    public string Name { get; set; } = string.Empty;
    public string Root { get; set; } = string.Empty;
    public List<string> Scripts { get; set; } = [];
    public DateTime UpdatedAt { get; set; }
}

public sealed class ScriptFile
{
    public string Name { get; }
    public string Path { get; }

    public ScriptFile(string name, string path)
    {
        Name = name;
        Path = path;
    }

    public string ReadText()
    {
        return File.Exists(Path) ? File.ReadAllText(Path) : string.Empty;
    }

    public void WriteText(string content)
    {
        File.WriteAllText(Path, content);
    }
}
