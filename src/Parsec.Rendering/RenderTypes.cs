using System.Numerics;

namespace Parsec.Rendering;

/// <summary>
/// Axis-aligned rectangle in IFS coordinate space, defining the visible region of a render.
/// </summary>
public readonly record struct ViewBounds(float XMin, float YMin, float XMax, float YMax)
{
    public float Width  => XMax - XMin;
    public float Height => YMax - YMin;

    public static ViewBounds UnitSquare { get; } = new(0f, 0f, 1f, 1f);

    /// <summary>
    /// Same center, expanded outward by <paramref name="amount"/> in IFS units on each side.
    /// Useful when an IFS's intermediate iterations can extend outside the obvious bounds.
    /// </summary>
    public ViewBounds Pad(float amount) => new(XMin - amount, YMin - amount, XMax + amount, YMax + amount);

    /// <summary>
    /// IFS-space point to pixel-space point, with Y flipped so that "up" in IFS
    /// space corresponds to "up" in image space.
    /// </summary>
    public Vector2 ToPixel(Vector2 p, int imageWidth, int imageHeight)
    {
        float u = (p.X - XMin) / Width * imageWidth;
        float v = (1f - (p.Y - YMin) / Height) * imageHeight;
        return new Vector2(u, v);
    }
}

/// <summary>
/// RGBA color with components in [0, 1].
/// </summary>
public readonly record struct Color(float R, float G, float B, float A = 1f)
{
    public static Color White { get; } = new(1f, 1f, 1f);
    public static Color Black { get; } = new(0f, 0f, 0f);
    public static Color Transparent { get; } = new(0f, 0f, 0f, 0f);

    public static Color Rgb(byte r, byte g, byte b, byte a = 255) =>
        new(r / 255f, g / 255f, b / 255f, a / 255f);

    public byte R8 => (byte)System.Math.Clamp((int)(R * 255f + 0.5f), 0, 255);
    public byte G8 => (byte)System.Math.Clamp((int)(G * 255f + 0.5f), 0, 255);
    public byte B8 => (byte)System.Math.Clamp((int)(B * 255f + 0.5f), 0, 255);
    public byte A8 => (byte)System.Math.Clamp((int)(A * 255f + 0.5f), 0, 255);
}
