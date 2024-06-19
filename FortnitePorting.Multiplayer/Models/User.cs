using System.Net;
using FortnitePorting.Multiplayer.Data;

namespace FortnitePorting.Multiplayer.Models;

public class User
{
    public IPEndPoint EndPoint;
    public UserData Data;
    public bool IsRegistering;
}