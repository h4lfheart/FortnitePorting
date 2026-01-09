using System.Text.RegularExpressions;
using AvaloniaEdit.Rendering;

namespace FortnitePorting.Models.AvaloniaEdit;

public class FilePathElementGenerator : VisualLineElementGenerator
{
    private readonly Regex _gamePathRegex =
        new("\"(?:ObjectPath|AssetPathName|AssetName|ParameterName|CollisionProfileName|TableId)\": \"(?'target'(?!/?Script/)(.*/.*))\",?$",
            RegexOptions.Compiled | RegexOptions.Singleline | RegexOptions.IgnoreCase | RegexOptions.CultureInvariant);


    private Match FindMatch(int startOffset)
    {
        var endOffset = CurrentContext.VisualLine.LastDocumentLine.EndOffset;
        var relevantText = CurrentContext.Document.GetText(startOffset, endOffset - startOffset);
        return _gamePathRegex.Match(relevantText);
    }

    public override int GetFirstInterestedOffset(int startOffset)
    {
        var m = FindMatch(startOffset);
        return m.Success ? startOffset + m.Index : -1;
    }

    public override VisualLineElement ConstructElement(int offset)
    {
        var m = FindMatch(offset);
        if (!m.Success || m.Index != 0 ||
            !m.Groups.TryGetValue("target", out var g)) return null;

        return new FilePathVisualLineText(g.Value, CurrentContext.VisualLine, g.Length + g.Index + 1);
    }
}