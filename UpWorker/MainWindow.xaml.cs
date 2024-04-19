using UpWorker.Helpers;
using Windows.UI.ViewManagement;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml;
using System.Collections.ObjectModel;
using UpWorker.Core.Services;
using System.Security.Policy;
using UpWorker.Core.Helpers;
using System.Diagnostics;
using UpWorker.Contracts.Services;
using UpWorker.Core.Models;
using Microsoft.UI.Xaml.Controls;
using UpWorker.Views;
using UpWorker.Services;
using Microsoft.Xaml.Interactions.Core;
using System.Data.SqlTypes;
using UpWorker.ViewModels;

namespace UpWorker;

public sealed partial class MainWindow : WindowEx
{
    private Microsoft.UI.Dispatching.DispatcherQueue dispatcherQueue;

    private UISettings settings;
    // Timer for background tasks and notifications


    public MainWindow()
    {
        InitializeComponent();
        AppWindow.SetIcon(Path.Combine(AppContext.BaseDirectory, "Assets/logo.png"));
        Content = null;

        // Theme change code picked from https://github.com/microsoft/WinUI-Gallery/pull/1239
        dispatcherQueue = Microsoft.UI.Dispatching.DispatcherQueue.GetForCurrentThread();
        settings = new UISettings();
        settings.ColorValuesChanged += Settings_ColorValuesChanged; // cannot use FrameworkElement.ActualThemeChanged event
        
    }


    // this handles updating the caption button colors correctly when indows system theme is changed
    // while the app is open
    private void Settings_ColorValuesChanged(UISettings sender, object args)
    {
        // This calls comes off-thread, hence we will need to dispatch it to current app's thread
        dispatcherQueue.TryEnqueue(() =>
        {
            TitleBarHelper.ApplySystemThemeToCaptionButtons();
        });
    }

}
