using System;
using System.Text.Json.Serialization;

namespace FortnitePorting.Models.Chat;

public class BroadcastMessage
{
    [JsonPropertyName("id")] public string Id { get; set; } = string.Empty;
    [JsonPropertyName("user_id")] public string UserId { get; set; } = string.Empty;
    [JsonPropertyName("timestamp")] public DateTime Timestamp { get; set; }
    [JsonPropertyName("text")] public string Text { get; set; } = string.Empty;
    [JsonPropertyName("application")] public string Application { get; set; } = string.Empty;
    [JsonPropertyName("was_edited")] public bool WasEdited { get; set; }
    [JsonPropertyName("reply_id")] public string? ReplyId { get; set; }
    [JsonPropertyName("image_path")] public string? ImagePath { get; set; }
    [JsonPropertyName("game_file_path")] public string? GameFilePath { get; set; }
    [JsonPropertyName("reactor_ids")] public string[] ReactorIds { get; set; } = [];
}
