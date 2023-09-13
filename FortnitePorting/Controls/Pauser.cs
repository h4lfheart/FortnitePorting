using System.Threading.Tasks;

namespace FortnitePorting.Controls;

public class Pauser
{
    public bool IsPaused;

    public void Pause()
    {
        IsPaused = true;
    }

    public void Unpause()
    {
        IsPaused = false;
    }

    public async Task WaitIfPaused()
    {
        while (IsPaused)
        {
            await Task.Delay(1);
        }
    }
}