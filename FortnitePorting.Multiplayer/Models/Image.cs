using System.Collections.Concurrent;
using FortnitePorting.Multiplayer.Data;

namespace FortnitePorting.Multiplayer.Models;

public class Image
{
    public UserData User;
    public DateTime Time;
    public List<BaseData> Chunks;
}