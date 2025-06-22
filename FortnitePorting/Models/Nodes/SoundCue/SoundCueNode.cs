namespace FortnitePorting.Models.Nodes.SoundCue;

public class SoundCueNode(string expressionName = "", bool isExpressionName = true) : Node(expressionName, isExpressionName)
{
    protected override string ExpressionPrefix => "SoundNode";
}