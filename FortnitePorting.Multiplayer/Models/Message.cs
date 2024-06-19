using System.Collections.Concurrent;
using FortnitePorting.Multiplayer.Data;

namespace FortnitePorting.Multiplayer.Models;

public class Message
{
    public UserData User;
    public DateTime Time;
    public MessageData Data;
}