using System.Numerics;
using Parsec.Rendering.Gpu;

namespace Parsec.App;

/// <summary>
/// Mutable live state for the 3D Apollonian gasket. The headline knob is
/// 'Tangency' -- a multiplier on the canonical inner-sphere centre distance.
/// At 1.0 the four spheres are exactly mutually tangent and the limit set is
/// the connected Coxeter gasket. Pulling tangency above 1 separates the
/// spheres and the gasket fragments into Kleinian fractal dust; pushing below
/// 1 overlaps them for a denser tetrahedral foam. The single slider crosses
/// two qualitative phase boundaries, which is rare for fractal parameters.
///
/// The cut plane (sweep PlaneOffset, optionally Cut on) exposes a 2D
/// Apollonian cross-section on the cut face -- the famous 2D gasket -- with
/// the remaining 3D lobes still visible behind.
/// </summary>
public sealed class ApollonianState
{
    public int Iterations = 25;
    public float Tangency = 1.0f;             // 1.0 = Coxeter packing
    public float OuterRadiusMult = 1.0f;      // 1.0 = canonical Soddy bound
    public float DeEnvelope = 0.30f;          // surface "thickness"

    public int Cut = 1;                       // 0/1 toggle
    public float PlaneNx = 0.3f, PlaneNy = 0.5f, PlaneNz = 0.8f;
    public float PlaneOffset = 0.0f;

    public float Fudge = 0.9f;

    public ApollonianParams ToParams() => new()
    {
        Iterations = Iterations,
        Tangency = Tangency,
        OuterRadiusMult = OuterRadiusMult,
        DeEnvelope = DeEnvelope,
        Cut = Cut >= 1,
        PlaneNormal = new Vector3(PlaneNx, PlaneNy, PlaneNz),
        PlaneOffset = PlaneOffset,
        Fudge = Fudge,
    };

    public ParamSchema BuildSchema() => new()
    {
        Parameters = new[]
        {
            new ParamDescriptor {
                Label = "Tangency (morph star)", Group = "Configuration",
                Min = 0.6, Max = 1.4, Decimals = 3,
                Get = () => Tangency, Set = v => Tangency = (float)v },
            new ParamDescriptor {
                Label = "Outer radius x", Group = "Configuration",
                Min = 0.85, Max = 1.5, Decimals = 3,
                Get = () => OuterRadiusMult, Set = v => OuterRadiusMult = (float)v },
            new ParamDescriptor {
                Label = "DE envelope", Group = "Configuration",
                Min = 0.06, Max = 0.6, Decimals = 3,
                Get = () => DeEnvelope, Set = v => DeEnvelope = (float)v },

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

            new ParamDescriptor {
                Label = "Iterations", Group = "Quality",
                Min = 4, Max = 500, Step = 1, Decimals = 0,
                Get = () => Iterations, Set = v => Iterations = (int)Math.Round(v) },
            new ParamDescriptor {
                Label = "DE fudge", Group = "Quality",
                Min = 0.4, Max = 2.0, Decimals = 2,
                Get = () => Fudge, Set = v => Fudge = (float)v },
        },
    };
}
