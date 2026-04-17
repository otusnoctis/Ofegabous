using App.Services;
using Microsoft.Extensions.Logging;

namespace App;

public static class MauiProgram
{
    public static MauiApp CreateMauiApp()
    {
        var builder = MauiApp.CreateBuilder();
        builder
            .UseMauiApp<App>()
            .ConfigureFonts(fonts => { fonts.AddFont("OpenSans-Regular.ttf", "OpenSansRegular"); });

        builder.Services.AddMauiBlazorWebView();
        builder.Services.AddSingleton<TemplateMetadata>();
        builder.Services.AddSingleton<TemplateEnvironment>();
        builder.Services.AddSingleton<SystemShellService>();
        builder.Services.AddSingleton<DocumentationService>();
        builder.Services.AddSingleton<PersistenceStore>();
        builder.Services.AddSingleton<UpdateLogStore>();
        builder.Services.AddSingleton<UpdateStartupState>();
        builder.Services.AddSingleton<AppUpdateService>();

#if DEBUG
        builder.Services.AddBlazorWebViewDeveloperTools();
        builder.Logging.AddDebug();
#endif

        return builder.Build();
    }
}
