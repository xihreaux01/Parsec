using SkiaSharp;

namespace Parsec.Cli.Examples;

/// <summary>
/// One self-contained example. Examples typically build an IFS, run a
/// renderer, and return a bitmap; diagnostic-only examples may return
/// <c>null</c> to indicate that no image was produced.
/// </summary>
public interface IExample
{
    string Name { get; }
    string Description { get; }

    /// <summary>
    /// Run the example. Returns an <see cref="SKBitmap"/> to save as PNG, or
    /// <c>null</c> if this example doesn't produce an image (e.g. a console
    /// diagnostic). The CLI runner is responsible for disposing the bitmap.
    /// </summary>
    SKBitmap? Render();
}
