using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using UpWorker.Contracts.Services;
using UpWorker.Models;
using UpWorker.ViewModels;

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

    private void HyperlinkButton_Click(object sender, RoutedEventArgs e)
    {
        if (sender is HyperlinkButton hyperlinkButton && hyperlinkButton.Content is string urlString)
        {
            var navigationService = App.GetService<INavigationService>();
            if (navigationService != null)
            {
                navigationService.NavigateToWebView(typeof(WebViewViewModel).FullName, new Uri(urlString));
            }
        }
    }


}
