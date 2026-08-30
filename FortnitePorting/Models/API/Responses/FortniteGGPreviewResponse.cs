namespace FortnitePorting.Models.API.Responses;

public record FortniteGGPreviewResponse(int ItemId)
{
    public string VideoUrl => $"https://fnggcdn.com/items/{ItemId}/video-sd.mp4";
    public string CosmeticsUrl => $"https://fortnite.gg/cosmetics?id={ItemId}";
}
