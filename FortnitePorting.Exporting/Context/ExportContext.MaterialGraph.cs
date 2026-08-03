using System.Collections.Generic;
using System.Linq;
using CUE4Parse.UE4.Assets.Exports.Material;
using CUE4Parse.UE4.Assets.Exports.Material.Editor;
using CUE4Parse.UE4.Assets.Exports.Texture;
using CUE4Parse.UE4.Assets.Objects;
using CUE4Parse.UE4.Assets.Objects.Properties;
using CUE4Parse.UE4.Objects.Core.Math;
using CUE4Parse.UE4.Objects.Engine;
using CUE4Parse.UE4.Objects.UObject;
using CUE4Parse.Utils;
using FortnitePorting.CUE4Parse.Extensions;
using FortnitePorting.Exporting.Models;

namespace FortnitePorting.Exporting.Context;

// Generic, structural export of a material's Unreal expression graph, as a fallback/alternative
// to the curated ParameterCollection template system for materials with no matching template.
// Ported from FortnitePorting/Models/Nodes/Material/MaterialNodeTree.cs (in-app material preview
// viewer), which already walks this graph generically via CUE4Parse's UObject/FProperty reflection.
public partial class ExportContext
{
    private Dictionary<string, MaterialGraphNode> GraphNodeCache = [];

    public MaterialGraph? MaterialGraph(UMaterial? material)
    {
        if (material is null) return null;
        if (!material.TryLoadEditorData<UMaterialEditorOnlyData>(out var editorData) || editorData is null) return null;

        GraphNodeCache = [];
        var graph = new MaterialGraph();

        var expressionCollection = editorData.GetOrDefault<FStructFallback>("ExpressionCollection");
        if (expressionCollection is null) return graph;

        var expressions = expressionCollection.GetOrDefault<FPackageIndex[]>("Expressions", []);
        foreach (var expressionLazy in expressions)
        {
            if (expressionLazy.Load<UMaterialExpression>() is not { } expression) continue;
            if (GraphNodeCache.ContainsKey(expression.Name)) continue;

            AddGraphNode(expression, graph);
        }

        if (editorData.Properties.FirstOrDefault(prop => prop.Name.Text.Equals("MaterialAttributes")) is { } materialAttributesProperty)
        {
            AddGraphOutput(graph, "MaterialAttributes", materialAttributesProperty);
        }
        else
        {
            foreach (var property in editorData.Properties)
            {
                if (property.Name.Text.Equals("ExpressionCollection")) continue;
                AddGraphOutput(graph, property.Name.Text, property);
            }
        }

        return graph;
    }

    private void AddGraphOutput(MaterialGraph graph, string name, FPropertyTag property)
    {
        // Top-level material inputs (BaseColor, Metallic, etc.) are UStructs (FColorMaterialInput,
        // FScalarMaterialInput, ...) rather than plain FExpressionInput, so read them generically
        // as a struct fallback instead of MaterialNodeTree.cs's GetValue<FExpressionInput>() (which
        // would silently drop the UseConstant/Constant fallback fields these structs also carry).
        if (property.Tag is not StructProperty) return;
        if (property.Tag.GetValue<FStructFallback>() is not { } structFallback) return;

        MaterialGraphNode? sourceNode = null;
        if (structFallback.GetOrDefault<FPackageIndex?>("Expression") is { } expressionIndex
            && expressionIndex.Load<UMaterialExpression>() is { } sourceExpression)
        {
            sourceNode = GetOrAddGraphNode(sourceExpression, graph);
        }

        var useConstant = structFallback.GetOrDefault("UseConstant", false);
        if (sourceNode is null && !useConstant) return;

        graph.Outputs.Add(new MaterialGraphOutput
        {
            PropertyName = name,
            SourceNodeId = sourceNode?.Id,
            SourceOutputIndex = structFallback.GetOrDefault("OutputIndex", 0),
            DefaultValue = useConstant ? ReadConstantValue(structFallback, "Constant") : null
        });
    }

    private MaterialGraphNode GetOrAddGraphNode(UMaterialExpression expression, MaterialGraph graph)
    {
        return GraphNodeCache.TryGetValue(expression.Name, out var existing) ? existing : AddGraphNode(expression, graph);
    }

    private MaterialGraphNode AddGraphNode(UMaterialExpression expression, MaterialGraph graph)
    {
        var node = new MaterialGraphNode
        {
            Id = expression.Name,
            Type = expression.ExportType.SubstringAfter("MaterialExpression")
        };

        GraphNodeCache[node.Id] = node;
        graph.Nodes.Add(node);

        SetupGraphNodeContent(node, expression, graph);

        foreach (var property in expression.Properties)
        {
            if (property.Tag is not StructProperty) continue;
            if (property.Tag.GetValue<FExpressionInput>() is not { } expressionInput) continue;

            var name = property.Name.Text;
            AddGraphInput(node, graph, expression, expressionInput, name.Equals("Input") ? string.Empty : name);
        }

        if (node.Outputs.Count == 0) node.Outputs.Add(string.Empty);

        return node;
    }

    private void AddGraphInput(MaterialGraphNode node, MaterialGraph graph, UMaterialExpression expression, FExpressionInput expressionInput, string name)
    {
        var input = new MaterialGraphInput
        {
            Name = name,
            Mask = expressionInput.Mask,
            MaskR = expressionInput.MaskR,
            MaskG = expressionInput.MaskG,
            MaskB = expressionInput.MaskB,
            MaskA = expressionInput.MaskA
        };

        if (expressionInput.Expression?.Load<UMaterialExpression>() is { } sourceExpression)
        {
            var sourceNode = GetOrAddGraphNode(sourceExpression, graph);
            input.SourceNodeId = sourceNode.Id;
            input.SourceOutputIndex = expressionInput.OutputIndex;
        }
        else if (!string.IsNullOrEmpty(name))
        {
            input.DefaultValue = ReadInputConstantFallback(expression, name);
        }

        node.Inputs.Add(input);
    }

    // Many math expressions (Add, Multiply, Lerp, ...) expose a "ConstX" (or "XDefault") sibling
    // scalar/color property used by Unreal when input pin X is left unconnected. Best-effort lookup
    // across the naming conventions observed in Unreal's material expression classes.
    private static object? ReadInputConstantFallback(UMaterialExpression expression, string inputName)
    {
        foreach (var candidate in new[] { $"Const{inputName}", $"{inputName}Default", $"Default{inputName}" })
        {
            if (!expression.Properties.Any(p => p.Name.Text.Equals(candidate))) continue;

            if (expression.TryGetValue(out float floatValue, candidate)) return floatValue;
            if (expression.TryGetValue(out FLinearColor colorValue, candidate)) return ToColorArray(colorValue);
        }

        return null;
    }

    private static object? ReadConstantValue(FStructFallback structFallback, string name)
    {
        if (structFallback.TryGetValue(out FLinearColor colorConstant, name)) return ToColorArray(colorConstant);
        if (structFallback.TryGetValue(out float floatConstant, name)) return floatConstant;

        return null;
    }

    private static float[] ToColorArray(FLinearColor color) => [color.R, color.G, color.B, color.A];

    private static void AddColorOutputs(MaterialGraphNode node, bool includeRGBA = false)
    {
        node.Outputs.AddRange(["RGB", "R", "G", "B", "A"]);
        if (includeRGBA) node.Outputs.Add("RGBA");
    }

    private void SetupGraphNodeContent(MaterialGraphNode node, UMaterialExpression expression, MaterialGraph graph)
    {
        switch (expression.ExportType)
        {
            case "MaterialExpressionTextureSampleParameter2D":
            case "MaterialExpressionTextureSample":
            case "MaterialExpressionTextureObjectParameter":
            {
                AddColorOutputs(node, includeRGBA: true);

                if (expression.GetOrDefault<FName?>("ParameterName") is { IsNone: false } parameterName)
                    node.Properties["ParameterName"] = parameterName.Text;

                if (expression.GetOrDefault<UTexture>("Texture") is { } texture)
                    node.Properties["Texture"] = new ExportTexture(Export(texture), texture.SRGB, texture.CompressionSettings);

                break;
            }
            case "MaterialExpressionConstant":
            {
                node.Properties["R"] = expression.GetOrDefault<float>("R");
                break;
            }
            case "MaterialExpressionConstant2Vector":
            {
                AddColorOutputs(node);
                node.Properties["R"] = expression.GetOrDefault<float>("R");
                node.Properties["G"] = expression.GetOrDefault<float>("G");
                break;
            }
            case "MaterialExpressionConstant3Vector":
            case "MaterialExpressionConstant4Vector":
            case "MaterialExpressionVectorParameter":
            {
                AddColorOutputs(node);

                var constantColor = expression.GetOrDefault<FLinearColor>(expression.ExportType == "MaterialExpressionVectorParameter" ? "DefaultValue" : "Constant");
                if (expression.ExportType == "MaterialExpressionConstant3Vector") constantColor.A = 1;

                node.Properties["R"] = constantColor.R;
                node.Properties["G"] = constantColor.G;
                node.Properties["B"] = constantColor.B;
                node.Properties["A"] = constantColor.A;

                if (expression.ExportType == "MaterialExpressionVectorParameter" && expression.GetOrDefault<FName?>("ParameterName") is { IsNone: false } vectorParamName)
                    node.Properties["ParameterName"] = vectorParamName.Text;

                break;
            }
            case "MaterialExpressionParticleColor":
            case "MaterialExpressionVertexColor":
            {
                AddColorOutputs(node);
                break;
            }
            case "MaterialExpressionScalarParameter":
            {
                node.Properties["DefaultValue"] = expression.GetOrDefault<float>("DefaultValue");
                if (expression.GetOrDefault<FName?>("ParameterName") is { IsNone: false } scalarParamName)
                    node.Properties["ParameterName"] = scalarParamName.Text;
                break;
            }
            case "MaterialExpressionStaticBoolParameter":
            {
                node.Properties["DefaultValue"] = expression.GetOrDefault<bool>("DefaultValue");
                if (expression.GetOrDefault<FName?>("ParameterName") is { IsNone: false } boolParamName)
                    node.Properties["ParameterName"] = boolParamName.Text;
                break;
            }
            case "MaterialExpressionComponentMask":
            {
                node.Properties["R"] = expression.GetOrDefault<bool>("R");
                node.Properties["G"] = expression.GetOrDefault<bool>("G");
                node.Properties["B"] = expression.GetOrDefault<bool>("B");
                node.Properties["A"] = expression.GetOrDefault<bool>("A");
                break;
            }
            case "MaterialExpressionTextureCoordinate":
            {
                node.Properties["CoordinateIndex"] = expression.GetOrDefault<int>("CoordinateIndex");
                node.Properties["UTiling"] = expression.GetOrDefault<float>("UTiling", 1f);
                node.Properties["VTiling"] = expression.GetOrDefault<float>("VTiling", 1f);
                break;
            }
            case "MaterialExpressionPanner":
            {
                node.Properties["SpeedX"] = expression.GetOrDefault<float>("SpeedX");
                node.Properties["SpeedY"] = expression.GetOrDefault<float>("SpeedY");
                break;
            }
            case "MaterialExpressionFresnel":
            {
                node.Properties["Exponent"] = expression.GetOrDefault<float>("Exponent", 5f);
                node.Properties["BaseReflectFraction"] = expression.GetOrDefault<float>("BaseReflectFraction", 0.04f);
                break;
            }
            case "MaterialExpressionPower":
            {
                node.Properties["ConstExponent"] = expression.GetOrDefault<float>("ConstExponent", 2f);
                break;
            }
            case "MaterialExpressionNamedRerouteDeclaration":
            {
                node.Type = "Reroute";
                break;
            }
            case "MaterialExpressionNamedRerouteUsage":
            {
                node.Type = "Reroute";

                if (expression.GetOrDefault<FPackageIndex?>("Declaration") is { } declarationIndex
                    && declarationIndex.Load<UMaterialExpression>() is { } declarationExpression)
                {
                    var declarationNode = GetOrAddGraphNode(declarationExpression, graph);
                    node.Inputs.Add(new MaterialGraphInput
                    {
                        Name = string.Empty,
                        SourceNodeId = declarationNode.Id,
                        SourceOutputIndex = 0
                    });
                }

                break;
            }
        }
    }
}
