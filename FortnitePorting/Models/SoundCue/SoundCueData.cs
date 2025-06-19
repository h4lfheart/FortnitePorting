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
using CUE4Parse.UE4.Assets.Exports.Sound;
using CUE4Parse.UE4.Assets.Exports.Sound.Node;
using CUE4Parse.UE4.Assets.Exports.Texture;
using CUE4Parse.UE4.Assets.Objects;
using CUE4Parse.UE4.Objects.Core.Math;
using CUE4Parse.UE4.Objects.Core.Misc;
using CUE4Parse.UE4.Objects.Engine;
using CUE4Parse.UE4.Objects.UObject;
using CUE4Parse.Utils;
using DynamicData;
using DynamicData.Binding;
using FluentAvalonia.UI.Controls;
using FortnitePorting.Extensions;
using FortnitePorting.Models.SoundCue;
using FortnitePorting.Shared.Extensions;
using Newtonsoft.Json;
using ReactiveUI;
using Serilog;
using ColorSpectrumShape = Avalonia.Controls.ColorSpectrumShape;

namespace FortnitePorting.Models.SoundCue;

// TODO combine general node viewer logic w/ material viewer and separate construction logic
public partial class SoundCueData : ObservableObject
{
    [ObservableProperty] private string _dataName;
    [ObservableProperty] private UObject? _asset;
    [ObservableProperty] private ReadOnlyObservableCollection<SoundCueNodeBase> _nodes = new([]);
    [ObservableProperty] private ObservableCollection<SoundCueNodeConnection> _connections = [];
    [ObservableProperty] private SoundCueNodeBase? _selectedNode;
    [ObservableProperty] private string _searchFilter = string.Empty;
    
    public SourceCache<SoundCueNodeBase, string> NodeCache { get; set; } = new(item => item.ExpressionName);

    private static Type[] JsonPropertyTypes =
    [
        typeof(FScriptStruct), typeof(FStructFallback), typeof(UScriptArray), typeof(UScriptMap), typeof(UScriptSet)
    ];
    
    private static string[] IgnoredPropertyNames =
    [
        "ChildNodes"
    ];
    
    
    private const float VERTICAL_SPACING = 75f;
    private const float HORIZONTAL_SPACING = 250f;

    public SoundCueData()
    {
        var assetFilter = this
            .WhenAnyValue<SoundCueData, string>(viewModel => viewModel.SearchFilter)
            .Select(CreateAssetFilter);
        
        NodeCache.Connect()
            .ObserveOn(RxApp.TaskpoolScheduler)
            .Filter(assetFilter)
            .Sort(SortExpressionComparer<SoundCueNodeBase>.Ascending(x => x.ExpressionName))
            .Bind(out var flatCollection)
            .Subscribe();

        Nodes = flatCollection;
    }
    
    private Func<SoundCueNodeBase, bool> CreateAssetFilter(string searchFilter)
    {
        return asset => MiscExtensions.Filter(asset.Label, searchFilter) || MiscExtensions.Filter(asset.ExpressionName, searchFilter);
    }
    
    [RelayCommand]
    public async Task Refresh()
    {
        NodeCache.Clear();
        Connections.Clear();
        
        if (Asset is USoundCue soundCue)
            LoadSoundCue(soundCue);
    }

    public void Load(UObject obj)
    {
        Asset = obj;
        DataName = Asset.Name;
        
        if (Asset is USoundCue soundCue)
            LoadSoundCue(soundCue);
    }
    
    public void LoadSoundCue(USoundCue soundCue)
    {
        var parentNode = new SoundCueNode("Output", isExpressionName: false)
        {
            HeaderColor = Color.Parse("#786859"),
            Content = new Image
            {
                Source = ImageExtensions.AvaresBitmap("avares://FortnitePorting/Assets/Unreal/SoundCue_SpeakerIcon.png"),
                Width = 128, Height = 128
            }
        };
        
        NodeCache.AddOrUpdate(parentNode);

        var firstNode = soundCue.FirstNode?.Load<USoundNode>();
        if (firstNode is null) return;

        AddInput(ref parentNode, firstNode);
        
        PositionSubtree(parentNode);
    }


    private SoundCueNode AddNode(USoundNode soundNode)
    {
        var node = new SoundCueNode(soundNode.Name)
        {
            Properties = CollectProperties(soundNode)
        };
        
        NodeCache.AddOrUpdate(node);
        SetupNodeContent(ref node, soundNode);

        var childNodes = soundNode.GetOrDefault<USoundNode[]>("ChildNodes", []);
        for (var childIndex = 0; childIndex < childNodes.Length; childIndex++)
        {
            AddInput(ref node, childNodes[childIndex], childIndex);
        }

        return node;
    }

    private void AddInput(ref SoundCueNode node, USoundNode soundNode, int index = 0)
    {
        var targetNode = (SoundCueNode?) NodeCache.Items.FirstOrDefault(node => node.ExpressionName.Equals(soundNode.Name, StringComparison.OrdinalIgnoreCase)) ?? AddNode(soundNode);

        var inputSocket = index < node.Inputs.Count ? node.Inputs[index] : node.AddInput(string.Empty);
        if (targetNode.Outputs.Count == 0)
            targetNode.AddOutput("Output");
        
        Connections.Add(new SoundCueNodeConnection(targetNode.Outputs[0], inputSocket));
    }

    private ObservableCollection<SoundCueNodeProperty> CollectProperties(USoundNode soundNode)
    {
        var properties = new ObservableCollection<SoundCueNodeProperty>();
        foreach (var property in soundNode.Properties)
        {
            var targetData = property.Tag!.GenericValue!;
            if (property.Tag is null) continue;
            if (IgnoredPropertyNames.Contains(property.Name.Text)) continue;

            var propType = property.Tag.GenericValue?.GetType();
            if (propType is null) continue;

            if (JsonPropertyTypes.Contains(propType) || propType.IsArray)
            {
                targetData = new NodePropertyJsonContainer
                {
                    JsonData = JsonConvert.SerializeObject(targetData, Formatting.Indented)
                };
            }
            
            properties.Add(new SoundCueNodeProperty
            {
                Key = property.Name.Text,
                Value = targetData
            });
        }

        return properties;
    }

    private void SetupNodeContent(ref SoundCueNode node, USoundNode soundNode)
    {
        switch (soundNode.ExportType)
        {
            case "SoundNodeWavePlayer":
            {
                var soundWavePath = soundNode.GetOrDefault<FSoftObjectPath?>("SoundWaveAssetPtr");
                if (soundWavePath is null) break;

                var assetName = soundWavePath.Value.AssetPathName.Text.SubstringAfterLast(".");
                node.Label = $"Wave Player : {assetName}";
                break;
            }
            case "SoundNodeQualityLevel":
            {
                node.AddInput("Default");
                node.AddInput("Low");
                node.AddInput("iOS");
                node.AddInput("Android");
                break;
            }
        }
    }
    
      
    private void PositionSubtree(SoundCueNode rootNode)
    {
        var nodePositions = new Dictionary<string, Point>();
        PositionSubtreeRightToLeft(rootNode, 0, 0, nodePositions);
        
        foreach (var (nodeName, position) in nodePositions)
        {
            var node = NodeCache.Items.FirstOrDefault(n => n.ExpressionName == nodeName);
            if (node != null)
            {
                node.Location = new Point(position.X, position.Y + 300);
            }
        }
    }
    
    private List<SoundCueNode> GetChildNodes(SoundCueNode parentNode)
    {
        return Connections
            .Where(c => c.To.Parent.ExpressionName == parentNode.ExpressionName)
            .Select(c => c.From.Parent)
            .Where(fromNode => fromNode.ExpressionName != parentNode.ExpressionName)
            .ToList();
    }

    private float CalculateSubtreeHeight(SoundCueNode node)
    {
        var childNodes = GetChildNodes(node);
        if (childNodes.Count == 0) 
            return VERTICAL_SPACING;
        
        return Math.Max(VERTICAL_SPACING, childNodes.Sum(CalculateSubtreeHeight));
    }

    private float PositionSubtreeRightToLeft(SoundCueNode node, int level, double centerY, Dictionary<string, Point> positions)
    {
        // Position current node
        var x = level * HORIZONTAL_SPACING + node.Label.Length * 14;
        positions[node.ExpressionName] = new Point(-x, centerY);
        
        var childNodes = GetChildNodes(node);
        if (childNodes.Count == 0)
            return VERTICAL_SPACING;
        
        // Calculate heights for all children
        var childHeights = childNodes.Select(CalculateSubtreeHeight).ToList();
        var totalChildHeight = childHeights.Sum();
        
        // Position children vertically centered around the parent
        var startY = centerY - (totalChildHeight / 2);
        var currentY = startY;
        
        for (var i = 0; i < childNodes.Count; i++)
        {
            var childCenterY = currentY + (childHeights[i] / 2);
            PositionSubtreeRightToLeft(childNodes[i], level + 1, childCenterY, positions);
            currentY += childHeights[i];
        }
        
        return Math.Max(VERTICAL_SPACING, totalChildHeight);
    }
}