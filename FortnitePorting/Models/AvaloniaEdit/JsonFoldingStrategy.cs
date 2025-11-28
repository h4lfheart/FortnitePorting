using System.Collections.Generic;
using AvaloniaEdit.Document;
using AvaloniaEdit.Folding;

namespace FortnitePorting.Models.AvaloniaEdit;

internal sealed class JsonFoldStart : NewFolding
{
    internal int StartLine;
    internal char BraceType;
}

public static class JsonFoldingStrategy
{
    public static void UpdateFoldings(FoldingManager manager, TextDocument document)
    {
        var foldings = CreateNewFoldings(document, out var firstErrorOffset);
        manager.UpdateFoldings(foldings, firstErrorOffset);
    }

    public static IEnumerable<NewFolding> CreateNewFoldings(TextDocument document, out int firstErrorOffset)
    {
        firstErrorOffset = -1;
        return CreateNewFoldings(document);
    }

    public static IEnumerable<NewFolding> CreateNewFoldings(ITextSource document)
    {
        var stack = new Stack<JsonFoldStart>();
        var foldMarkers = new List<NewFolding>();
        var inString = false;
        var escapeNext = false;

        for (var i = 0; i < document.TextLength; i++)
        {
            var c = document.GetCharAt(i);

            if (escapeNext)
            {
                escapeNext = false;
                continue;
            }

            if (c == '\\')
            {
                escapeNext = true;
                continue;
            }

            if (c == '"')
            {
                inString = !inString;
                continue;
            }

            if (inString)
                continue;

            if (c == '{' || c == '[')
            {
                var textDocument = document as TextDocument;
                if (textDocument != null)
                {
                    var location = textDocument.GetLocation(i);
                    var foldStart = new JsonFoldStart
                    {
                        StartLine = location.Line,
                        StartOffset = i,
                        BraceType = c
                    };
                    stack.Push(foldStart);
                }
            }
            else if (c == '}' || c == ']')
            {
                if (stack.Count > 0)
                {
                    var foldStart = stack.Peek();
                    char expectedOpening = c == '}' ? '{' : '[';

                    if (foldStart.BraceType == expectedOpening)
                    {
                        stack.Pop();
                        
                        var textDocument = document as TextDocument;
                        if (textDocument != null)
                        {
                            var location = textDocument.GetLocation(i);
                            if (location.Line > foldStart.StartLine)
                            {
                                foldStart.EndOffset = i + 1;
                                
                                foldStart.Name = c == '}' ? "{...}" : "[...]";

                                foldMarkers.Add(foldStart);
                            }
                        }
                    }
                }
            }
        }

        foldMarkers.Sort((a, b) => a.StartOffset.CompareTo(b.StartOffset));
        return foldMarkers;
    }
}