using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Avalonia.Media.Imaging;
using CommunityToolkit.Mvvm.ComponentModel;
using CUE4Parse_Conversion.Textures;
using CUE4Parse.Utils;
using FortnitePorting.Models.Assets;
using FortnitePorting.Shared.Extensions;
using Newtonsoft.Json;

namespace FortnitePorting.Models.Leaderboard;

public partial class LeaderboardPersonalExport : ObservableObject
{
    [ObservableProperty] [JsonProperty("export_id")] private string _exportId;
    [ObservableProperty] [JsonProperty("export_time")] private DateTime _timestamp;
    [ObservableProperty] [JsonProperty("export_path")] private string _objectPath;

}