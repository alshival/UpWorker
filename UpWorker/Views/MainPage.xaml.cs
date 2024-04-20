using Microsoft.UI.Xaml.Controls;
using UpWorker.ViewModels;
namespace UpWorker.Views;
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
