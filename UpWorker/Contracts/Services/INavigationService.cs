using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Navigation;

namespace UpWorker.Contracts.Services;

public interface INavigationService
{
    event NavigatedEventHandler Navigated;

    bool CanGoBack
    {
        get;
    }

    Frame? Frame
    {
        get; set;
    }

    bool NavigateTo(string pageKey, object? parameter = null, bool clearNavigation = false);

    bool GoBack();

    public bool NavigateToWebView(string pageKey, Uri uri)
    {
        // This assumes your ViewModel or Page can accept a Uri directly.
        return NavigateTo(pageKey, uri);
    }

}
