using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using Avalonia;
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
using FortnitePorting.Rendering;
using FortnitePorting.Shared.Extensions;
using FortnitePorting.Shared.Services;
using FortnitePorting.ViewModels;
using FortnitePorting.Windows;
using Microsoft.VisualBasic.Logging;
using ScottPlot.Colormaps;
using ColorSpectrumShape = Avalonia.Controls.ColorSpectrumShape;
using Log = Serilog.Log;

namespace FortnitePorting.WindowModels;

public partial class MaterialPreviewWindowModel : WindowModelBase
{
    [ObservableProperty] private UMaterial _material;
    [ObservableProperty] private ObservableCollection<MaterialNode> _nodes = [];
    [ObservableProperty] private ObservableCollection<MaterialNodeConnection> _connections = [];

    [RelayCommand]
    public async Task Refresh()
    {
        Nodes.Clear();
        Connections.Clear();
        LoadMaterial(Material);
    }

    public void LoadMaterial(UMaterial material)
    {
        Material = material;
        
        if (!material.TryLoadEditorData<UMaterialEditorOnlyData>(out var editorData)) return;
        if (editorData is null) return;
        
        var expressionCollection = editorData.GetOrDefault<FStructFallback>("ExpressionCollection");
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

        var parentNode = new MaterialNode(material.Name, isExpressionName: false);
        Nodes.Add(parentNode);

        foreach (var property in editorData.Properties)
        {
            if (property.Tag?.GetValue<FExpressionInput>() is not { } expressionInput) continue;
            if (expressionInput.Expression?.Load<UMaterialExpression>() is not { } expression) continue;

            if (property.Name.Text.Equals("MaterialAttributes"))
            {
                Connections.RemoveAll(con => con.To.Parent.Equals(parentNode));
                parentNode.Inputs.Clear();
            }

            var input = new MaterialNodeSocket(property.Name.Text, parent: parentNode);
            parentNode.Inputs.Add(input);
            
            AddNode(expression, input);
        }
    }

    private void AddNode(UMaterialExpression? expression, MaterialNodeSocket parentNodeSocket)
    {
        if (expression is null) return;

        var x = expression.GetOrDefault<int>("MaterialExpressionEditorX");
        var y = expression.GetOrDefault<int>("MaterialExpressionEditorY");

        var node = new MaterialNode(expression.Name)
        {
            Location = new Point(x, y)
        };
        
        Nodes.Add(node);
        
        CustomNodeContent(ref node, expression, parentNodeSocket);

        if (node.Outputs.Count == 0)
        {
            var outputSocket = new MaterialNodeSocket("Result", parent: node);
            node.Outputs.Add(outputSocket);
        
            Connections.Add(new MaterialNodeConnection(outputSocket, parentNodeSocket));
        }

        foreach (var property in expression.Properties)
        {
            if (property.Tag?.GetValue<FExpressionInput>() is { } expressionInput)
            {
                AddInput(ref node, expressionInput, nameOverride: property.Name.Text);
            }
        }
    }

    private void AddInput(ref MaterialNode node, FExpressionInput expressionInput, string? nameOverride = null)
    {
        var inputSocket = new MaterialNodeSocket(nameOverride ?? expressionInput.InputName.Text, parent: node);
        
        node.Inputs.Add(inputSocket);
                
        if (expressionInput.Expression?.Load<UMaterialExpression>() is { } subExpresssion)
        {
            if (Nodes.FirstOrDefault(node => node.ExpressionName.Equals(subExpresssion.Name)) is { } existingNode)
            {
                var existingSocket = existingNode.GetOutput(0);
                if (existingSocket is not null)
                {
                    Connections.Add(new MaterialNodeConnection(existingSocket, inputSocket));
                    return;
                }
                    
                Debugger.Break();
            }
                
            AddNode(subExpresssion, inputSocket);
        }
    }

    private void CustomNodeContent(ref MaterialNode node, UMaterialExpression expression, MaterialNodeSocket parentNodeSocket)
    {
        //if (expression.Name.Contains("SetMaterialAttributes")) Debugger.Break();
        switch (expression.ExportType)
        {
            case "MaterialExpressionMaterialFunctionCall":
            {
                var materialFunction = expression.Get<FPackageIndex>("MaterialFunction");
                var inputs = expression.GetOrDefault<FStructFallback[]>("FunctionInputs", []);
                var outputs = expression.GetOrDefault<FStructFallback[]>("FunctionOutputs", []);
                
                node.Label = materialFunction.ResolvedObject?.Name.Text ?? "Material Function";
                
                node.Inputs.Clear();
                foreach (var functionInput in inputs)
                {
                    var expressionInput = functionInput.Get<FExpressionInput>("Input");
                    AddInput(ref node, expressionInput);
                }
                
                node.Outputs.Clear();
                foreach (var functionOutput in outputs)
                {
                    var output = functionOutput.Get<FStructFallback>("Output");
                    var outputName = output.Get<FName>("OutputName");
                    
                    var outputSocket = new MaterialNodeSocket(outputName.Text, parent: node);
                    node.Outputs.Add(outputSocket);
        
                    Connections.Add(new MaterialNodeConnection(outputSocket, parentNodeSocket));
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
            {
                var name = expression.Get<FName>("Name").Text;
                var nodeColor = expression.GetOrDefault<FLinearColor>("NodeColor");
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
                if (expression.GetOrDefault<UTexture2D>("Texture") is not { } texture) break;
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
            
                var normalizedColor = constantColor.ToFColor(false);    
                node.Content = new ColorPicker
                {
                    Color = new Color(normalizedColor.A, normalizedColor.R, normalizedColor.G, normalizedColor.B),
                    ColorSpectrumShape = ColorSpectrumShape.Ring,
                    IsColorPaletteVisible = false,
                    IsAlphaEnabled = true,
                    IsAlphaVisible = true,
                    Margin = SpaceExtension.Space(1),
                    Width = 96
                };
                
                break;
            }
            case "MaterialExpressionScalarParameter":
            {
                var name = expression.Get<FName>("ParameterName").Text;
                var sliderMin = expression.GetOrDefault<float>("SliderMin", 0);
                var sliderMax = expression.GetOrDefault<float>("SliderMax", 1);
                var defaultValue = expression.GetOrDefault<float>("DefaultValue");
                
                if (name.Equals("SphereMaskRadius")) Debugger.Break();
                
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
        }

    }

}

public partial class MaterialNode(string? expressionName = "", bool isExpressionName = true) : ObservableObject
{
    [ObservableProperty, NotifyPropertyChangedFor(nameof(DisplayName))] private string _expressionName = expressionName;
    [ObservableProperty, NotifyPropertyChangedFor(nameof(DisplayName))] private string _label = expressionName;
    public string DisplayName => Label.Equals(ExpressionName) && isExpressionName ? Label.Replace("MaterialExpression", string.Empty).SubstringBefore("_") : Label;

    [ObservableProperty, NotifyPropertyChangedFor(nameof(HeaderBrush))] private Color _headerColor = Color.Parse("#C00f3547");
    public SolidColorBrush HeaderBrush => new SolidColorBrush(HeaderColor);
    
    [ObservableProperty] private Point _location;
    [ObservableProperty] private object? _content;

    [ObservableProperty] private ObservableCollection<MaterialNodeSocket> _inputs = [];
    [ObservableProperty] private ObservableCollection<MaterialNodeSocket> _outputs = [];
    

    public MaterialNodeSocket? GetInput(string name)
    {
        return Inputs.FirstOrDefault(input => input.Name.Equals(name));
    }
    
    public MaterialNodeSocket? GetOutput(string name)
    {
        return Outputs.FirstOrDefault(input => input.Name.Equals(name));
    }
    
    public MaterialNodeSocket? GetInput(int index)
    {
        return Inputs[index];
    }
    
    public MaterialNodeSocket? GetOutput(int index)
    {
        return Outputs[index];
    }
}

public partial class MaterialNodeComment(string text) : MaterialNode(text)
{
    [ObservableProperty] private Size _size;
}

public partial class MaterialNodeReroute : MaterialNode
{
    
}

public partial class MaterialNodeSocket(string name, MaterialNode? parent = null) : ObservableObject
{
    [ObservableProperty] private string _name = name;
    [ObservableProperty] private Point _anchor;

    public MaterialNode Parent = parent;
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