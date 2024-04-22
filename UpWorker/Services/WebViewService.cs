using System.Diagnostics.CodeAnalysis;

using Microsoft.UI.Xaml.Controls;
using Microsoft.Web.WebView2.Core;

using UpWorker.Contracts.Services;

namespace UpWorker.Services;

public class WebViewService : IWebViewService
{
    private WebView2? _webView;

    public Uri? Source => _webView?.Source;

    [MemberNotNullWhen(true, nameof(_webView))]
    public bool CanGoBack => _webView != null && _webView.CanGoBack;

    [MemberNotNullWhen(true, nameof(_webView))]
    public bool CanGoForward => _webView != null && _webView.CanGoForward;

    public event EventHandler<CoreWebView2WebErrorStatus>? NavigationCompleted;

    public WebViewService()
    {
    }

    [MemberNotNull(nameof(_webView))]
    public void Initialize(WebView2 webView)
    {
        _webView = webView;
        _webView.NavigationCompleted += OnWebViewNavigationCompleted;
    }


    public void GoBack() => _webView?.GoBack();

    public void GoForward() => _webView?.GoForward();

    public void Reload() => _webView?.Reload();

    public void UnregisterEvents()
    {
        if (_webView != null)
        {
            _webView.NavigationCompleted -= OnWebViewNavigationCompleted;
        }
    }

    public void Cleanup()
    {
        _webView.Close();
    }

    //private void OnWebViewNavigationCompleted(WebView2 sender, CoreWebView2NavigationCompletedEventArgs args) => NavigationCompleted?.Invoke(this, args.WebErrorStatus);
    private async void OnWebViewNavigationCompleted(WebView2 sender, CoreWebView2NavigationCompletedEventArgs args)
    {
        if (args.IsSuccess)
        {
            // Navigation succeeded, invoke the completed event with no error status.
            NavigationCompleted?.Invoke(this, CoreWebView2WebErrorStatus.Unknown);
            sender.CoreWebView2.Profile.PreferredTrackingPreventionLevel = CoreWebView2TrackingPreventionLevel.Basic;
            sender.CoreWebView2.Settings.IsScriptEnabled = true;
            var cookieManager = sender.CoreWebView2.CookieManager;
            var cookies = await cookieManager.GetCookiesAsync("");
            //var applyCookies = await cookieManager.GetCookiesAsync("apply/");
            Console.WriteLine("Navigation to " + sender.Source + " completed successfully.");
        }
        else
        {
            // Navigation failed, log the error and invoke the completed event with the error status.
            Console.WriteLine("Failed to navigate to " + sender.Source + ". Error: " + args.WebErrorStatus);
            NavigationCompleted?.Invoke(this, args.WebErrorStatus);
        }
        await sender.EnsureCoreWebView2Async(null);
    }

}
