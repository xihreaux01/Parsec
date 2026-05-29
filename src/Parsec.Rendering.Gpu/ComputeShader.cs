namespace Parsec.Rendering.Gpu;

/// <summary>
/// A compiled and linked compute shader program. Disposes the GL program
/// when disposed. All GL calls go through an injected <see cref="Gl"/> instance
/// (the unified GetProcAddress layer), so this works identically under the
/// headless CLI context and the Avalonia on-screen context.
/// </summary>
public sealed class ComputeShader : IDisposable
{
    private readonly Gl _gl;
    public uint ProgramHandle { get; }
    public string Name { get; }
    private bool _disposed;

    private ComputeShader(Gl gl, uint handle, string name)
    {
        _gl = gl;
        ProgramHandle = handle;
        Name = name;
    }

    /// <summary>
    /// Compile and link a compute shader from GLSL source. Throws with a
    /// detailed error message including the shader log if compilation or
    /// linking fails.
    /// </summary>
    public static ComputeShader FromSource(Gl gl, string source, string name)
    {
        uint shader = gl.CreateShader(GlConst.ComputeShader);
        gl.ShaderSource(shader, source);
        gl.CompileShader(shader);
        int compileStatus = gl.GetShaderInt(shader, GlConst.CompileStatus);
        if (compileStatus == 0)
        {
            string log = gl.GetShaderInfoLog(shader);
            gl.DeleteShader(shader);
            throw new InvalidOperationException(
                $"Compute shader '{name}' failed to compile:\n{log}\n\nSource:\n{NumberLines(source)}");
        }

        uint program = gl.CreateProgram();
        gl.AttachShader(program, shader);
        gl.LinkProgram(program);
        // Once linked into the program the shader object itself can be detached and deleted.
        gl.DetachShader(program, shader);
        gl.DeleteShader(shader);

        int linkStatus = gl.GetProgramInt(program, GlConst.LinkStatus);
        if (linkStatus == 0)
        {
            string log = gl.GetProgramInfoLog(program);
            gl.DeleteProgram(program);
            throw new InvalidOperationException($"Compute shader program '{name}' failed to link:\n{log}");
        }

        return new ComputeShader(gl, program, name);
    }

    /// <summary>
    /// Bind this shader for subsequent dispatch.
    /// </summary>
    public void Use()
    {
        ThrowIfDisposed();
        _gl.UseProgram(ProgramHandle);
    }

    /// <summary>
    /// Dispatch the shader over a grid of <paramref name="groupsX"/> x
    /// <paramref name="groupsY"/> x <paramref name="groupsZ"/> workgroups.
    /// </summary>
    public void Dispatch(int groupsX, int groupsY = 1, int groupsZ = 1)
    {
        ThrowIfDisposed();
        _gl.DispatchCompute((uint)groupsX, (uint)groupsY, (uint)groupsZ);
    }

    /// <summary>
    /// Find the uniform location for <paramref name="name"/>. Returns -1 if
    /// the uniform doesn't exist (or was optimized out).
    /// </summary>
    public int UniformLocation(string name)
    {
        ThrowIfDisposed();
        return _gl.GetUniformLocation(ProgramHandle, name);
    }

    private void ThrowIfDisposed()
    {
        if (_disposed) throw new ObjectDisposedException(nameof(ComputeShader));
    }

    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;
        _gl.DeleteProgram(ProgramHandle);
    }

    private static string NumberLines(string text)
    {
        var lines = text.Split('\n');
        var width = lines.Length.ToString().Length;
        return string.Join("\n",
            lines.Select((line, i) => $"{(i + 1).ToString().PadLeft(width)}: {line}"));
    }
}
