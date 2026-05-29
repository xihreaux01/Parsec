using System.Reflection;

namespace Parsec.Rendering.Gpu;

/// <summary>
/// Load shader source code from embedded resources in this assembly. Shader
/// files live under <c>Shaders/</c> in the project and are included as
/// EmbeddedResource via the .csproj.
/// </summary>
public static class ShaderLoader
{
    private static readonly Assembly Asm = typeof(ShaderLoader).Assembly;

    /// <summary>
    /// Load the named shader. The name is the bare filename (e.g.
    /// <c>"smoke.comp"</c> or <c>"ifs_de.glsl"</c>). The shader must have
    /// been included as an <c>EmbeddedResource</c> in the project file.
    /// </summary>
    public static string Load(string filename)
    {
        // Embedded resource names are <DefaultNamespace>.<path-with-dots>.<filename>
        // For us: Parsec.Rendering.Gpu.Shaders.smoke.comp
        var resourceName = $"Parsec.Rendering.Gpu.Shaders.{filename}";
        using var stream = Asm.GetManifestResourceStream(resourceName)
            ?? throw new FileNotFoundException(
                $"Embedded shader '{resourceName}' not found. " +
                $"Available resources: {string.Join(", ", Asm.GetManifestResourceNames())}");
        using var reader = new StreamReader(stream);
        var source = reader.ReadToEnd();

        if (string.IsNullOrWhiteSpace(source))
            throw new InvalidOperationException(
                $"Embedded shader '{resourceName}' was found but its contents are empty " +
                $"or whitespace-only ({source.Length} chars). " +
                $"Available resources: {string.Join(", ", Asm.GetManifestResourceNames())}");

        // Defensive ASCII-strip. NVIDIA's GLSL compiler (Cg-derived) rejects
        // non-ASCII bytes even inside comments, producing a baffling
        // "unexpected $end, expecting '::'" error. Khronos glslang tolerates
        // them, so they pass offline validation and only fail on real NVIDIA
        // hardware. Rather than police every shader comment by hand, we strip
        // non-ASCII here. Shader code is always ASCII; only comments would
        // ever contain anything else, so this never changes behavior.
        source = StripNonAscii(source);

        return source;
    }

    /// <summary>
    /// Diagnostic: list all embedded resource names in this assembly.
    /// </summary>
    public static string[] AvailableResources() => Asm.GetManifestResourceNames();

    /// <summary>
    /// Assemble a compute shader from a shared core chunk plus an
    /// entry-point chunk. Produces:
    ///   #version 430 core
    ///   &lt;core&gt;
    ///   &lt;entry&gt;
    /// Neither chunk file contains a <c>#version</c> line — this method adds it.
    /// </summary>
    public static string LoadComposite(string coreFile, string entryFile)
    {
        var core = Load(coreFile);
        var entry = Load(entryFile);
        return "#version 430 core\n" + core + "\n" + entry + "\n";
    }

    /// <summary>
    /// Replace any non-ASCII characters with a space. Preserves line/column
    /// structure (1 char in, 1 char out) so compiler error line numbers stay
    /// accurate.
    /// </summary>
    private static string StripNonAscii(string s)
    {
        char[]? buffer = null;
        for (int i = 0; i < s.Length; i++)
        {
            if (s[i] > 0x7F)
            {
                buffer ??= s.ToCharArray();
                buffer[i] = ' ';
            }
        }
        return buffer is null ? s : new string(buffer);
    }
}
