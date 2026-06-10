using System.Numerics;
using Parsec.Rendering.Gpu;

namespace Parsec.App;

/// <summary>
/// Mutable live state for the octonion associator-Julia, plus a
/// <see cref="ParamSchema"/>. Mirrors the other *State.cs files; the renderer
/// consumes an immutable <see cref="OctonionParams"/> via <see cref="ToParams"/>.
///
/// Headline knob is the associator strength (eps): 0 is the axisymmetric complex
/// shadow, larger pushes the orbit into the full 8D algebra and breaks the
/// revolution symmetry. The Julia constant c (real/imag) selects which member.
/// p, q (the associator 2-plane) are fixed in this version -- exposing them is a
/// follow-up (recompute AssocNorm, or set it to 2.0 as a safe universal bound).
/// </summary>
public sealed class OctonionState
{
    public int Iterations = 64;
    public float Eps = 0.25f;
    public float CRe = 0.28f;
    public float CIm = 0.012f;
    public float Fudge = 0.9f;

    public bool Stereo = false;
    public float StereoK = 1.0f;
    public float StereoR = 0.8f;

    public OctonionParams ToParams() => new()
    {
        Iterations = Iterations,
        Eps = Eps,
        Fudge = Fudge,
        CLo = new Vector4(CRe, CIm, 0f, 0f),
        Stereo = Stereo,
        StereoK = StereoK,
        StereoR = StereoR,
        // CHi, P*, Q*, AssocNorm, Bailout, BoundRadius use OctonionParams defaults.
    };

    public ParamSchema BuildSchema() => new()
    {
        Parameters = new[]
        {
            new ParamDescriptor {
                Label = "Associator eps", Group = "Octonion", Min = 0.0, Max = 0.5, Decimals = 3,
                Get = () => Eps, Set = v => Eps = (float)v },
            new ParamDescriptor {
                Label = "c (real)", Group = "Julia seed", Min = -1.0, Max = 0.5, Decimals = 3,
                Get = () => CRe, Set = v => CRe = (float)v },
            new ParamDescriptor {
                Label = "c (imag)", Group = "Julia seed", Min = -0.6, Max = 0.6, Decimals = 3,
                Get = () => CIm, Set = v => CIm = (float)v },
            new ParamDescriptor {
                Label = "Iterations", Group = "Render", Min = 16, Max = 500, Decimals = 0,
                Get = () => Iterations, Set = v => Iterations = (int)v },
            new ParamDescriptor {
                Label = "DE fudge", Group = "Render", Min = 0.3, Max = 2.0, Decimals = 2,
                Get = () => Fudge, Set = v => Fudge = (float)v },

            // Slice mode: 0 = flat plane (smooth), 1 = stereographic (curved, reveals
            // organic structure). k frames the wrap; R is the sphere radius -- set it
            // near the boundary (~0.75-0.9) for the separated-lobe sweet spot.
            new ParamDescriptor {
                Label = "Stereographic (0/1)", Group = "Slice", Min = 0, Max = 1, Decimals = 0,
                Get = () => Stereo ? 1.0 : 0.0, Set = v => Stereo = v >= 0.5 },
            new ParamDescriptor {
                Label = "Stereo scale k", Group = "Slice", Min = 0.3, Max = 3.0, Decimals = 2,
                Get = () => StereoK, Set = v => StereoK = (float)v },
            new ParamDescriptor {
                Label = "Stereo radius R", Group = "Slice", Min = 0.3, Max = 1.4, Decimals = 2,
                Get = () => StereoR, Set = v => StereoR = (float)v },
        }
    };
}
