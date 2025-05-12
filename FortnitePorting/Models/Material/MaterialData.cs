using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Linq;
using System.Reactive.Linq;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Media;
using AvaloniaEdit.Utils;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
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
using DynamicData.Binding;
using FluentAvalonia.UI.Controls;
using FortnitePorting.Extensions;
using FortnitePorting.Models.Files;
using FortnitePorting.Models.Unreal.Material;
using FortnitePorting.Shared.Extensions;
using FortnitePorting.WindowModels;
using ReactiveUI;
using Serilog;
using ColorSpectrumShape = Avalonia.Controls.ColorSpectrumShape;

namespace FortnitePorting.Models.Material;

public partial class MaterialData : ObservableObject
{
    [ObservableProperty] private string _dataName;
    [ObservableProperty] private UObject? _asset;
    [ObservableProperty] private ReadOnlyObservableCollection<MaterialNodeBase> _nodes = new([]);
    [ObservableProperty] private ObservableCollection<MaterialNodeConnection> _connections = [];
    [ObservableProperty] private MaterialNodeBase? _selectedNode;
    [ObservableProperty] private string _searchFilter = string.Empty;


    public SourceCache<MaterialNodeBase, string> NodeCache { get; set; } = new(item => item.ExpressionName);

    public List<(MaterialNode, UMaterialExpression)> DeferredSubgraphs = [];

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

    private static Type[] IgnoredPropertyTypes =
    [
        typeof(FScriptStruct), typeof(FStructFallback), typeof(FGuid), typeof(UScriptArray)
    ];
    
    private static string[] IgnoredPropertyNames =
    [
        "MaterialExpressionEditorX",
        "MaterialExpressionEditorY",
        "Material",
        "Function",
        "Declaration",
        "InputExpressions",
        "OutputExpressions",
    ];

    public MaterialData()
    {
        var assetFilter = this
            .WhenAnyValue(viewModel => viewModel.SearchFilter)
            .Select(CreateAssetFilter);
        
        NodeCache.Connect()
            .ObserveOn(RxApp.TaskpoolScheduler)
            .Filter(assetFilter)
            .Sort(SortExpressionComparer<MaterialNodeBase>.Ascending(x => x.ExpressionName))
            .Bind(out var flatCollection)
            .Subscribe();

        Nodes = flatCollection;
    }
    
    private Func<MaterialNodeBase, bool> CreateAssetFilter(string searchFilter)
    {
        return asset => MiscExtensions.Filter(asset.Label, searchFilter) || MiscExtensions.Filter(asset.ExpressionName, searchFilter);
    }
    
    [RelayCommand]
    public async Task Refresh()
    {
        NodeCache.Clear();
        Connections.Clear();
        DeferredSubgraphs.Clear();
        
        if (Asset is UMaterial material)
            LoadMaterial(material);
        if (Asset is UMaterialFunction materialFunction)
            LoadMaterialFunction(materialFunction);
    }

    public void Load(UObject obj)
    {
        Asset = obj;
        DataName = Asset.Name;
        
        if (Asset is UMaterial material)
            LoadMaterial(material);
        
        if (Asset is UMaterialFunction materialFunction)
            LoadMaterialFunction(materialFunction);
    }
    
    public void LoadMaterialFunction(UMaterialFunction materialFunction)
    {
        if (!materialFunction.TryLoadEditorData<UMaterialFunctionEditorOnlyData>(out var editorData) || editorData is null)
        {
            AppWM.Dialog("Material Preview", $"Failed to load {materialFunction.Name} because it has no editor-only data.");
            return;
        }
        
        var expressionCollection = editorData.GetOrDefault<FStructFallback>("ExpressionCollection");
        LoadExpressionCollection(expressionCollection);
    }

    public void LoadMaterial(UMaterial material)
    {
        if (!material.TryLoadEditorData<UMaterialEditorOnlyData>(out var editorData) || editorData is null)
        {
            AppWM.Dialog("Material Preview", $"Failed to load {material.Name} because it has no editor-only data.");
            return;
        }
        
        var expressionCollection = editorData.GetOrDefault<FStructFallback>("ExpressionCollection");
        LoadExpressionCollection(expressionCollection);

        var parentNode = new MaterialNode(material.Name, isExpressionName: false)
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
            
            NodeCache.AddOrUpdate(new MaterialNodeComment(editorCommentExpression.Name)
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
                        Connections.Add(new MaterialNodeConnection(existingInput.From, pinInput));
                    }
                    
                    var existingOutputs = Connections.Where(con => targetRerouteNode.Equals(con.From.Parent)).ToArray();
                    foreach (var existingOutput in existingOutputs)
                    {
                        Connections.Add(new MaterialNodeConnection(startPinNode.GetOutput(pinName)!, existingOutput.To));
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
                        Connections.Add(new MaterialNodeConnection(existingInput.From, endPinNode.GetInput(pinName)!));
                    }
                    
                    var existingOutputs = Connections.Where(con => targetRerouteNode.Equals(con.From.Parent)).ToArray();
                    foreach (var existingOutput in existingOutputs)
                    {
                        Connections.Add(new MaterialNodeConnection(pinOutput, existingOutput.To));
                    }
                    
                    Connections.RemoveMany(existingInputs);
                    Connections.RemoveMany(existingOutputs);
                    NodeCache.Remove(targetRerouteNode);
                }
            }

            var subgraphNodes = new HashSet<MaterialNode>();
            var subgraphConnections = new HashSet<MaterialNodeConnection>();

            CollectSubgraphNodes(endPinNode);

            node.Subgraph = new MaterialData
            {
                DataName = node.Label,
                Connections = [..subgraphConnections],
            };
            
            node.Subgraph.NodeCache.AddOrUpdate(subgraphNodes);
            
            NodeCache.RemoveKeys(subgraphNodes.Select(node => node.ExpressionName));
            Connections.RemoveMany(subgraphConnections);
            continue;

            void CollectSubgraphNodes(MaterialNode current)
            {
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

    private ObservableCollection<MaterialNodeProperty> CollectProperties(UMaterialExpression expression)
    {
        var properties = new ObservableCollection<MaterialNodeProperty>();
        foreach (var property in expression.Properties)
        {
            if (property.Tag is null) continue;
            if (IgnoredPropertyNames.Contains(property.Name.Text)) continue;

            var propType = property.Tag.GenericValue?.GetType();
            if (propType is null) continue;
            if (propType.IsArray) continue;
            if (IgnoredPropertyTypes.Contains(propType)) continue;
            
            properties.Add(new MaterialNodeProperty
            {
                Key = property.Name.Text,
                Value = property.Tag!.GenericValue!
            });
        }

        return properties;
    }

    private void AddInput(ref MaterialNode node, FExpressionInput expressionInput, string? nameOverride = null)
    {
        if (expressionInput.Expression?.Load<UMaterialExpression>() is not { } subExpression) return;
        
        var targetNode = (MaterialNode?) NodeCache.Items.FirstOrDefault(node => node.ExpressionName.Equals(subExpression.Name, StringComparison.OrdinalIgnoreCase)) ?? AddNode(subExpression);

        var inputSocket = node.AddInput(new MaterialNodeSocket(nameOverride ?? expressionInput.InputName.Text));
        
        if (expressionInput.OutputIndex >= targetNode.Outputs.Count)
            Log.Warning("Expression {expressionName} has no output index {outputIndex}", targetNode.ExpressionName, expressionInput.OutputIndex);
        
        Connections.Add(new MaterialNodeConnection(targetNode.Outputs[Math.Min(expressionInput.OutputIndex, targetNode.Outputs.Count - 1)], inputSocket));
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
                    "MaterialExpressionNamedRerouteUsage" => expression.Get<UMaterialExpression>("Declaration"),
                    _ => expression
                };
                
                var name = declaration.Get<FName>("Name").Text;
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
                
                node.Content = new Image
                {
                    Width = 128,
                    Height = 128,
                    Source = bitmap,
                    Margin = SpaceExtension.Space(1)
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
                        node.Label = expression.Get<FName>("ParameterName").Text;
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
                var name = expression.Get<FName>("ParameterName").Text;
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
        node.AddOutput(new MaterialNodeSocket("RGB"));
        node.Outputs.Add(new MaterialNodeSocket("R")
        {
            SocketColor = Colors.Red
        });
        node.AddOutput(new MaterialNodeSocket("G")
        {
            SocketColor = Colors.Green
        });
        node.AddOutput(new MaterialNodeSocket("B")
        {
            SocketColor = Colors.Blue
        });
        node.AddOutput(new MaterialNodeSocket("A")
        {
            SocketColor = Colors.DarkGray
        });
        if (includeRGBA)
            node.AddOutput(new MaterialNodeSocket("RGBA"));
    }
}