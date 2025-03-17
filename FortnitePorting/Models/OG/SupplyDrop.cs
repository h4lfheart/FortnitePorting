using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Media;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CUE4Parse.UE4.Assets.Exports;
using CUE4Parse.UE4.Assets.Exports.Texture;
using CUE4Parse.UE4.Objects.Engine;
using CUE4Parse.Utils;
using FluentAvalonia.UI.Controls;
using FortnitePorting.Application;
using FortnitePorting.Export;
using FortnitePorting.Models.Leaderboard;
using FortnitePorting.Models.TimeWaster.Audio;
using FortnitePorting.Models.Unreal;
using FortnitePorting.Shared.Extensions;
using FortnitePorting.Shared.Services;

namespace FortnitePorting.Models.OG;

public partial class SupplyDrop : ObservableObject
{
    
    public MatrixTransform Transform =>
        new(Matrix.CreateScale(Scale, Scale) * Matrix.CreateTranslation(XPosition, YPosition));
    
    [ObservableProperty, NotifyPropertyChangedFor(nameof(Transform))] private float _xPosition;
    [ObservableProperty, NotifyPropertyChangedFor(nameof(Transform))] private float _yPosition;
    [ObservableProperty, NotifyPropertyChangedFor(nameof(Transform))] private float _scale = 1;
    [ObservableProperty] private bool _isAlive;

    public bool IsOpening = false;
    
    private float _personalTime;
    private float _spawnX;

    public const float DELTA_TIME = 1f / 60f;
    
    private const float SPEED = 1;
    
    private static readonly CachedSound SupplyDropAppearSound = new("avares://FortnitePorting/Assets/OG/SupplyDrop_Appear_01.ogg");
    private static readonly CachedSound SupplyDropReticleAppearSound = new("avares://FortnitePorting/Assets/OG/sfx_supplydrop_reticle_appear.ogg");
    private static readonly CachedSound SupplyDropOpenSound = new("avares://FortnitePorting/Assets/OG/sfx_supplydrop_open.ogg");
    private static readonly CachedSound SupplyDropLandSound = new("avares://FortnitePorting/Assets/OG/SupplyDrop_Land_01.ogg");

    private static readonly string[] ResourcePaths = 
    [
        "FortniteGame/Content/Items/Art_noLOD/Meshes/IBeam",
        "FortniteGame/Content/Items/Art_noLOD/Meshes/Logs",
        "FortniteGame/Content/Items/Art_noLOD/Meshes/S_Loot_Stone"
    ];
    
    private static readonly string[] HealingPaths = 
    [
        "FortniteGame/Content/Items/Art_noLOD/Meshes/MedKit"
    ];
    
      
    private static readonly string[] WeaponPaths = 
    [
        "FortniteGame/Content/Weapons/FORT_RocketLaunchers/Mesh/SK_RPG7",
        "FortniteGame/Content/Weapons/FORT_Rifles/Mesh/SK_SCAR",
        
    ];
    
    public void Update()
    {
        if (!IsAlive) return;
        
        _personalTime += SPEED * DELTA_TIME;
        
        XPosition = _spawnX + MathF.Cos(_personalTime) * 25;
        YPosition += SPEED;
    }

    public void Spawn()
    {    

        _spawnX = Random.Shared.Next(100, (int) AppWM.Bounds.Width - 100);
        YPosition = -100;
        
        PlaySFX(SupplyDropAppearSound);
        PlaySFX(SupplyDropReticleAppearSound);
        
        IsAlive = true;
    }
    
    public void Destroy()
    {
        IsAlive = false;

        PlaySFX(SupplyDropLandSound);
    }

    public async void Open()
    {
        IsOpening = true;
        var xaml =
            """
                <ContentControl xmlns="https://github.com/avaloniaui"
                            xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml"
                            xmlns:ext="clr-namespace:FortnitePorting.Shared.Extensions;assembly=FortnitePorting.Shared"
                            xmlns:shared="clr-namespace:FortnitePorting.Shared;assembly=FortnitePorting.Shared"
                            xmlns:fortnitePorting="clr-namespace:FortnitePorting">
                    <StackPanel HorizontalAlignment="Stretch">
                        <TextBlock Text="Would you like to open this supply drop?" TextWrapping="Wrap"/>
                        <ComboBox x:Name="ExportLocationBox" SelectedIndex="0" Margin="{ext:Space 0, 1, 0, 0}"
                                  ItemsSource="{ext:EnumToItemsSource {x:Type fortnitePorting:EExportLocation}}"
                                  HorizontalAlignment="Stretch">
                            <ComboBox.ItemContainerTheme>
                                <ControlTheme x:DataType="ext:EnumRecord" TargetType="ComboBoxItem" BasedOn="{StaticResource {x:Type ComboBoxItem}}">
                                    <Setter Property="IsEnabled" Value="{Binding !IsDisabled}"/>
                                </ControlTheme>
                            </ComboBox.ItemContainerTheme>
                        </ComboBox>
                    </StackPanel>
                </ContentControl>
            """;

        var content = xaml.CreateXaml<ContentControl>(new {});
        
        IsAlive = false;

        PlaySFX(SupplyDropOpenSound);
        
        var comboBox = content.FindControl<ComboBox>("ExportLocationBox");
        var exportDialog = new ContentDialog
        {
            Title = "Supply Drop",
            Content = content,
            CloseButtonText = "No",
            PrimaryButtonText = "Yes",
            PrimaryButtonCommand = new RelayCommand(async () =>
            {
                TaskService.Run(async () =>
                {
                    string[] allExportPaths = [..ResourcePaths, HealingPaths.Random()!, WeaponPaths.Random()!];

                    var exports = new List<KeyValuePair<UObject, EExportType>>();
                    foreach (var path in allExportPaths)
                    {
                        var asset = await CUE4ParseVM.Provider.SafeLoadPackageObjectAsync(path);
                        if (asset is null) continue;

                        var exportType = Exporter.DetermineExportType(asset);
                        if (exportType is EExportType.None) continue;

                        exports.Add(new KeyValuePair<UObject, EExportType>(asset, exportType));
                    }

                    var enumRecord = (EnumRecord)comboBox.SelectedItem;
                    var exportLocation = (EExportLocation)enumRecord.Value;
                    await Exporter.Export(exports, AppSettings.Current.CreateExportMeta(exportLocation));

                    if (AppSettings.Current.Online.UseIntegration)
                    {
                        var sendExports = exports.Select(export =>
                        {
                            var (asset, type) = export;
                            return new PersonalExport(asset.GetPathName());
                        });

                        await ApiVM.FortnitePorting.PostExportsAsync(sendExports);
                    }
                });
            })
        };

        await exportDialog.ShowAsync();

        IsOpening = false;
    }

    private void PlaySFX(CachedSound sound)
    {
        if (!AppSettings.Current.Application.UseOGAudio) return;
        
        sound.Play();
    }

}