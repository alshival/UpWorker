using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Controls;
using UpWorker.Core.Services;
using UpWorker.Core.Helpers;
using UpWorker.ViewModels;
namespace UpWorker.Views;
using UpWorker.Core.Models;
using static UpWorker.Core.Services.SQLiteDataAccess;
using System.Collections.ObjectModel;

public sealed partial class MainPage : Page
{
    
    public MainViewModel ViewModel
    {
        get;
    }

    public MainPage()
    {
        ViewModel = App.GetService<MainViewModel>();
        InitializeComponent();
        
    }
}
