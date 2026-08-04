using System.IO;
using Microsoft.Extensions.DependencyInjection;
using GuitarInputController.Audio.Interfaces;
using GuitarInputController.Audio.Services;
using GuitarInputController.Input.Interfaces;
using GuitarInputController.Input.Services;
using GuitarInputController.Core.Services;
using GuitarInputController.App.Services;
using GuitarInputController.App.ViewModels;

namespace GuitarInputController.App;

/// <summary>
/// 应用程序依赖注入容器配置
/// </summary>
public static class AppHost
{
    public static IServiceProvider ServiceProvider { get; private set; } = null!;

    public static void Configure()
    {
        var services = new ServiceCollection();

        // 获取应用数据目录
        string appDataPath = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
            "GuitarInputController");

        // ─── Core Services ───
        services.AddSingleton<IMappingService, MappingService>();
        services.AddSingleton<IProfileService>(sp =>
            new ProfileService(Path.Combine(appDataPath, "profiles")));
        services.AddSingleton<ISettingsService>(sp =>
            new SettingsService(Path.Combine(appDataPath, "settings.json")));

        // ─── Audio Services ───
        services.AddSingleton<IAudioCaptureService, AudioCaptureService>();
        services.AddSingleton<IPitchDetector>(sp =>
        {
            var settings = sp.GetRequiredService<ISettingsService>().Load();
            return new YinPitchDetector(
                yinThreshold: 0.15f,
                minFrequency: 65,
                maxFrequency: 1500)
            {
                ConfidenceThreshold = settings.PitchDetection.ConfidenceThreshold,
                VolumeThreshold = settings.PitchDetection.VolumeThreshold
            };
        });

        // ─── Input Services ───
        services.AddSingleton<IInputSimulator, InputSimulator>();

        // ─── App Engine ───
        services.AddSingleton<AudioInputEngine>();

        // ─── ViewModels ───
        services.AddTransient<MainViewModel>();
        services.AddTransient<FretboardViewModel>();
        services.AddTransient<VirtualKeyboardViewModel>();
        services.AddTransient<AdvancedSettingsViewModel>();
        services.AddTransient<MappingEditorViewModel>();

        ServiceProvider = services.BuildServiceProvider();
    }

    public static T GetService<T>() where T : notnull => ServiceProvider.GetRequiredService<T>();
}
