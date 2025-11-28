using System;
using CommunityToolkit.Mvvm.ComponentModel;
using Newtonsoft.Json;

namespace FortnitePorting.Models.Leaderboard;

public partial class LeaderboardPersonalExport : ObservableObject
{
    [ObservableProperty] [JsonProperty("export_id")] private string _exportId;
    [ObservableProperty] [JsonProperty("export_time")] private DateTime _timestamp;
    [ObservableProperty] [JsonProperty("export_path")] private string _objectPath;

}