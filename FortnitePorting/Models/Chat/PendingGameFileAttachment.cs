using Avalonia.Media.Imaging;

namespace FortnitePorting.Models.Chat;

public record PendingGameFileAttachment(string Path, Bitmap Icon, string? DisplayName);
