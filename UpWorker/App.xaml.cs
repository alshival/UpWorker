using System.Diagnostics;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.UI.Xaml;
using UpWorker.Activation;
using UpWorker.Contracts.Services;
using UpWorker.Core.Contracts.Services;
using UpWorker.Core.Services;
using UpWorker.Helpers;
using UpWorker.Models;
using UpWorker.Notifications;
using UpWorker.Services;
using UpWorker.ViewModels;
using UpWorker.Views;

namespace UpWorker;

// To learn more about WinUI 3, see https://docs.microsoft.com/windows/apps/winui/winui3/.
public partial class App : Application
{
    // The .NET Generic Host provides dependency injection, configuration, logging, and other services.
    // https://docs.microsoft.com/dotnet/core/extensions/generic-host
    // https://docs.microsoft.com/dotnet/core/extensions/dependency-injection
    // https://docs.microsoft.com/dotnet/core/extensions/configuration
    // https://docs.microsoft.com/dotnet/core/extensions/logging
    private DispatcherTimer timer;
    private IAppNotificationService appNotificationService;
    public IHost Host
    {
        get;
    }

    public static T GetService<T>()
        where T : class
    {
        if ((App.Current as App)!.Host.Services.GetService(typeof(T)) is not T service)
        {
            throw new ArgumentException($"{typeof(T)} needs to be registered in ConfigureServices within App.xaml.cs.");
        }

        return service;
    }

    public static WindowEx MainWindow { get; } = new MainWindow();

    public static UIElement? AppTitlebar
    {
        get; set;
    }

    public App()
    {
        InitializeComponent();

        Host = Microsoft.Extensions.Hosting.Host.
        CreateDefaultBuilder().
        UseContentRoot(AppContext.BaseDirectory).
        ConfigureServices((context, services) =>
        {
            // Default Activation Handler
            services.AddTransient<ActivationHandler<LaunchActivatedEventArgs>, DefaultActivationHandler>();

            // Other Activation Handlers
            services.AddTransient<IActivationHandler, AppNotificationActivationHandler>();

            // Services
            services.AddSingleton<IAppNotificationService, AppNotificationService>();
            services.AddSingleton<ILocalSettingsService, LocalSettingsService>();
            services.AddSingleton<IThemeSelectorService, ThemeSelectorService>();
            services.AddTransient<IWebViewService, WebViewService>();
            services.AddTransient<INavigationViewService, NavigationViewService>();

            services.AddSingleton<IActivationService, ActivationService>();
            services.AddSingleton<IPageService, PageService>();
            services.AddSingleton<INavigationService, NavigationService>();

            // Core Services
            services.AddSingleton<ISampleDataService, SampleDataService>();
            services.AddSingleton<IFileService, FileService>();

            // Views and ViewModels
            services.AddTransient<SettingsViewModel>();
            services.AddTransient<SettingsPage>();
            //services.AddTransient<ListDetailsViewModel>();
            //services.AddTransient<ListDetailsPage>();
            services.AddTransient<FeedViewModel>();
            services.AddTransient<FeedPage>();
            services.AddTransient<WebViewViewModel>();
            services.AddTransient<WebViewPage>();
            services.AddTransient<MainViewModel>();
            services.AddTransient<MainPage>();
            services.AddTransient<ShellPage>();
            services.AddTransient<ShellViewModel>();

            // Configuration
            services.Configure<LocalSettingsOptions>(context.Configuration.GetSection(nameof(LocalSettingsOptions)));
        }).
        Build();

        App.GetService<IAppNotificationService>().Initialize();

        UnhandledException += App_UnhandledException;
    }

    private void SetupTimer()
    {
        var refreshRate = GetRefreshRate();
        timer = new DispatcherTimer();
        timer.Interval = TimeSpan.FromMinutes(refreshRate);
        timer.Tick += Timer_Tick;
        timer.Start();
    }

    private double GetRefreshRate()
    {
        var settingValue = DataAccess.GetSetting("RefreshRate").Option;
        if (double.TryParse(settingValue, out double minutes))
        {
            return minutes;
        }
        return 1; // Default value if parsing fails or setting is unavailable
    }
    public void RestartTimer()
    {
        timer.Stop();  // Stop the current timer
        SetupTimer();  // Re-setup the timer with new interval
    }
    public static void RestartAppTimer()
    {
        var app = (App)Application.Current;
        app.RestartTimer();
    }

    private void Timer_Tick(object sender, object e)
    {
        // Your recurring task code here
        Debug.WriteLine("Timer ticked at " + DateTime.Now.ToString());
        DataAccess.DeleteOldJobs();
        RefreshFeed();
    }
    public async void RefreshFeed()
    {
        RssParser rssParser = new RssParser();
        //Refresh URLs
        List<Job> notifydata;
        using (var conn = DataAccess.GetConnection())
        {
            conn.Open();
            var refreshList = DataAccess.GetRssUrls();
            foreach (var url in refreshList)
            {
                await rssParser.FetchAndProcessRSS(url.Url);
            }
            conn.Close();
        }
        notifydata = DataAccess.GetUnnotifiedJobs();
        // Send a notification
        foreach (var job in notifydata)
        {
            appNotificationService.Show(job.notificationPayload);
            DataAccess.MarkJobAsNotified(job);
        }
    }

    private void App_UnhandledException(object sender, Microsoft.UI.Xaml.UnhandledExceptionEventArgs e)
    {
        // TODO: Log and handle exceptions as appropriate.
        // https://docs.microsoft.com/windows/windows-app-sdk/api/winrt/microsoft.ui.xaml.application.unhandledexception.
    }

    protected async override void OnLaunched(LaunchActivatedEventArgs args)
    {

        base.OnLaunched(args);
        DataAccess.InitializeSettings();
        DataAccess.InitializeDatabase();
        //RefreshFeed();
        //SetupTimer();

        // Retrieve the notification service
        appNotificationService = App.GetService<IAppNotificationService>();
        await App.GetService<IActivationService>().ActivateAsync(args);

        var navigationService = App.GetService<INavigationService>();
        if (navigationService != null)
        {
            navigationService.NavigateToWebView(typeof(WebViewViewModel).FullName, new Uri("https://Upwork.com"));
        }

    }
}
