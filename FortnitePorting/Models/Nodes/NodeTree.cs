using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Linq;
using System.Reactive.Linq;
using CommunityToolkit.Mvvm.ComponentModel;
using CUE4Parse.UE4.Assets.Exports;
using CUE4Parse.UE4.Assets.Objects;
using DynamicData;
using DynamicData.Binding;
using FortnitePorting.Shared.Extensions;
using Newtonsoft.Json;
using ReactiveUI;

namespace FortnitePorting.Models.Nodes;

public partial class NodeTree : ObservableObject
{
    [ObservableProperty] private string _treeName;
    [ObservableProperty] private UObject? _asset;
    
    [ObservableProperty] private ReadOnlyObservableCollection<BaseNode> _nodes = new([]);
    [ObservableProperty] private BaseNode? _selectedNode;
    
    [ObservableProperty] private ObservableCollection<NodeConnection> _connections = [];
    
    [ObservableProperty] private string _searchFilter = string.Empty;

    protected virtual string[] IgnoredPropertyNames { get; set; } = [];
    protected virtual Type[] IgnoredPropertyTypes { get; set; } = [];

    public SourceCache<BaseNode, string> NodeCache { get; set; } = new(item => item.ExpressionName);
    
    private static Type[] JsonPropertyTypes =
    [
        typeof(FScriptStruct), typeof(FStructFallback), typeof(UScriptArray), typeof(UScriptMap), typeof(UScriptSet)
    ];

    public NodeTree()
    {
        var assetFilter = this
            .WhenAnyValue(viewModel => viewModel.SearchFilter)
            .Select(CreateAssetFilter);
        
        NodeCache.Connect()
            .ObserveOn(RxApp.TaskpoolScheduler)
            .Filter(assetFilter)
            .Sort(SortExpressionComparer<BaseNode>.Ascending(x => x.ExpressionName))
            .Bind(out var flatCollection)
            .Subscribe();

        Nodes = flatCollection;
    }

    public virtual void Load(UObject obj)
    {
        TreeName = obj.Name;
        Asset = obj;
    }

    protected ObservableCollection<NodeProperty> CollectProperties(IPropertyHolder propertyHolder)
    {
        var properties = new ObservableCollection<NodeProperty>();
        foreach (var property in propertyHolder.Properties)
        {
            var targetData = property.Tag!.GenericValue!;
            if (property.Tag is null) continue;
            if (IgnoredPropertyNames.Contains(property.Name.Text)) continue;

            var propType = property.Tag.GenericValue?.GetType();
            if (property.Tag.GenericValue is FScriptStruct scriptStruct)
            {
                propType = scriptStruct.StructType.GetType();
            }
            
            if (propType is null) continue;
            if (IgnoredPropertyTypes.Contains(propType)) continue;

            if (JsonPropertyTypes.Contains(propType) || propType.IsArray)
            {
                targetData = new JsonPropertyContainer
                {
                    JsonData = JsonConvert.SerializeObject(targetData, Formatting.Indented)
                };
            }
            
            properties.Add(new NodeProperty
            {
                Key = property.Name.Text,
                Value = targetData
            });
        }

        return properties;
    }
    
    private Func<BaseNode, bool> CreateAssetFilter(string searchFilter)
    {
        return asset => MiscExtensions.Filter(asset.Label, searchFilter) || MiscExtensions.Filter(asset.ExpressionName, searchFilter);
    }
}

public partial class JsonPropertyContainer : ObservableObject
{
    [ObservableProperty] private string _jsonData;
}

