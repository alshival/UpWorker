using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using UpWorker.Contracts.Services;
using UpWorker.Models;
using UpWorker.ViewModels;
using Windows.System;

namespace UpWorker.Views;

public sealed partial class FeedControl : UserControl
{
    public Job? FeedItem
    {
        get => GetValue(FeedItemProperty) as Job;
        set => SetValue(FeedItemProperty, value);
    }

    public static readonly DependencyProperty FeedItemProperty = DependencyProperty.Register("FeedItem", typeof(Job), typeof(FeedControl), new PropertyMetadata(null, OnFeedItemPropertyChanged));

    public FeedControl()
    {
        InitializeComponent();
    }

    private static void OnFeedItemPropertyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is FeedControl control)
        {
            control.FeedForegroundElement.ChangeView(0, 0, 1);
        }
    }

    private async void HyperlinkButton_Click(object sender, RoutedEventArgs e)
    {
        if (sender is HyperlinkButton hyperlinkButton && hyperlinkButton.Content is string urlString)
        {
            //// For now, we are not using the webview because of a bug: https://github.com/microsoft/microsoft-ui-xaml/issues/9566
            /// Leaving the code here until it is patched. 
            
            //var navigationService = App.GetService<INavigationService>();
            //if (Uri.TryCreate(urlString, UriKind.Absolute, out var uri))
            //{
            //    _ = navigationService.NavigateToWebView(typeof(WebViewViewModel).FullName, uri);
            //}
            //else
            //{
            //    // Handle the error, perhaps log it or show a user-friendly message
            //}
            if (Uri.TryCreate(urlString, UriKind.Absolute, out var uri))
            {
                bool success = await Launcher.LaunchUriAsync(uri);
            }
        }
    }


}
