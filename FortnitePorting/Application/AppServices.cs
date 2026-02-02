using System;
using System.Linq;
using System.Reflection;
using FortnitePorting.Framework;
using FortnitePorting.Services;
using FortnitePorting.ViewModels;
using FortnitePorting.WindowModels;
using Microsoft.Extensions.DependencyInjection;

namespace FortnitePorting.Application;

public static class AppServices
{
    public static ServiceProvider Services;

    public static void Initialize()
    {
        var serviceCollection = new ServiceCollection();
        serviceCollection.AddCommonServices();
        serviceCollection.AddViewModels();

        Services = serviceCollection.BuildServiceProvider();
    }
    
    // Services
    public static AppService App => Services.GetRequiredService<AppService>();
    public static SettingsService AppSettings => Services.GetRequiredService<SettingsService>();
    public static DependencyService Dependencies => Services.GetRequiredService<DependencyService>();
    public static InfoService Info => Services.GetRequiredService<InfoService>();
    public static APIService Api => Services.GetRequiredService<APIService>();
    public static NavigationService Navigation => Services.GetRequiredService<NavigationService>();
    public static CUE4ParseService UEParse => Services.GetRequiredService<CUE4ParseService>();
    public static SupabaseService SupaBase => Services.GetRequiredService<SupabaseService>();
    public static ChatService Chat => Services.GetRequiredService<ChatService>();
    public static DiscordService Discord => Services.GetRequiredService<DiscordService>();
    public static BlackHoleService BlackHole => Services.GetRequiredService<BlackHoleService>();
    public static AssetLoaderService AssetLoading => Services.GetRequiredService<AssetLoaderService>();
    public static ExportClientService ExportClient => Services.GetRequiredService<ExportClientService>();
   
    // ViewModels
    public static AppWindowModel AppWM => Services.GetRequiredService<AppWindowModel>();
    
    public static ChatViewModel ChatVM => Services.GetRequiredService<ChatViewModel>();
    
    public static FilesViewModel FilesVM => Services.GetRequiredService<FilesViewModel>();
    public static MapViewModel MapVM => Services.GetRequiredService<MapViewModel>();
    public static MusicViewModel MusicVM => Services.GetRequiredService<MusicViewModel>();
    public static TimeWasterViewModel TimeWasterVM => Services.GetRequiredService<TimeWasterViewModel>();
    public static SoundPreviewWindowModel SoundPreviewWM => Services.GetRequiredService<SoundPreviewWindowModel>();
    
}

public static class AppServiceExtensions
{
    extension(ServiceCollection collection)
    {
        public  void AddCommonServices()
        {
            var serviceTypes = Assembly.GetAssembly(typeof(IService))?
                .GetTypes()
                .Where(type => !type.IsAbstract && type.IsAssignableTo(typeof(IService))) ?? [];

            foreach (var serviceType in serviceTypes)
            {
                collection.AddSingleton(serviceType);
            }
        }
    
        public void AddViewModels()
        {
            var viewModelTypes = Assembly.GetAssembly(typeof(ViewModelBase))?
                .GetTypes()
                .Where(type => !type.IsAbstract && type.IsAssignableTo(typeof(ViewModelBase))) ?? [];

            foreach (var viewModelType in viewModelTypes)
            {
                if (viewModelType.GetCustomAttribute<TransientAttribute>() is not null)
                {
                    collection.AddTransient(viewModelType);
                }
                else
                {
                    collection.AddSingleton(viewModelType);
                }
            }
        }
    }
}

public class TransientAttribute : Attribute;