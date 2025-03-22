using System.Collections.Generic;

namespace FortnitePorting.Export.Models;

public class ExportCurveMapping
{
    public string Name;
    public List<ExportCurveExpressionElement> ExpressionStack = [];
}

public class ExportCurveExpressionElement(int elementType, object value)
{
    public int ElementType = elementType;
    public object Value = value;
}