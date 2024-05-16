// https://github.com/SamboyCoding/OpenTKAvalonia

using System.Collections;
using Avalonia.Input;

namespace FortnitePorting.OpenGL.OpenTK;

public class AvaloniaKeyboardState
{
    private readonly BitArray _keys = new((int) Key.DeadCharProcessed + 1);
    private readonly BitArray _keysPrevious = new((int) Key.DeadCharProcessed + 1);

    /// <summary>
    /// Called internally at the start of each frame (before OpenTkRender) to copy keyboard state to the previous frame's buffer.
    /// You probably don't want to call this, but it's public just-in-case.
    /// </summary>
    public void OnFrame()
    {
        _keysPrevious.SetAll(false);
        _keysPrevious.Or(_keys);
    }
    
    /// <summary>
    /// Called to set the state of a key when an input event is received.
    /// </summary>
    /// <param name="key">The key to set</param>
    /// <param name="pressed">True if the key is down, false if it is up.</param>
    public void SetKey(Key key, bool pressed) => _keys.Set((int) key, pressed);

    /// <summary>
    /// Checks if the specified key was down at the start of the current frame.
    /// </summary>
    /// <param name="key">The key to check the state of</param>
    /// <returns>True if the key was down, false if it was up.</returns>
    public bool IsKeyDown(Key key) => _keys.Get((int) key);

    /// <summary>
    /// Checks if the specified key was down at the start of the <em>previous</em> frame.
    /// </summary>
    /// <param name="key">The key to check the state of</param>
    /// <returns>True if the key was down, false if it was up.</returns>
    public bool WasKeyDownLastFrame(Key key) => _keysPrevious.Get((int) key);
}