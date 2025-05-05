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
    public static BlackHoleService BlackHole => Services.GetRequiredService<BlackHoleService>();
    
    // ViewModels
    public static AppWindowModel AppWM => Services.GetRequiredService<AppWindowModel>();
    
    public static ChatViewModel ChatVM => Services.GetRequiredService<ChatViewModel>();
    public static VotingViewModel VotingVM => Services.GetRequiredService<VotingViewModel>();
    
    public static FilesViewModel FilesVM => Services.GetRequiredService<FilesViewModel>();
    public static MapViewModel MapVM => Services.GetRequiredService<MapViewModel>();
    public static RadioViewModel RadioVM => Services.GetRequiredService<RadioViewModel>();
    
    
    public static ConsoleViewModel ConsoleVM => Services.GetRequiredService<ConsoleViewModel>();
    
    public static TimeWasterViewModel TimeWasterVM => Services.GetRequiredService<TimeWasterViewModel>();
    public static SoundPreviewWindowModel SoundPreviewWM => Services.GetRequiredService<SoundPreviewWindowModel>();
    
}

public static class AppServiceExtensions
{
    public static void AddCommonServices(this ServiceCollection collection)
    {
        var serviceTypes = Assembly.GetAssembly(typeof(IService))?
            .GetTypes()
            .Where(type => !type.IsAbstract && type.IsAssignableTo(typeof(IService))) ?? [];

        foreach (var serviceType in serviceTypes)
        {
            collection.AddSingleton(serviceType);
        }
    }
    
    public static void AddViewModels(this ServiceCollection collection)
    {
        var viewModelTypes = Assembly.GetAssembly(typeof(ViewModelBase))?
            .GetTypes()
            .Where(type => !type.IsAbstract && type.IsAssignableTo(typeof(ViewModelBase))) ?? [];

        foreach (var viewModelType in viewModelTypes)
        {
            collection.AddSingleton(viewModelType);
        }
    }
}