using App.Services;
using Velopack;

namespace App;

public partial class App : Application
{
    private readonly TemplateMetadata _metadata;

    public App(UpdateStartupState startupState, TemplateMetadata metadata)
    {
        _metadata = metadata;

        VelopackApp.Build()
            .OnFirstRun(version => startupState.FirstRunVersion = version.ToString())
            .OnRestarted(version => startupState.RestartedVersion = version.ToString())
            .Run();

        InitializeComponent();
    }

    protected override Window CreateWindow(IActivationState? activationState)
    {
        return new Window(new MainPage()) { Title = _metadata.DisplayName };
    }
}
