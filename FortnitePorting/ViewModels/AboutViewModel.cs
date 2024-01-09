using FortnitePorting.Framework;

namespace FortnitePorting.ViewModels;

public class AboutViewModel : ViewModelBase
{
    public void OpenLink(string url)
    {
        Launch(url);
    }
    
    public void Discord()
    {
        Launch(Globals.DISCORD_URL);
    }

    public void KoFi()
    {
        Launch(Globals.KOFI_URL);
    }

    public void GitHub()
    {
        Launch(Globals.GITHUB_URL);
    }
}