using System.Diagnostics;
using System.Text;

namespace PencilReproMaui;

/// <summary>Session log in the same line format as the Swift version (for side-by-side diffing).</summary>
public sealed class DiagLog
{
    public enum Category { App, Touch, State, Tool, Stuck, Ui }

    public static DiagLog Shared { get; } = new();

    readonly object _lock = new();
    readonly List<string> _lines = new();
    readonly Stopwatch _clock = Stopwatch.StartNew();
    const int MaxLines = 200_000;

    public double Elapsed => _clock.Elapsed.TotalSeconds;

    public void Log(string message, Category category)
    {
        var line = $"{DateTime.Now:HH:mm:ss.fff} +{Elapsed,9:F3}s [{category.ToString().ToLowerInvariant()}] {message}";
        Console.WriteLine("PencilRepro " + line);
        lock (_lock)
        {
            _lines.Add(line);
            if (_lines.Count > MaxLines) _lines.RemoveRange(0, _lines.Count - MaxLines);
        }
    }

    public void LogBlock(string header, IEnumerable<string> body, Category category)
    {
        Log(header, category);
        foreach (var l in body) Log("    " + l, category);
    }

    public string Snapshot()
    {
        lock (_lock) return string.Join("\n", _lines);
    }

    public int Count { get { lock (_lock) return _lines.Count; } }
}
