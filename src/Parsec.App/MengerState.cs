using System.Numerics;
using Parsec.Rendering.Gpu;

namespace Parsec.App;

/// <summary>
/// Mutable live state for the rotated folded Menger-IFS. The scale (default 3 =
/// classic Menger) and offset control the IFS topology; the rotation angles
/// (degrees) compound across iterations as the morph knobs. A distinct
/// aesthetic category in the collection: rectilinear/architectural geometry,
/// the alien-city / temple-fractal family.
/// </summary>
public sealed class MengerState
{
    public int Iterations = 6;
    public float Scale = 3.0f;
    public float OffsetX = 1.0f;
    public float OffsetY = 1.0f;
    public float OffsetZ = 0.0f;

    // Rotation in degrees.
    public float RotXDeg = 5.7f;   // ~0.10 rad
    public float RotYDeg = 4.0f;   // ~0.07 rad
    public float RotZDeg = 2.3f;   // ~0.04 rad

    public float Fudge = 0.8f;

    private const float Deg2Rad = MathF.PI / 180f;

    public MengerParams ToParams() => new()
    {
        Iterations = Iterations,
        Scale = Scale,
        Offset = new Vector3(OffsetX, OffsetY, OffsetZ),
        Rotation = new Vector3(RotXDeg * Deg2Rad, RotYDeg * Deg2Rad, RotZDeg * Deg2Rad),
        Fudge = Fudge,
        BoundRadius = 3.5f,
    };

    public ParamSchema BuildSchema() => new()
    {
        Parameters = new[]
        {
            // Rotation first -- the morph stars (note: smaller effect than rotbox/hybrid
            // because the abs-fold reflects after rotation, but still meaningful).
            new ParamDescriptor {
                Label = "Rotate X", Group = "Rotation (morph)", Min = -30, Max = 30, Decimals = 2,
                Get = () => RotXDeg, Set = v => RotXDeg = (float)v },
            new ParamDescriptor {
                Label = "Rotate Y", Group = "Rotation (morph)", Min = -30, Max = 30, Decimals = 2,
                Get = () => RotYDeg, Set = v => RotYDeg = (float)v },
            new ParamDescriptor {
                Label = "Rotate Z", Group = "Rotation (morph)", Min = -30, Max = 30, Decimals = 2,
                Get = () => RotZDeg, Set = v => RotZDeg = (float)v },

            new ParamDescriptor {
                Label = "Scale", Group = "IFS", Min = 2.0, Max = 5.0, Decimals = 3,
                Get = () => Scale, Set = v => Scale = (float)v },
            new ParamDescriptor {
                Label = "Offset X", Group = "IFS", Min = 0.0, Max = 2.0, Decimals = 3,
                Get = () => OffsetX, Set = v => OffsetX = (float)v },
            new ParamDescriptor {
                Label = "Offset Y", Group = "IFS", Min = 0.0, Max = 2.0, Decimals = 3,
                Get = () => OffsetY, Set = v => OffsetY = (float)v },
            new ParamDescriptor {
                Label = "Offset Z", Group = "IFS", Min = 0.0, Max = 2.0, Decimals = 3,
                Get = () => OffsetZ, Set = v => OffsetZ = (float)v },

            new ParamDescriptor {
                Label = "Iterations", Group = "Quality", Min = 3, Max = 500, Step = 1, Decimals = 0,
                Get = () => Iterations, Set = v => Iterations = (int)Math.Round(v) },
            new ParamDescriptor {
                Label = "DE fudge", Group = "Quality", Min = 0.4, Max = 2.0, Decimals = 2,
                Get = () => Fudge, Set = v => Fudge = (float)v },
        },
    };
}
