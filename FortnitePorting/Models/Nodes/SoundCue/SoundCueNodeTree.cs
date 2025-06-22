using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Media;
using CUE4Parse.UE4.Assets.Exports;
using CUE4Parse.UE4.Assets.Exports.Sound;
using CUE4Parse.UE4.Assets.Exports.Sound.Node;
using CUE4Parse.UE4.Objects.UObject;
using CUE4Parse.Utils;
using DynamicData;
using FortnitePorting.Extensions;
using Newtonsoft.Json;

namespace FortnitePorting.Models.Nodes.SoundCue;

public class SoundCueNodeTree : NodeTree
{
    protected override string[] IgnoredPropertyNames { get; set; } = ["ChildNodes"];

    public override void Load(UObject obj)
    {
        base.Load(obj);

        var soundCue = (USoundCue) obj;
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
            Properties = CollectProperties(soundNode),
            HeaderColor = Color.Parse("#748394")
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
        var targetNode = NodeCache.Items.OfType<SoundCueNode>().FirstOrDefault(node => node.ExpressionName.Equals(soundNode.Name, StringComparison.OrdinalIgnoreCase)) ?? AddNode(soundNode);

        var inputSocket = index < node.Inputs.Count ? node.Inputs[index] : node.AddInput(string.Empty);
        if (targetNode.Outputs.Count == 0)
            targetNode.AddOutput("Output");
        
        Connections.Add(new NodeConnection(targetNode.Outputs[0], inputSocket));
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
    
    private const float VERTICAL_SPACING = 75f;
    private const float HORIZONTAL_SPACING = 250f;
      
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
    
    private List<BaseNode> GetChildNodes(BaseNode parentNode)
    {
        return Connections
            .Where(c => c.To.Parent.ExpressionName == parentNode.ExpressionName)
            .Select(c => c.From.Parent)
            .Where(fromNode => fromNode.ExpressionName != parentNode.ExpressionName)
            .ToList();
    }

    private float CalculateSubtreeHeight(BaseNode node)
    {
        var childNodes = GetChildNodes(node);
        return childNodes.Count == 0 ? VERTICAL_SPACING : Math.Max(VERTICAL_SPACING, childNodes.Sum(CalculateSubtreeHeight));
    }

    private float PositionSubtreeRightToLeft(BaseNode node, int level, double centerY, Dictionary<string, Point> positions)
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