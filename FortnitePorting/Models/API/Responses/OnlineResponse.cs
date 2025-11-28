namespace FortnitePorting.Models.API.Responses;

public class OnlineResponse
{
    public OnlineStatus Chat { get; set; } = new();
    public OnlineStatus Voting { get; set; } = new();
    public OnlineStatus Leaderboard { get; set; } = new();
}

public class OnlineStatus
{
    public bool Enabled { get; set; } = true;
}
