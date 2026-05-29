using System.Numerics;
using Parsec.Rendering.Gpu;

namespace Parsec.App;

/// <summary>
/// Mutable live state for the 3D Phoenix Julia. The (c, p_mem) parameter
/// space is the headline -- c picks the static shape, p_mem the memory
/// strength that gives Phoenix its characteristic curling growth. p=0
/// collapses to Mandelbulb-Julia; p=-0.5 the canonical Phoenix from Ushiki's
/// original 2D formula, lifted here to 3D via Mandelbulb-style n=2 trig.
///
/// The half-cut uses a general plane normal (not axis-aligned), matching the
/// quaternion Julia / Apollonian pattern -- tilted cuts on Phoenix reveal
/// cross-sections that axis cuts often miss.
/// </summary>
public sealed class PhoenixState
{
    public int Iterations = 14;

    // Phoenix constant c (vec3 in 3D Phoenix; 4th component unused).
    public float Cx = 0.4f, Cy = 0.0f, Cz = 0.0f;

    /// <summary>Memory strength. 0 = pure Mandelbulb-Julia, -0.5 = canonical Phoenix.</summary>
    public float PMem = -0.5f;

    public float Bailout = 4.0f;

    // Half-cut (general plane normal, like QJ/Apollonian).
    public int Cut = 1;
    public float PlaneNx = 0.3f, PlaneNy = 0.5f, PlaneNz = 0.8f;
    public float PlaneOffset = 0.0f;

    public float Fudge = 0.85f;

    public PhoenixParams ToParams() => new()
    {
        Iterations = Iterations,
        C = new Vector3(Cx, Cy, Cz),
        PMem = PMem,
        Bailout = Bailout,
        Cut = Cut >= 1,
        PlaneNormal = new Vector3(PlaneNx, PlaneNy, PlaneNz),
        PlaneOffset = PlaneOffset,
        Fudge = Fudge,
        BoundRadius = 4.0f,
    };

    public ParamSchema BuildSchema() => new()
    {
        Parameters = new[]
        {
            // Phoenix shape parameter c.
            new ParamDescriptor {
                Label = "c.x", Group = "Constant c", Min = -1.2, Max = 1.2, Decimals = 3,
                Get = () => Cx, Set = v => Cx = (float)v },
            new ParamDescriptor {
                Label = "c.y", Group = "Constant c", Min = -1.2, Max = 1.2, Decimals = 3,
                Get = () => Cy, Set = v => Cy = (float)v },
            new ParamDescriptor {
                Label = "c.z", Group = "Constant c", Min = -1.2, Max = 1.2, Decimals = 3,
                Get = () => Cz, Set = v => Cz = (float)v },

            // The morph star: memory strength.
            new ParamDescriptor {
                Label = "Memory p (morph star)", Group = "Memory",
                Min = -1.0, Max = 0.5, Decimals = 3,
                Get = () => PMem, Set = v => PMem = (float)v },

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
                Label = "Bailout", Group = "Quality",
                Min = 2.0, Max = 8.0, Decimals = 2,
                Get = () => Bailout, Set = v => Bailout = (float)v },
            new ParamDescriptor {
                Label = "DE fudge", Group = "Quality",
                Min = 0.4, Max = 1.0, Decimals = 2,
                Get = () => Fudge, Set = v => Fudge = (float)v },
        },
    };
}
