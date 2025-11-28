using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Media;
using AvaloniaEdit.Utils;
using CUE4Parse_Conversion.Textures;
using CUE4Parse.UE4.Assets.Exports;
using CUE4Parse.UE4.Assets.Exports.Material;
using CUE4Parse.UE4.Assets.Exports.Material.Editor;
using CUE4Parse.UE4.Assets.Exports.Texture;
using CUE4Parse.UE4.Assets.Objects;
using CUE4Parse.UE4.Objects.Core.Math;
using CUE4Parse.UE4.Objects.Core.Misc;
using CUE4Parse.UE4.Objects.Engine;
using CUE4Parse.UE4.Objects.UObject;
using DynamicData;
using FluentAvalonia.UI.Controls;
using FortnitePorting.Extensions;
using FortnitePorting.Models.Unreal.Material;
using Serilog;
using ColorSpectrumShape = Avalonia.Controls.ColorSpectrumShape;

namespace FortnitePorting.Models.Nodes.Material;

public class MaterialNodeTree : NodeTree
{
    protected override string[] IgnoredPropertyNames { get; set; } =
    [
        "MaterialExpressionEditorX",
        "MaterialExpressionEditorY",
        "Material",
        "Function",
        "Declaration",
        "InputExpressions",
        "OutputExpressions",
        "Input",
        "Output"
    ];

    protected override Type[] IgnoredPropertyTypes { get; set; } =
    [
        typeof(FExpressionInput),
        typeof(FGuid)
    ];

    private List<(MaterialNode, UMaterialExpression)> DeferredSubgraphs = [];
    
    private static Dictionary<string, Color> HeaderColorMappings = new()
    {
        ["Vector"] = Color.Parse("#877020"),
        ["Scalar"] = Color.Parse("#547a2f"),
        ["TextureCoordinate"] = Color.Parse("#8c1313"),
        ["Texture"] = Color.Parse("#136384"),
        ["Bool"] = Color.Parse("#561a1a"),
        ["Switch"] = Color.Parse("#561a1a"),
        ["MaterialFunction"] = Color.Parse("#466e86"),
        ["Composite"] = Color.Parse("#272827")
    };

    public override void Load(UObject obj)
    {
        base.Load(obj);
        
        if (obj is UMaterial material)
            LoadMaterial(material);
        
        if (obj is UMaterialFunction materialFunction)
            LoadMaterialFunction(materialFunction);
    }
    
    public void LoadMaterialFunction(UMaterialFunction materialFunction)
    {
        if (!materialFunction.TryLoadEditorData<UMaterialFunctionEditorOnlyData>(out var editorData) || editorData is null)
        {
            Info.Dialog("Material Preview", $"Failed to load {materialFunction.Name} because it has no editor-only data.");
            return;
        }
        
        var expressionCollection = editorData.GetOrDefault<FStructFallback>("ExpressionCollection");
        LoadExpressionCollection(expressionCollection);
    }

    public void LoadMaterial(UMaterial material)
    {
        if (!material.TryLoadEditorData<UMaterialEditorOnlyData>(out var editorData) || editorData is null)
        {
            Info.Dialog("Material Preview", $"Failed to load {material.Name} because it has no editor-only data.");
            return;
        }
        
        var expressionCollection = editorData.GetOrDefault<FStructFallback>("ExpressionCollection");
        LoadExpressionCollection(expressionCollection);

        var parentNode = new MaterialNode(material.Name, isEngineNode: false)
        {
            HeaderColor = Color.Parse("#786859")
        };
        
        NodeCache.AddOrUpdate(parentNode);

        if (editorData.Properties.FirstOrDefault(prop => prop.Name.Text.Equals("MaterialAttributes")) is
            { } materialAttributesProperty)
        {
            if (materialAttributesProperty.Tag?.GetValue<FExpressionInput>() is { } expressionInput)
            {
                AddInput(ref parentNode, expressionInput, materialAttributesProperty.Name.Text);
            }
        }
        else
        {
            foreach (var property in editorData.Properties)
            {
                if (property.Tag?.GetValue<FExpressionInput>() is not { } expressionInput) continue;
            
                AddInput(ref parentNode, expressionInput, property.Name.Text);
            }
        }
    }

    public void LoadExpressionCollection(FStructFallback expressionCollection)
    {
        var editorComments = expressionCollection.GetOrDefault<FPackageIndex[]>("EditorComments", []);
        foreach (var editorCommentLazy in editorComments)
        {
            var editorCommentExpression = editorCommentLazy.Load<UMaterialExpression>();
            if (editorCommentExpression is null) continue;
            
            var x = editorCommentExpression.GetOrDefault<int>("MaterialExpressionEditorX");
            var y = editorCommentExpression.GetOrDefault<int>("MaterialExpressionEditorY");
            var sizeX = editorCommentExpression.GetOrDefault<int>("SizeX");
            var sizeY = editorCommentExpression.GetOrDefault<int>("SizeY");
            var text = editorCommentExpression.GetOrDefault<string>("Text");
            var commentColor = editorCommentExpression.GetOrDefault<FLinearColor?>("CommentColor")?.ToFColor(false);
            
            NodeCache.AddOrUpdate(new NodeComment(editorCommentExpression.Name)
            {
                Label = text,
                Location = new Point(x, y),
                Size = new Size(sizeX, sizeY),
                CommentColor = commentColor is { } color ? new Color(color.A, color.R, color.G, color.B) : null,
                Properties = CollectProperties(editorCommentExpression)
            });
        }

        var expressions = expressionCollection.GetOrDefault<FPackageIndex[]>("Expressions", []);
        foreach (var expressionLazy in expressions)
        {
            var expression = expressionLazy.Load<UMaterialExpression>();
            if (expression is null) continue;

            if (NodeCache.Items.Any(node => node.ExpressionName.Equals(expression.Name, StringComparison.OrdinalIgnoreCase))) continue;

            AddNode(expression);
        }

        foreach (var (node, expression) in DeferredSubgraphs)
        {
            MaterialNode endPinNode = null!;
            
            var inputExpressionsLazy = expression.GetOrDefault<FPackageIndex?>("InputExpressions");
            if (inputExpressionsLazy?.Load<UMaterialExpression>() is { } inputExpressions)
            {
                var startPinNode = (MaterialNode) NodeCache.Items.FirstOrDefault(node =>
                    node.ExpressionName.Equals(inputExpressions.Name, StringComparison.OrdinalIgnoreCase))!;
                
                node.Inputs.Clear();
                
                var reroutePins = inputExpressions.GetOrDefault<FStructFallback[]>("ReroutePins", []);
                foreach (var reroutePin in reroutePins)
                {
                    if (reroutePin.Get<FPackageIndex?>("Expression")?.Load<UMaterialExpression>() is not { } targetExpression) continue;
                    
                    var pinName = reroutePin.GetOrDefault<FName?>("Name")?.Text ?? "Pin";
                    var pinInput = node.AddInput(pinName);

                    var targetRerouteNode = (MaterialNode?) NodeCache.Items.FirstOrDefault(node =>
                        node.ExpressionName.Equals(targetExpression.Name, StringComparison.OrdinalIgnoreCase));
                    if (targetRerouteNode is null) continue;

                    var existingInputs = Connections.Where(con => targetRerouteNode.Equals(con.To.Parent)).ToArray();
                    foreach (var existingInput in existingInputs)
                    {
                        Connections.Add(new NodeConnection(existingInput.From, pinInput));
                    }
                    
                    var existingOutputs = Connections.Where(con => targetRerouteNode.Equals(con.From.Parent)).ToArray();
                    foreach (var existingOutput in existingOutputs)
                    {
                        Connections.Add(new NodeConnection(startPinNode.GetOutput(pinName)!, existingOutput.To));
                    }
                    
                    Connections.RemoveMany(existingInputs);
                    Connections.RemoveMany(existingOutputs);
                    NodeCache.Remove(targetRerouteNode);

                }
            }
            
            var outputExpressionsLazy = expression.GetOrDefault<FPackageIndex?>("OutputExpressions");
            if (outputExpressionsLazy?.Load<UMaterialExpression>() is { } outputExpressions)
            {
                endPinNode = (MaterialNode) NodeCache.Items.FirstOrDefault(node =>
                    node.ExpressionName.Equals(outputExpressions.Name, StringComparison.OrdinalIgnoreCase))!;
                
                node.Outputs.Clear();
                
                var reroutePins = outputExpressions.GetOrDefault<FStructFallback[]>("ReroutePins", []);
                foreach (var reroutePin in reroutePins)
                {
                    if (reroutePin.Get<FPackageIndex?>("Expression")?.Load<UMaterialExpression>() is not { } targetExpression) continue;
                    
                    var pinName = reroutePin.GetOrDefault<FName?>("Name")?.Text ?? "Pin";
                    var pinOutput = node.AddOutput(pinName);
                    
                    var targetRerouteNode = (MaterialNode?) NodeCache.Items.FirstOrDefault(node => node.ExpressionName.Equals(targetExpression.Name, StringComparison.OrdinalIgnoreCase));
                    if (targetRerouteNode is null) continue;
                    
                    var existingInputs = Connections.Where(con => targetRerouteNode.Equals(con.To.Parent)).ToArray();
                    foreach (var existingInput in existingInputs)
                    {
                        Connections.Add(new NodeConnection(existingInput.From, endPinNode.GetInput(pinName)!));
                    }
                    
                    var existingOutputs = Connections.Where(con => targetRerouteNode.Equals(con.From.Parent)).ToArray();
                    foreach (var existingOutput in existingOutputs)
                    {
                        Connections.Add(new NodeConnection(pinOutput, existingOutput.To));
                    }
                    
                    Connections.RemoveMany(existingInputs);
                    Connections.RemoveMany(existingOutputs);
                    NodeCache.Remove(targetRerouteNode);
                }
            }

            var subgraphNodes = new HashSet<BaseNode>();
            var subgraphConnections = new HashSet<NodeConnection>();

            CollectSubgraphNodes(endPinNode);

            node.Subgraph = new MaterialNodeTree
            {
                TreeName = node.Label,
                Connections = [..subgraphConnections],
            };
            
            node.Subgraph.NodeCache.AddOrUpdate(subgraphNodes);
            
            NodeCache.RemoveKeys(subgraphNodes.Select(node => node.ExpressionName));
            Connections.RemoveMany(subgraphConnections);
            continue;

            void CollectSubgraphNodes(BaseNode current)
            {
                if (current == null)
                {
                    Log.Debug("Null BaseNode");
                    return;
                }
                subgraphNodes.Add(current);
                
                var childConnections = Connections
                    .Where(con => con.To.Parent.Equals(current))
                    .ToArray();
                
                if (childConnections.Length == 0) return;
                
                subgraphConnections.AddRange(childConnections);
                
                var childNodes = childConnections
                    .Select(con => con.From.Parent)
                    .ToArray();

                subgraphNodes.AddRange(childNodes);
                foreach (var childNode in childNodes)
                {
                    CollectSubgraphNodes(childNode);
                }
                
            }
        }
    }
    
     private MaterialNode AddNode(UMaterialExpression expression)
    {
        var x = expression.GetOrDefault<int>("MaterialExpressionEditorX");
        var y = expression.GetOrDefault<int>("MaterialExpressionEditorY");

        var node = new MaterialNode(expression.Name)
        {
            Location = new Point(x, y),
            Properties = CollectProperties(expression)
        };
        
        NodeCache.AddOrUpdate(node);
        SetupNodeContent(ref node, expression);

        foreach (var property in expression.Properties)
        {
            if (property.Tag?.GetValue<FExpressionInput>() is not { } expressionInput) continue;
            
            var name = property.Name.Text;
            AddInput(ref node, expressionInput, nameOverride: name.Equals("Input") ? string.Empty : name);
        }

        return node;
    }

    private void AddInput(ref MaterialNode node, FExpressionInput expressionInput, string? nameOverride = null)
    {
        if (expressionInput.Expression?.Load<UMaterialExpression>() is not { } subExpression) return;
        
        var targetNode = (MaterialNode?) NodeCache.Items.FirstOrDefault(node => node.ExpressionName.Equals(subExpression.Name, StringComparison.OrdinalIgnoreCase)) ?? AddNode(subExpression);

        var inputSocket = node.AddInput(new NodeSocket(nameOverride ?? expressionInput.InputName.Text));

        if (expressionInput.OutputIndex >= targetNode.Outputs.Count)
        {
            Log.Warning("Expression {expressionName} has no output index {outputIndex}", targetNode.ExpressionName, expressionInput.OutputIndex);

            if (targetNode.Outputs.Count == 0)
            {
                targetNode.AddOutput("Output");
            }
        }
        
        Connections.Add(new NodeConnection(targetNode.Outputs[Math.Min(expressionInput.OutputIndex, targetNode.Outputs.Count - 1)], inputSocket));
    }

    private void SetupNodeContent(ref MaterialNode node, UMaterialExpression expression)
    {
        switch (expression.ExportType)
        {
            case "MaterialExpressionMaterialFunctionCall":
            {
                var materialFunction = expression.Get<FPackageIndex>("MaterialFunction");
                node.Package = materialFunction;
                node.Label = materialFunction.ResolvedObject?.Name.Text ?? "Material Function";
                
                node.Inputs.Clear();
                var inputs = expression.GetOrDefault<FStructFallback[]>("FunctionInputs", []);
                foreach (var functionInput in inputs)
                {
                    var expressionInput = functionInput.Get<FExpressionInput>("Input");
                    AddInput(ref node, expressionInput);
                }
                
                node.Outputs.Clear();
                var outputs = expression.GetOrDefault<FStructFallback[]>("FunctionOutputs", []);
                foreach (var functionOutput in outputs)
                {
                    var output = functionOutput.Get<FStructFallback>("Output");
                    var outputName = output.Get<FName>("OutputName");

                    node.AddOutput(outputName.Text);
                }
                
                break;
            }
            case "MaterialExpressionComposite":
            {
                var name = expression.GetOrDefault<string?>("SubgraphName") ?? "Subgraph";
                node.Label = name;
                
                DeferredSubgraphs.Add((node, expression));
                break;
            }
            case "MaterialExpressionPinBase":
            {
                var pinDirection = expression.GetOrDefault<FName?>("PinDirection")?.Text;
                if (pinDirection == "EEdGraphPinDirection::EGPD_Output")
                {
                    node.Label = "Input";
                    
                    var outputs = expression.GetOrDefault<FStructFallback[]>("Outputs", []);
                    foreach (var output in outputs)
                    {
                        var outputName = output.Get<FName>("OutputName");
                        node.AddOutput(outputName.Text);
                    }
                }
                else
                {
                    node.Label = "Output";
                    
                    var reroutePins = expression.GetOrDefault<FStructFallback[]>("ReroutePins", []);
                    foreach (var reroutePin in reroutePins)
                    {
                        var pinName = reroutePin.GetOrDefault<FName?>("Name")?.Text ?? "Pin";
                        node.AddInput(pinName);
                    }
                }
                
                break;
            }
            case "MaterialExpressionFunctionInput":
            {
                var name = expression.GetOrDefault<FName?>("InputName")?.Text ?? "Input";
                node.Label = name;
                node.HeaderColor = new Color(255, 255 / 2, 0, 0);
                break;
            }
            case "MaterialExpressionFunctionOutput":
            {
                var name = expression.GetOrDefault<FName?>("OutputName")?.Text ?? "Output";
                node.Label = name;
                node.HeaderColor = new Color(255, 255 / 2, 0, 0);
                break;
            }

            case "MaterialExpressionDynamicParameter":
            {
                var outputs = expression.GetOrDefault<FStructFallback[]>("Outputs", []);
                node.Outputs.Clear();
                foreach (var output in outputs)
                {
                    var outputName = output.Get<FName>("OutputName");

                    node.AddOutput(outputName.Text);
                }
                break;
            }
            
            case "MaterialExpressionSetMaterialAttributes":
            {
                var expressionInputs = expression.GetOrDefault<FExpressionInput[]>("Inputs", []);
                foreach (var expressionInput in expressionInputs)
                {
                    var overrideName = expressionInput.InputName.IsNone ? "MaterialAttributes" : null;
                    AddInput(ref node, expressionInput, overrideName);
                }
                
                break;
            }
            
            case "MaterialExpressionNamedRerouteDeclaration":
            case "MaterialExpressionNamedRerouteUsage":
            {
                var declaration = expression.ExportType switch
                {
                    "MaterialExpressionNamedRerouteUsage" => expression.GetOrDefault("Declaration", expression),
                    _ => expression
                };
                
                var name = declaration.GetOrDefault("Name", new FName("Invalid Reroute")).Text;
                var nodeColor = declaration.GetOrDefault<FLinearColor>("NodeColor");
                nodeColor.R *= 0.25f;
                nodeColor.G *= 0.25f;
                nodeColor.B *= 0.25f;
                
                var normalizedColor = nodeColor.ToFColor(true);
                
                node.Label = name;
                node.HeaderColor = new Color(normalizedColor.A, normalizedColor.R, normalizedColor.G, normalizedColor.B);

              
                if (expression.ExportType == "MaterialExpressionNamedRerouteUsage")
                {
                    var declarationNode = (MaterialNode?) NodeCache.Items.FirstOrDefault(node => node.ExpressionName.Equals(declaration.Name)) 
                                          ?? AddNode(declaration);
                    node.LinkedNode = declarationNode;
                }
                break;
            }
            
            case "MaterialExpressionTextureSampleParameter2D":
            case "MaterialExpressionTextureSample":
            case "MaterialExpressionTextureObjectParameter":
            {
                AddColorInputs(ref node, includeRGBA: true);
                
                node.Label = expression.GetOrDefault<FName?>("ParameterName")?.Text ?? "Texture";
                
                if (expression.GetOrDefault<UTexture>("Texture") is not { } texture) break;
                if (texture.Decode()?.ToWriteableBitmap() is not { } bitmap) break;

                if (node.Label.Equals("Texture"))
                    node.Label = texture.Name;

                node.Content = new Border
                {
                    CornerRadius = new CornerRadius(4),
                    ClipToBounds = true,
                    Margin = SpaceExtension.Space(1),
                    Child = new Image
                    {
                        Width = 128,
                        Height = 128,
                        Source = bitmap,
                    }
                };
                
                break;
            }
            case "MaterialExpressionRuntimeVirtualTextureSample":
            {
                node.AddOutput("BaseColor");
                node.AddOutput("Specular");
                node.AddOutput("Roughness");
                node.AddOutput("Normal");
                node.AddOutput("WorldHeight");
                node.AddOutput("Mask");
                break;
            }
            
            case "MaterialExpressionConstant2Vector":
            case "MaterialExpressionConstant3Vector":
            case "MaterialExpressionConstant4Vector":
            case "MaterialExpressionVectorParameter":
            {
                var constantColor = expression.GetOrDefault<FLinearColor>(expression.ExportType switch
                {
                    "MaterialExpressionVectorParameter" => "DefaultValue",
                    _ => "Constant"
                });

                switch (expression.ExportType)
                {
                    case "MaterialExpressionConstant3Vector":
                        constantColor.A = 1;
                        break;
                    case "MaterialExpressionConstant2Vector":
                        constantColor.B = 0;
                        constantColor.A = 1;
                        break;
                    case "MaterialExpressionVectorParameter":
                        node.Label = expression.GetOrDefault<FName?>("ParameterName")?.Text ?? expression.Name;
                        break;
                }
                
                
                AddColorInputs(ref node);
            
                var normalizedColor = constantColor.ToFColor(false);    
                node.Content = new ColorPicker
                {
                    Color = new Color(normalizedColor.A, normalizedColor.R, normalizedColor.G, normalizedColor.B),
                    ColorSpectrumShape = ColorSpectrumShape.Ring,
                    IsColorPaletteVisible = false,
                    IsAlphaEnabled = true,
                    IsAlphaVisible = true,
                    Margin = SpaceExtension.Space(1),
                    Width = 96,
                    Height = 64,
                };
                
                break;
            }
            case "MaterialExpressionParticleColor":
            {
                AddColorInputs(ref node);
                break;
            }
            case "MaterialExpressionComponentMask":
            {
                var r = expression.GetOrDefault<bool>("R");
                var g = expression.GetOrDefault<bool>("G");
                var b = expression.GetOrDefault<bool>("B");
                var a = expression.GetOrDefault<bool>("A");

                var enabledComponents = new List<string>();
                if (r) enabledComponents.Add("R");
                if (g) enabledComponents.Add("G");
                if (b) enabledComponents.Add("B");
                if (a) enabledComponents.Add("A");

                node.Label = $"Mask ( {string.Join(' ', enabledComponents)} )";
                break;
            }
            case "MaterialExpressionCurveAtlasRowParameter":
            {
                AddColorInputs(ref node);
                
                break;
            }
            case "MaterialExpressionVertexColor":
            {
                AddColorInputs(ref node);
                
                break;
            }
            
            case "MaterialExpressionScalarParameter":
            {
                var name = expression.GetOrDefault<FName?>("ParameterName")?.Text ?? expression.Name;
                var sliderMin = expression.GetOrDefault<float>("SliderMin", 0);
                var sliderMax = expression.GetOrDefault<float>("SliderMax", 1);
                var defaultValue = expression.GetOrDefault<float>("DefaultValue");
                
                node.Label = name;
                node.Content = new NumberBox
                {
                    Value = defaultValue,
                    Minimum = Math.Min(sliderMin, defaultValue),
                    Maximum = Math.Max(sliderMax, defaultValue),
                    Margin = SpaceExtension.Space(1)
                };
                break;
            }
            case "MaterialExpressionConstant":
            {
                var value = expression.GetOrDefault<float>("R");
                node.Content = new NumberBox
                {
                    Value = value,
                    Margin = SpaceExtension.Space(1)
                };
                
                break;
            }
            
            case "MaterialExpressionGetMaterialAttributes":
            {
                var outputs = expression.GetOrDefault<FStructFallback[]>("Outputs", []);
                node.Outputs.Clear();
                foreach (var output in outputs)
                {
                    var outputName = output.Get<FName>("OutputName");

                    node.AddOutput(outputName.Text);
                }
                break;
            }

            case "MaterialExpressionCustom":
            {
                var inputs = expression.GetOrDefault<FStructFallback[]>("Inputs", []);
                foreach (var input in inputs)
                {
                    var overrideName = input.GetOrDefault<FName?>("InputName")?.Text ?? "Input";
                    AddInput(ref node, input.Get<FExpressionInput>("Input"), overrideName);
                }

                node.Content = new TextBox
                {
                    Text = expression.GetOrDefault<string>("Code"),
                    Margin = SpaceExtension.Space(1)
                };
                break;
            }

            case "MaterialExpressionStaticSwitchParameter":
            {
                node.Label = expression.GetOrDefault<FName?>("ParameterName")?.Text ?? expression.Name;
                break;
            }
            case "MaterialExpressionStaticBoolParameter":
            {
                node.Label = expression.GetOrDefault<FName?>("ParameterName")?.Text ?? expression.Name;
                node.Content = new ToggleSwitch
                {
                    IsChecked = expression.GetOrDefault<bool>("DefaultValue")
                };
                break;
            }
            case "MaterialExpressionStaticComponentMaskParameter":
            {
                node.Label = expression.GetOrDefault<FName?>("ParameterName")?.Text ?? expression.Name;
                node.Content = new StackPanel
                {
                    Children =
                    {
                        new CheckBox { Content = "R", IsChecked = expression.GetOrDefault<bool>("DefaultR") },
                        new CheckBox { Content = "G", IsChecked = expression.GetOrDefault<bool>("DefaultG") },
                        new CheckBox { Content = "B", IsChecked = expression.GetOrDefault<bool>("DefaultB") },
                        new CheckBox { Content = "A", IsChecked = expression.GetOrDefault<bool>("DefaultA") },
                    }
                };
                break;
            }
            case "MaterialExpressionChannelMaskParameter":
            {
                node.Label = expression.GetOrDefault<FName?>("ParameterName")?.Text ?? expression.Name;
                
                break;
            }
            case "MaterialExpressionSwitch":
            {
                var inputs = expression.GetOrDefault<FStructFallback[]>("Inputs", []);
                for (var i = 0; i < inputs.Length; i++)
                {
                    var input = inputs[i];
                    var overrideName = input.GetOrDefault<FName?>("InputName")?.Text ?? $"{i}";
                    if ("None".Equals(overrideName, StringComparison.OrdinalIgnoreCase)) overrideName = $"{i}";
                    AddInput(ref node, input.Get<FExpressionInput>("Input"), overrideName);
                }
                break;
            }
            case "MaterialExpressionConvert":
            {
                node.Label = expression.GetOrDefault("NodeName", expression.Name);
                var inputs = expression.GetOrDefault<FStructFallback[]>("ConvertInputs", []);
                foreach (var input in inputs)
                {
                    var inputName = input.GetOrDefault<FName?>("Name", new FName(""))?.Text;
                    AddInput(ref node, input.Get<FExpressionInput>("ExpressionInput"), inputName);
                }
                var outputs = expression.GetOrDefault<FStructFallback[]>("Outputs", []);
                node.Outputs.Clear();
                for (var i = 0; i < outputs.Length; i++)
                {
                    var output = outputs[i];
                    var outputName = output.Get<FName?>("OutputName")?.Text ?? $"{i}";
                    if ("None".Equals(outputName, StringComparison.OrdinalIgnoreCase)) outputName = $"{i}";
                    node.AddOutput(outputName);
                }
                break;
            }
        }

        if (node.HeaderColor is null)
        {
            if (HeaderColorMappings.FirstOrDefault(kvp => expression.ExportType.Contains(kvp.Key, StringComparison.OrdinalIgnoreCase)) is { Key: not null } colorMapping)
            {
                node.HeaderColor = colorMapping.Value;
            }
            else
            {
                node.HeaderColor = Color.Parse("#60815c");
            }
        }

        if (expression.GetOrDefault<string?>("Desc") is { } description)
        {
            node.FooterContent = description;
        }
        
        if (node.Outputs.Count == 0)
        {
            node.AddOutput(string.Empty);
        }
    }

    private void AddColorInputs(ref MaterialNode node, bool includeRGBA = false)
    {
        node.AddOutput(new NodeSocket("RGB"));
        node.Outputs.Add(new NodeSocket("R")
        {
            SocketColor = Colors.Red
        });
        node.AddOutput(new NodeSocket("G")
        {
            SocketColor = Colors.Green
        });
        node.AddOutput(new NodeSocket("B")
        {
            SocketColor = Colors.Blue
        });
        node.AddOutput(new NodeSocket("A")
        {
            SocketColor = Colors.DarkGray
        });
        if (includeRGBA)
            node.AddOutput(new NodeSocket("RGBA"));
    }
}