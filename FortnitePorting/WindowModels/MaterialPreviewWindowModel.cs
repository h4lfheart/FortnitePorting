using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Collections;
using Avalonia.Controls;
using Avalonia.Media;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CUE4Parse_Conversion.Textures;
using CUE4Parse.UE4.Assets.Exports;
using CUE4Parse.UE4.Assets.Exports.Material;
using CUE4Parse.UE4.Assets.Exports.Material.Editor;
using CUE4Parse.UE4.Assets.Exports.Texture;
using CUE4Parse.UE4.Assets.Objects;
using CUE4Parse.UE4.Objects.Core.Math;
using CUE4Parse.UE4.Objects.Engine;
using CUE4Parse.UE4.Objects.UObject;
using CUE4Parse.Utils;
using FluentAvalonia.UI.Controls;
using FortnitePorting.Extensions;
using FortnitePorting.Models.Unreal;
using FortnitePorting.Models.Unreal.Material;
using FortnitePorting.Rendering;
using FortnitePorting.Shared.Extensions;
using FortnitePorting.Shared.Services;
using FortnitePorting.ViewModels;
using FortnitePorting.Windows;
using Microsoft.VisualBasic.Logging;
using ScottPlot.Colormaps;
using ColorSpectrumShape = Avalonia.Controls.ColorSpectrumShape;
using Log = Serilog.Log;
using Orientation = Avalonia.Layout.Orientation;

namespace FortnitePorting.WindowModels;

public partial class MaterialPreviewWindowModel : WindowModelBase
{
    [ObservableProperty] private UObject _asset;
    [ObservableProperty] private ObservableCollection<MaterialNode> _nodes = [];
    [ObservableProperty] private ObservableCollection<MaterialNodeConnection> _connections = [];
    [ObservableProperty] private MaterialNode? _selectedNode;

    [RelayCommand]
    public async Task Refresh()
    {
        Nodes.Clear();
        Connections.Clear();
        
        if (Asset is UMaterial material)
            LoadMaterial(material);
        if (Asset is UMaterialFunction materialFunction)
            LoadMaterialFunction(materialFunction);
    }

    public void LoadMaterialFunction(UMaterialFunction materialFunction)
    {
        Asset = materialFunction;

        if (!materialFunction.TryLoadEditorData<UMaterialFunctionEditorOnlyData>(out var editorData)) return;
        if (editorData is null) return;
        
        var expressionCollection = editorData.GetOrDefault<FStructFallback>("ExpressionCollection");
        LoadExpressionCollection(expressionCollection);
    }

    public void LoadMaterial(UMaterial material)
    {
        Asset = material;
        
        if (!material.TryLoadEditorData<UMaterialEditorOnlyData>(out var editorData)) return;
        if (editorData is null) return;
        
        var expressionCollection = editorData.GetOrDefault<FStructFallback>("ExpressionCollection");
        LoadExpressionCollection(expressionCollection);

        var parentNode = new MaterialNode(material.Name, isExpressionName: false);
        Nodes.Add(parentNode);

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
            var editorComment = editorCommentLazy.Load();
            if (editorComment is null) continue;
            
            var x = editorComment.GetOrDefault<int>("MaterialExpressionEditorX");
            var y = editorComment.GetOrDefault<int>("MaterialExpressionEditorY");
            var sizeX = editorComment.GetOrDefault<int>("SizeX");
            var sizeY = editorComment.GetOrDefault<int>("SizeY");
            var text = editorComment.GetOrDefault<string>("Text");
            
            Nodes.Add(new MaterialNodeComment(text)
            {
                Location = new Point(x, y),
                Size = new Size(sizeX, sizeY)
            });
        }

        var expressions = expressionCollection.GetOrDefault<FPackageIndex[]>("Expressions", []);
        foreach (var expressionLazy in expressions)
        {
            var expression = expressionLazy.Load<UMaterialExpression>();
            if (expression is null) continue;

            if (Nodes.Any(node => node.ExpressionName.Equals(expression.Name, StringComparison.OrdinalIgnoreCase))) continue;

            AddNode(expression);
        }
    }

    private MaterialNode AddNode(UMaterialExpression expression)
    {
        var x = expression.GetOrDefault<int>("MaterialExpressionEditorX");
        var y = expression.GetOrDefault<int>("MaterialExpressionEditorY");

        var node = new MaterialNode(expression.Name)
        {
            Location = new Point(x, y)
        };
        
        Nodes.Add(node);
        
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
        
        var targetNode = Nodes.FirstOrDefault(node => node.ExpressionName.Equals(subExpression.Name, StringComparison.OrdinalIgnoreCase), AddNode(subExpression));
        
        var inputSocket = node.AddInput(new MaterialNodeSocket(nameOverride ?? expressionInput.InputName.Text));
        if (expressionInput.OutputIndex < targetNode.Outputs.Count)
            Connections.Add(new MaterialNodeConnection(targetNode.Outputs[expressionInput.OutputIndex], inputSocket));
        else
            Log.Warning("Expression {expressionName} has no output index {outputIndex}", targetNode.ExpressionName, expressionInput.OutputIndex);
    }

    private void SetupNodeContent(ref MaterialNode node, UMaterialExpression expression)
    {
        switch (expression.ExportType)
        {
            case "MaterialExpressionMaterialFunctionCall":
            {
                var materialFunction = expression.Get<FPackageIndex>("MaterialFunction");
                node.DoubleClickPackage = materialFunction;
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
            case "MaterialExpressionFunctionInput":
            {
                var name = expression.Get<FName>("InputName").Text;
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
                break;
            }
            
            case "MaterialExpressionTextureSampleParameter2D":
            case "MaterialExpressionTextureSample":
            {
                AddColorInputs(ref node, includeRGBA: true);
                
                if (expression.GetOrDefault<UTexture>("Texture") is not { } texture) break;
                if (texture.Decode()?.ToWriteableBitmap() is not { } bitmap) break;
                
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

public partial class MaterialNode(string expressionName = "", bool isExpressionName = true) : ObservableObject
{
    [ObservableProperty, NotifyPropertyChangedFor(nameof(DisplayName))] private string _expressionName = expressionName;
    [ObservableProperty, NotifyPropertyChangedFor(nameof(DisplayName))] private string _label = expressionName;
    public string DisplayName => Label.Equals(ExpressionName) && isExpressionName ? Label.Replace("MaterialExpression", string.Empty).SubstringBefore("_") : Label;

    [ObservableProperty, NotifyPropertyChangedFor(nameof(HeaderBrush))] private Color _headerColor = Color.Parse("#C00f3547");
    public Brush HeaderBrush => new LinearGradientBrush
    {
        StartPoint = new RelativePoint(0, 0, RelativeUnit.Relative),
        EndPoint = new RelativePoint(1, 0, RelativeUnit.Relative),
        GradientStops = 
        [
            new GradientStop(HeaderColor, 0),
            new GradientStop(new Color(255 / 4, HeaderColor.R, HeaderColor.G, HeaderColor.B), 1),
        ]
    };
    
    [ObservableProperty,NotifyPropertyChangedFor(nameof(BorderBrush))] private bool _isSelected;
    public SolidColorBrush BorderBrush => new(IsSelected ? Color.Parse("#d77601") : Colors.Transparent);
    
    [ObservableProperty] private Point _location;
    [ObservableProperty] private object? _content;

    [ObservableProperty] private ObservableCollection<MaterialNodeSocket> _inputs = [];
    [ObservableProperty] private ObservableCollection<MaterialNodeSocket> _outputs = [];

    [ObservableProperty] private ObservableCollection<MaterialNodeProperty> _properties = [];

    public FPackageIndex? DoubleClickPackage;
    
    public MaterialNodeSocket AddInput(MaterialNodeSocket socket)
    {
        socket.Parent = this;
        Inputs.Add(socket);
        return socket;
    }
    
    public MaterialNodeSocket AddOutput(MaterialNodeSocket socket)
    {
        socket.Parent = this;
        Outputs.Add(socket);
        return socket;
    }
    
    public MaterialNodeSocket AddInput(string socketName)
    {
        return AddInput(new MaterialNodeSocket(socketName));
    }
    
    public MaterialNodeSocket AddOutput(string socketName)
    {
        return AddOutput(new MaterialNodeSocket(socketName));
    }
}

public partial class MaterialNodeProperty : ObservableObject
{
    [ObservableProperty] private string _key;
    [ObservableProperty] private object _value;
}

public partial class MaterialNodeComment(string text) : MaterialNode(text)
{
    [ObservableProperty] private Size _size;
}

public partial class MaterialNodeReroute(string expressionName) : MaterialNode(expressionName);

public partial class MaterialNodeSocket(string name) : ObservableObject
{
    [ObservableProperty] private string _name = name;
    [ObservableProperty] private Point _anchor;
    [ObservableProperty, NotifyPropertyChangedFor(nameof(SocketBrush))] private Color _socketColor = Colors.LightGray;

    public SolidColorBrush SocketBrush => new(SocketColor);

    public MaterialNode Parent;
}

public partial class MaterialNodeConnection(MaterialNodeSocket from, MaterialNodeSocket to) : ObservableObject
{
    [ObservableProperty] private MaterialNodeSocket _from = from;
    [ObservableProperty] private MaterialNodeSocket _to = to;

    public override string ToString()
    {
        return $"{From.Name} ({From.Parent.ExpressionName}) -> {To.Name} ({to.Parent.ExpressionName})";
    }
}