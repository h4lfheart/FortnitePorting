namespace FortnitePorting.Models.Nodes.Material;

public class MaterialNode(string expressionName, bool isEngineNode = true) : Node(expressionName, isEngineNode)
{
    protected override string ExpressionPrefix => "MaterialExpression";
}