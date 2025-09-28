using ColorControl.Shared.Contracts;
using ColorControl.UI.Services;
using Microsoft.AspNetCore.Hosting.Server;
using Microsoft.AspNetCore.Hosting.Server.Features;
using System.Net;

namespace ColorControl.UI;

public static class Blazor
{
    private static WebApplication? CurrentApplication;
    private static int Port;
    private static bool AllowRemoteConnections;

    public static bool IsRunning(Config config) => CurrentApplication != null && Port == config.UiPort && AllowRemoteConnections == config.UiAllowRemoteConnections;

    public static async Task Start(Config config)
    {
        if (IsRunning(config))
        {
            return;
        }

        await Stop();

        var builder = WebApplication.CreateBuilder();

        // Add services to the container.
        builder.Services.AddRazorComponents()
            .AddInteractiveServerComponents();

        builder.Services.AddSingleton<AppState>(new AppState { SelectedTheme = config.UseDarkMode ? "dark" : "light" });
        builder.Services.AddTransient<RpcUiClientService>();
        builder.Services.AddTransient<JSHelper>();
        builder.Services.AddHttpContextAccessor();
        builder.Services.AddSingleton<NotificationService>();

        builder.Services.Configure<HostOptions>(opts => opts.ShutdownTimeout = TimeSpan.FromSeconds(3));

        var listenPort = config.UiPort > 0 ? config.UiPort : 0;

        builder.WebHost.ConfigureKestrel(options =>
        {
            if (config.UiAllowRemoteConnections)
            {
                options.ListenAnyIP(listenPort);
            }
            else
            {
                options.Listen(IPAddress.Parse("127.0.0.1"), listenPort);
            }
        });

        var app = builder.Build();

        // Configure the HTTP request pipeline.
        if (!app.Environment.IsDevelopment())
        {
            app.UseExceptionHandler("/Error", createScopeForErrors: true);
            // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
            app.UseHsts();
        }

        app.UseHttpsRedirection();

        app.UseStaticFiles();
        app.UseAntiforgery();

        app.MapRazorComponents<Components.App>()
            .AddInteractiveServerRenderMode();

        CurrentApplication = app;
        Port = config.UiPort;
        AllowRemoteConnections = config.UiAllowRemoteConnections;

        await app.RunAsync();
    }

    public static async Task Stop()
    {
        if (CurrentApplication != null)
        {
            await CurrentApplication.StopAsync();
            CurrentApplication = null;
        }
    }

    public static string GetCurrentUrl()
    {
        var port = GetCurrentPort();

        return $"http://localhost:{port}";
    }

    public static int GetCurrentPort()
    {
        if (CurrentApplication == null)
        {
            return -1;
        }

        var server = CurrentApplication.Services.GetService<IServer>();
        var addressFeature = server?.Features.Get<IServerAddressesFeature>();

        if (addressFeature == null)
        {
            return -1;
        }

        foreach (var address in addressFeature.Addresses)
        {
            return int.Parse(address.Split(':').Last());
        }

        return -1;
    }

}
