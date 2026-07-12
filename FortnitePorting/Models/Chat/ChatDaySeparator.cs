namespace FortnitePorting.Models.Chat;

public class ChatDaySeparator(string label) : IChatFeedItem
{
    public string Label { get; } = label;
}
