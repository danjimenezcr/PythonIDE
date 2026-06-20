using PyStudioDesktopSharp.UI;

namespace PyStudioDesktopSharp;

internal static class Program
{
    [STAThread]
    private static void Main()
    {
        ApplicationConfiguration.Initialize();
        Application.Run(new MainForm());
    }
}
