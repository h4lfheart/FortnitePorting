using System;
using AdonisUI.Controls;
using AutoUpdaterDotNET;
using FortnitePorting.AppUtils;
using FortnitePorting.Views;
using Newtonsoft.Json;

namespace FortnitePorting.Services;

public static class UpdateService
{
    private static bool IgnoreEqualMessage = false;

    public static void Initialize()
    {
        AutoUpdater.InstalledVersion = new Version(Globals.VERSION);
        AutoUpdater.ParseUpdateInfoEvent += ParseUpdateInfo;
        AutoUpdater.CheckForUpdateEvent += CheckForUpdate;
        AutoUpdater.ApplicationExitEvent += FinishedUpdate;
    }

    public static void Start(bool automaticCheck = false)
    {
        IgnoreEqualMessage = automaticCheck;
        AutoUpdater.Start($"https://www.halfheart.dev/fortnite-porting/{AppSettings.Current.UpdateMode.ToString().ToLower()}.json");
    }

    public static (bool UpdateAvailable, Version UpdateVersion) GetStats()
    {
        var releaseData = EndpointService.FortnitePorting.GetReleaseInfo(AppSettings.Current.UpdateMode);
        var currentVersion = new Version(Globals.VERSION);
        if (releaseData is null) return (false, currentVersion);

        var updateVersion = new Version(releaseData.Version);
        return (currentVersion != updateVersion, updateVersion);
    }

    private static void FinishedUpdate()
    {
        Log.Information("Finished Update");
    }

    private static void CheckForUpdate(UpdateInfoEventArgs args)
    {
        if (args.CurrentVersion is null)
        {
            if (!IgnoreEqualMessage)
                MessageBox.Show("There was an issue trying to reach the update server, please check your internet connection or try again later.", "Update Check Failed", MessageBoxButton.OK, MessageBoxImage.Error);
            return;
        }

        var updateVersion = new Version(args.CurrentVersion);
        if (updateVersion == args.InstalledVersion)
        {
            if (!IgnoreEqualMessage)
                MessageBox.Show($"FortnitePorting {AppSettings.Current.UpdateMode} is up-to-date.", "No Update Available.", MessageBoxButton.OK, MessageBoxImage.Information);
            return;
        }

        var isDowngrade = updateVersion < args.InstalledVersion;
        var messageBox = new MessageBoxModel
        {
            Text = $"FortnitePorting {AppSettings.Current.UpdateMode} has {(isDowngrade ? "a downgrade" : "an update")} available from {args.InstalledVersion} to {updateVersion}. Would you like to {(isDowngrade ? "downgrade" : "update")} now?\n" + $"\nChangelog:\n{args.ChangelogURL}",
            Caption = $"{(isDowngrade ? "Downgrade" : "Update")} Available",
            Icon = MessageBoxImage.Exclamation,
            Buttons = MessageBoxButtons.YesNo(),
            IsSoundEnabled = false
        };

        MessageBox.Show(MainView.YesWeDogs, messageBox);
        if (messageBox.Result == MessageBoxResult.No) return;

        if (AutoUpdater.DownloadUpdate(args))
        {
            AppSettings.Current.JustUpdated = true;
            AppVM.Quit();
        }
    }

    private static void ParseUpdateInfo(ParseUpdateInfoEventArgs args)
    {
        var info = JsonConvert.DeserializeObject<UpdateInfo>(args.RemoteData);
        if (info is null) return;

        args.UpdateInfo = new UpdateInfoEventArgs
        {
            CurrentVersion = info.Version,
            DownloadURL = info.DownloadURL,
            ChangelogURL = info.Changelog
        };
    }
}

public class UpdateInfo
{
    public string Version;
    public string DownloadURL;
    public string Changelog;
}