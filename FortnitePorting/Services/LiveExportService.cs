using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using FortnitePorting.Extensions;
using FortnitePorting.Framework;
using FortnitePorting.Models.Chat;
using FortnitePorting.Models.Leaderboard;
using FortnitePorting.Models.Supabase.Tables;
using FortnitePorting.Shared.Extensions;
using Mapster;
using Serilog;
using Supabase.Realtime;
using Supabase.Realtime.Interfaces;
using Supabase.Realtime.Models;
using Constants = Supabase.Postgrest.Constants;

namespace FortnitePorting.Services;

public partial class LiveExportService : ObservableObject, IService
{
    [ObservableProperty] private SupabaseService _supaBase;
    
    public LiveExportService(SupabaseService supaBase)
    {
        SupaBase = supaBase;
    }

    [ObservableProperty] private ObservableCollection<LeaderboardLiveExportEntry> _entries = [];

    private RealtimeChannel _exportChannel;
    private RealtimeBroadcast<BaseBroadcast> _exportBroadcast;

    public async Task Initialize()
    {
        _exportChannel = SupaBase.Client.Realtime.Channel("live_exports");

        await InitializeBroadcasts();
        
        await _exportChannel.Subscribe();
    }

    public async Task Uninitialize()
    {
        _exportChannel.Unsubscribe();
    }
    
    private async Task InitializeBroadcasts()
    {
        if (_exportBroadcast is not null) return;
        
        _exportBroadcast = _exportChannel.Register<BaseBroadcast>();
        _exportBroadcast.AddBroadcastEventHandler((sender, broadcast) =>
        {
            if (broadcast is null) return;

            switch (broadcast.Event)
            {
                case "insert_export":
                {
                    var instancedId = broadcast.Get<string>("instance_id");
                    var userId = broadcast.Get<string>("user_id");
                    var timestamp = broadcast.Get<DateTime>("timestamp").ToLocalTime();
                    var objectPaths = broadcast.GetArray<string>("object_paths");

                    var entry = new LeaderboardLiveExportEntry(instancedId, userId, timestamp, 
                        [..objectPaths.Select(objectPath => new LeaderboardLiveExport(objectPath))]);

                    Entries.Insert(0, entry);
                    break;
                }
              
            }
        });
    }
}