using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Avalonia.Media.Imaging;
using CUE4Parse_Conversion.Textures;
using CUE4Parse.Utils;
using FortnitePorting.Models.Assets;
using FortnitePorting.Shared.Extensions;

namespace FortnitePorting.Models.Leaderboard;

public class PersonalExport
{
    public Guid UserId { get; set; }
    public string ObjectName { get; set; }
    public string ObjectPath { get; set; }
    public string Category { get; set; }
    public DateTime TimeExported { get; set; }
    public Guid InstanceGuid { get; set; }
}