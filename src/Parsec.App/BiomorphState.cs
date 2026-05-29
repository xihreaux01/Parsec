using System.Numerics;
using Parsec.Rendering.Gpu;

namespace Parsec.App;

/// <summary>
/// Mutable live state for the 3D Pickover biomorph. The (c, B) parameter
/// space is the headline -- c picks the static creature shape (in Julia
/// fashion), B (the componentwise bailout) is the biomorph's defining
/// parameter that gives it the limb-like character. Pickover's canonical
/// is c = (-0.5, 0.5, 0), B = 10, which gives a clear radial multi-armed
/// creature.
///
/// Half-cut uses general plane normal (matches Phoenix/QJ/Apollonian).
/// </summary>
public sealed class BiomorphState
{
    public int Iterations = 16;

    // Julia constant c (vec3 in 3D Mandelbulb-style lift).
    public float Cx = -0.5f, Cy = 0.5f, Cz = 0.0f;

    /// <summary>Componentwise bailout. Pickover's canonical is 10.</summary>
    public float Bailout = 10.0f;

    // Half-cut (general plane normal).
    public int Cut = 1;
    public float PlaneNx = 0.3f, PlaneNy = 0.5f, PlaneNz = 0.8f;
    public float PlaneOffset = 0.0f;

    public float Fudge = 0.85f;

    public BiomorphParams ToParams() => new()
    {
        Iterations = Iterations,
        C = new Vector3(Cx, Cy, Cz),
        Bailout = Bailout,
        Cut = Cut >= 1,
        PlaneNormal = new Vector3(PlaneNx, PlaneNy, PlaneNz),
        PlaneOffset = PlaneOffset,
        Fudge = Fudge,
        BoundRadius = 3.0f,
    };

    public ParamSchema BuildSchema() => new()
    {
        Parameters = new[]
        {
            // Julia constant c.
            new ParamDescriptor {
                Label = "c.x", Group = "Constant c", Min = -1.5, Max = 1.5, Decimals = 3,
                Get = () => Cx, Set = v => Cx = (float)v },
            new ParamDescriptor {
                Label = "c.y", Group = "Constant c", Min = -1.5, Max = 1.5, Decimals = 3,
                Get = () => Cy, Set = v => Cy = (float)v },
            new ParamDescriptor {
                Label = "c.z", Group = "Constant c", Min = -1.5, Max = 1.5, Decimals = 3,
                Get = () => Cz, Set = v => Cz = (float)v },

            // The biomorph's defining parameter.
            new ParamDescriptor {
                Label = "Bailout B (biomorph)", Group = "Escape",
                Min = 1.5, Max = 20.0, Decimals = 2,
                Get = () => Bailout, Set = v => Bailout = (float)v },

            // Half-cut.
            new ParamDescriptor {
                Label = "Cut (0/1)", Group = "Slice & Cut",
                Min = 0, Max = 1, Step = 1, Decimals = 0,
                Get = () => Cut, Set = v => Cut = (int)Math.Round(v) },
            new ParamDescriptor {
                Label = "Plane normal x", Group = "Slice & Cut",
                Min = -1, Max = 1, Decimals = 3,
                Get = () => PlaneNx, Set = v => PlaneNx = (float)v },
            new ParamDescriptor {
                Label = "Plane normal y", Group = "Slice & Cut",
                Min = -1, Max = 1, Decimals = 3,
                Get = () => PlaneNy, Set = v => PlaneNy = (float)v },
            new ParamDescriptor {
                Label = "Plane normal z", Group = "Slice & Cut",
                Min = -1, Max = 1, Decimals = 3,
                Get = () => PlaneNz, Set = v => PlaneNz = (float)v },
            new ParamDescriptor {
                Label = "Cut plane offset", Group = "Slice & Cut",
                Min = -1.5, Max = 1.5, Decimals = 3,
                Get = () => PlaneOffset, Set = v => PlaneOffset = (float)v },

            // Quality.
            new ParamDescriptor {
                Label = "Iterations", Group = "Quality",
                Min = 4, Max = 40, Step = 1, Decimals = 0,
                Get = () => Iterations, Set = v => Iterations = (int)Math.Round(v) },
            new ParamDescriptor {
                Label = "DE fudge", Group = "Quality",
                Min = 0.4, Max = 1.0, Decimals = 2,
                Get = () => Fudge, Set = v => Fudge = (float)v },
        },
    };
}
