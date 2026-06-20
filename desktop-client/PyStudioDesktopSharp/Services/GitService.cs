using System.Diagnostics;

namespace PyStudioDesktopSharp.Services;

public sealed record GitResult(bool Ok, string Message);

public sealed class GitService
{
    public bool IsGitAvailable() => TryRun(null, "git", "--version", 5).Ok;

    public GitResult InitRepo(string projectRoot)
    {
        if (!IsGitAvailable())
            return new GitResult(false, "Git no está instalado o no está en PATH.");

        if (Directory.Exists(Path.Combine(projectRoot, ".git")))
            return new GitResult(true, "El repositorio Git local ya existe.");

        var init = TryRun(projectRoot, "git", "init", 10);
        if (!init.Ok) return init;

        TryRun(projectRoot, "git", "config user.email pystudio.local@desktop", 5);
        TryRun(projectRoot, "git", "config user.name \"PyStudio Desktop CSharp\"", 5);
        return new GitResult(true, "Repositorio Git local inicializado.");
    }

    public GitResult CommitAll(string projectRoot, string message)
    {
        if (!IsGitAvailable())
            return new GitResult(false, "Git no está instalado o no está en PATH.");

        InitRepo(projectRoot);
        TryRun(projectRoot, "git", "add .", 10);
        var commit = TryRun(projectRoot, "git", $"commit -m \"{Escape(message)}\"", 15);

        string combined = (commit.Message ?? string.Empty).ToLowerInvariant();
        if (!commit.Ok && (combined.Contains("nothing to commit") || combined.Contains("no changes added")))
            return new GitResult(true, "No había cambios nuevos para commitear.");

        return commit.Ok
            ? new GitResult(true, $"Commit automático creado: {message}")
            : commit;
    }

    public string History(string projectRoot)
    {
        if (!IsGitAvailable())
            return "Git no está instalado o no está en PATH.";

        InitRepo(projectRoot);
        var history = TryRun(projectRoot, "git", "log --oneline --decorate --date=local --pretty=format:\"%h | %ad | %s\"", 10);
        return string.IsNullOrWhiteSpace(history.Message) ? "No hay commits todavía." : history.Message;
    }

    private static string Escape(string value) => value.Replace("\"", "'");

    private static GitResult TryRun(string? workingDirectory, string fileName, string arguments, int timeoutSeconds)
    {
        try
        {
            using var process = new Process();
            process.StartInfo = new ProcessStartInfo
            {
                FileName = fileName,
                Arguments = arguments,
                WorkingDirectory = workingDirectory ?? Environment.CurrentDirectory,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true
            };

            process.Start();
            bool exited = process.WaitForExit(timeoutSeconds * 1000);
            if (!exited)
            {
                try { process.Kill(true); } catch { }
                return new GitResult(false, "El comando Git excedió el tiempo permitido.");
            }

            string output = process.StandardOutput.ReadToEnd();
            string error = process.StandardError.ReadToEnd();
            string message = string.IsNullOrWhiteSpace(output) ? error : output + Environment.NewLine + error;
            return new GitResult(process.ExitCode == 0, message.Trim());
        }
        catch (Exception ex)
        {
            return new GitResult(false, ex.Message);
        }
    }
}
