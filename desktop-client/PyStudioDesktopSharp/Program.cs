using PyStudioDesktopSharp.UI;

namespace PyStudioDesktopSharp;

internal static class Program
{
    [STAThread]
    private static void Main()
    {
        ApplicationConfiguration.Initialize();

        using var loginForm = new LoginForm();
        if (loginForm.ShowDialog() != DialogResult.OK)
            return;

        var mainForm = new MainForm(loginForm.AuthenticatedApi);
        Application.Run(mainForm);
    }
}