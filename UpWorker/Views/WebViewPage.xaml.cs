using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Navigation;
using UpWorker.ViewModels;

namespace UpWorker.Views;

// To learn more about WebView2, see https://docs.microsoft.com/microsoft-edge/webview2/.
public sealed partial class WebViewPage : Page
{
    public WebViewViewModel ViewModel
    {
        get;
    }

    public WebViewPage()
    {
        ViewModel = App.GetService<WebViewViewModel>();
        InitializeComponent();
        ViewModel.WebViewService.Initialize(WebView);
    }
    protected override void OnNavigatedTo(NavigationEventArgs e)
    {
        base.OnNavigatedTo(e);

        // Check if a URL is passed and it's valid, else use default URL
        if (e.Parameter is Uri uri)
        {
            ViewModel.Source = uri;  // Set to the passed URL
        }
        else
        {
            ViewModel.Source = new Uri("https://upwork.com");  // Default URL
        }

        ViewModel.WebViewService.Initialize(WebView);
    }

}
