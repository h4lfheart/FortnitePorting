namespace FortnitePorting.Models.API.Responses;

public class OnlineResponse
{
    public OnlineStatus Chat { get; set; } = new();
    public CanvasStatus Canvas { get; set; } = new();
    public OnlineStatus Voting { get; set; } = new();
    public OnlineStatus Leaderboard { get; set; } = new();
}

public class OnlineStatus
{
    public bool Enabled { get; set; } = true;
}

public class CanvasStatus : OnlineStatus
{
    public int Width { get; set; } = 200;
    public int Height { get; set; } = 200;
    public int Cooldown { get; set; } = 30;
}