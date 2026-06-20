namespace PyStudioDesktopSharp.Patterns;

public interface ITerminalPrinter
{
    string Format(string text);
}

public sealed class PlainTerminalPrinter : ITerminalPrinter
{
    public string Format(string text) => text;
}

public sealed class TimestampTerminalDecorator : ITerminalPrinter
{
    private readonly ITerminalPrinter _inner;

    public TimestampTerminalDecorator(ITerminalPrinter inner)
    {
        _inner = inner;
    }

    public string Format(string text)
    {
        return $"[{DateTime.Now:HH:mm:ss}] {_inner.Format(text)}";
    }
}

public interface IStatusObserver
{
    void UpdateStatus(string message);
}

public sealed class StatusSubject
{
    private readonly List<IStatusObserver> _observers = [];

    public void Attach(IStatusObserver observer)
    {
        if (!_observers.Contains(observer))
            _observers.Add(observer);
    }

    public void Notify(string message)
    {
        foreach (var observer in _observers)
            observer.UpdateStatus(message);
    }
}
