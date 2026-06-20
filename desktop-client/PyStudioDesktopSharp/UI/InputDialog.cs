namespace PyStudioDesktopSharp.UI;

public sealed class InputDialog : Form
{
    private readonly TextBox _input;

    private InputDialog(string title, string prompt, string defaultValue = "")
    {
        Text = title;
        FormBorderStyle = FormBorderStyle.FixedDialog;
        StartPosition = FormStartPosition.CenterParent;
        MinimizeBox = false;
        MaximizeBox = false;
        Width = 420;
        Height = 160;

        var label = new Label
        {
            Text = prompt,
            Left = 12,
            Top = 12,
            Width = 380,
            AutoSize = false
        };

        _input = new TextBox
        {
            Left = 12,
            Top = 40,
            Width = 380,
            Text = defaultValue
        };

        var ok = new Button
        {
            Text = "Aceptar",
            DialogResult = DialogResult.OK,
            Left = 226,
            Width = 80,
            Top = 78
        };

        var cancel = new Button
        {
            Text = "Cancelar",
            DialogResult = DialogResult.Cancel,
            Left = 312,
            Width = 80,
            Top = 78
        };

        Controls.Add(label);
        Controls.Add(_input);
        Controls.Add(ok);
        Controls.Add(cancel);
        AcceptButton = ok;
        CancelButton = cancel;
    }

    public static string? Show(string title, string prompt, string defaultValue = "", IWin32Window? owner = null)
    {
        using var dialog = new InputDialog(title, prompt, defaultValue);
        return dialog.ShowDialog(owner) == DialogResult.OK ? dialog._input.Text : null;
    }
}
