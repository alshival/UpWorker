using CommunityToolkit.WinUI.UI.Controls;

using Microsoft.UI.Xaml.Controls;

using UpWorker.ViewModels;

namespace UpWorker.Views;

public sealed partial class FeedPage : Page
{
    public FeedViewModel ViewModel
    {
        get;
    }

    public FeedPage()
    {
        ViewModel = App.GetService<FeedViewModel>();
        InitializeComponent();
    }

    private void OnViewStateChanged(object sender, ListDetailsViewState e)
    {
        if (e == ListDetailsViewState.Both)
        {
            ViewModel.EnsureItemSelected();
        }
    }
}
