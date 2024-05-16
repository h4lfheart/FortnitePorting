// https://github.com/SamboyCoding/OpenTKAvalonia

using Avalonia.OpenGL;
using OpenTK;

namespace FortnitePorting.OpenGL.OpenTK;

/// <summary>
/// Wrapper to expose GetProcAddress from Avalonia in a manner that OpenTK can consume. 
/// </summary>
class AvaloniaTkContext : IBindingsContext
{
    private readonly GlInterface _glInterface;

    public AvaloniaTkContext(GlInterface glInterface)
    {
        _glInterface = glInterface;
    }

    public IntPtr GetProcAddress(string procName) => _glInterface.GetProcAddress(procName);
}