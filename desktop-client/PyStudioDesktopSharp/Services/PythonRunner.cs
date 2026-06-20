using System.Diagnostics;

namespace PyStudioDesktopSharp.Services;

public sealed record ExecutionResult(string Stdout, string Stderr, int ReturnCode);

public sealed class PythonRunner
{
    public async Task<ExecutionResult> RunScriptAsync(string scriptPath, int timeoutSeconds = 10)
    {
        if (!File.Exists(scriptPath))
            throw new FileNotFoundException("No existe el script seleccionado.", scriptPath);

        return await RunPythonAsync(new[] { scriptPath }, timeoutSeconds);
    }

    public async Task<ExecutionResult> RunInteractiveLineAsync(string line, int timeoutSeconds = 5)
    {
        if (string.IsNullOrWhiteSpace(line))
            return new ExecutionResult(string.Empty, string.Empty, 0);

        return await RunPythonAsync(new[] { "-c", line }, timeoutSeconds);
    }

    private static async Task<ExecutionResult> RunPythonAsync(string[] args, int timeoutSeconds)
    {
        string[] candidates = OperatingSystem.IsWindows() ? ["python", "py"] : ["python3", "python"];
        Exception? lastError = null;

        foreach (string candidate in candidates)
        {
            try
            {
                return await RunProcessAsync(candidate, args, timeoutSeconds);
            }
            catch (Exception ex)
            {
                lastError = ex;
            }
        }

        throw new InvalidOperationException("No se pudo ejecutar Python. Verifique que Python esté instalado y agregado al PATH.", lastError);
    }

    private static async Task<ExecutionResult> RunProcessAsync(string executable, string[] args, int timeoutSeconds)
    {
        using var process = new Process();
        process.StartInfo = new ProcessStartInfo
        {
            FileName = executable,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
            CreateNoWindow = true
        };

        foreach (string arg in args)
            process.StartInfo.ArgumentList.Add(arg);

        process.Start();
        using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(timeoutSeconds));

        try
        {
            string stdoutTaskResult = string.Empty;
            string stderrTaskResult = string.Empty;

            Task<string> stdoutTask = process.StandardOutput.ReadToEndAsync();
            Task<string> stderrTask = process.StandardError.ReadToEndAsync();
            await process.WaitForExitAsync(cts.Token);
            stdoutTaskResult = await stdoutTask;
            stderrTaskResult = await stderrTask;
            return new ExecutionResult(stdoutTaskResult.TrimEnd(), stderrTaskResult.TrimEnd(), process.ExitCode);
        }
        catch (OperationCanceledException)
        {
            try { process.Kill(true); } catch { }
            return new ExecutionResult(string.Empty, $"La ejecución superó el límite de {timeoutSeconds} segundos y fue detenida.", -1);
        }
    }
}
