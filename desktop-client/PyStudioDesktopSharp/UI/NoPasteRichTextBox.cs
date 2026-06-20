namespace PyStudioDesktopSharp.UI;

public sealed class NoPasteRichTextBox : RichTextBox
{
    private const int WmPaste = 0x0302;

    public event EventHandler? PasteBlocked;

    protected override void WndProc(ref Message m)
    {
        if (m.Msg == WmPaste)
        {
            PasteBlocked?.Invoke(this, EventArgs.Empty);
            return;
        }

        base.WndProc(ref m);
    }

    protected override void OnKeyDown(KeyEventArgs e)
    {
        if ((e.Control && e.KeyCode == Keys.V) || (e.Shift && e.KeyCode == Keys.Insert))
        {
            e.SuppressKeyPress = true;
            PasteBlocked?.Invoke(this, EventArgs.Empty);
            return;
        }

        base.OnKeyDown(e);
    }
}
