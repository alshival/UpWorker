﻿using System.Collections.ObjectModel;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Navigation;
using UpWorker.Contracts.Services;
using UpWorker.Helpers;
using UpWorker.Models;
using UpWorker.Services;
using UpWorker.ViewModels;

namespace UpWorker.Views;

// TODO: Set the URL for your privacy policy by updating SettingsPage_PrivacyTermsLink.NavigateUri in Resources.resw.
public sealed partial class SettingsPage : Page
{
    private IAppNotificationService appNotificationService;
    public ObservableCollection<RssURL> UrlEntries { get; set; } = new ObservableCollection<RssURL>();
    public SettingsViewModel ViewModel
    {
        get;
    }

    public SettingsPage()
    {
        ViewModel = App.GetService<SettingsViewModel>();
        InitializeComponent();
        LoadUrls();
        urlListView.ItemsSource = UrlEntries;
        appNotificationService = App.GetService<IAppNotificationService>();
    }

    private async void submitButton_Click(object sender, RoutedEventArgs e)
    {
        string url = urlInput.Text;
        string entryName = entryNameInput.Text;
        if (!string.IsNullOrEmpty(url) && !string.IsNullOrEmpty(entryName))
        {
            try
            {
                List<Job> notifydata;
                DataAccess.AddRssUrl(new RssURL { Name = entryName, Url = url, Enabled = true });
                statusTextBlock.Text = "Entry Saved Successfully!";
                statusTextBlock.Foreground = new SolidColorBrush(Microsoft.UI.Colors.Green);
                ReloadUrls();
                urlInput.Text = "";
                entryNameInput.Text = "";
                RssParser rssParser = new RssParser();
                await rssParser.FetchAndProcessRSS(url);
                notifydata = DataAccess.GetUnnotifiedJobs();
                // Send a notification
                foreach (var job in notifydata)
                {
                    appNotificationService.Show(job.notificationPayload);
                    DataAccess.MarkJobAsNotified(job);
                }
            }
            catch (Exception ex)
            {
                statusTextBlock.Text = $"Error: {ex.Message}";
            }
        }
        else
        {
            statusTextBlock.Text = "Please fill in all fields.";
        }
    }

    private void ReloadUrls()
    {
        UrlEntries.Clear();  // Clear the existing list
        LoadUrls();          // Reload the list from the database
    }

    private void LoadUrls()
    {
        UrlEntries.Clear();
        var urls = DataAccess.GetRssUrls();
        foreach (var url in urls)
        {
            url.DeleteCommand = new RelayCommand(param => DeleteUrl((int)param));
            UrlEntries.Add(url);
        }
    }

    public async Task InitialFetchAndProcessRSS(string url)
    {
        var feed = await RssParser.FetchRssDataAsync(url);
        foreach (var item in feed.Items)
        {
            var jobListing = RssParser.FromRssItem(item);
            if (jobListing != null)
            {
                jobListing.Url = url;
                DataAccess.InsertJobListing(jobListing);
            }
        }
    }

    public void DeleteUrl(int id)
    {
        var item = UrlEntries.FirstOrDefault(x => x.Id == id);
        if (item != null)
        {
            DataAccess.DeleteRssUrl(item.Id);
            UrlEntries.Remove(item);
        }
        statusTextBlock.Text = "Entry Deleted Successfully!";
        statusTextBlock.Foreground = new SolidColorBrush(Microsoft.UI.Colors.Green);
        ReloadUrls();
    }

    private void RefreshRateComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (refreshRateComboBox.SelectedItem is ComboBoxItem selectedRefreshRate)
        {
            var refreshRate = selectedRefreshRate.Tag.ToString();
            DataAccess.SetSetting("RefreshRate", new Setting(refreshRate, true));
            Console.WriteLine("Refresh rate setting updated to: " + refreshRate);
        }
        App.RestartAppTimer();
    }

    private void NotificationTimeFrameComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (notificationTimeFrameComboBox.SelectedItem is ComboBoxItem selectedTimeFrame)
        {
            var timeFrame = selectedTimeFrame.Tag.ToString();
            DataAccess.SetSetting("NotificationTimeFrame", new Setting(timeFrame, true));
            Console.WriteLine("Refresh rate setting updated to: " + timeFrame);
        }
    }

    private void clearDataTimeFrameComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (clearDataTimeFrameComboBox.SelectedItem is ComboBoxItem selectedTimeFrame)
        {
            var timeFrame = selectedTimeFrame.Tag.ToString();

            if (timeFrame == "None")
            {
                // Disable the setting if the time frame is "None"
                DataAccess.SetSetting("ClearDataTimeFrame", new Setting(timeFrame, false));
            }
            else
            {
                // Update the option and enable the setting
                DataAccess.SetSetting("ClearDataTimeFrame", new Setting(timeFrame, true));
            }

            Console.WriteLine("Data purge updated to: " + timeFrame);
        }
    }


    protected override async void OnNavigatedTo(NavigationEventArgs e)
    {
        base.OnNavigatedTo(e);
        InitializeSettings();
    }

    private void InitializeSettings()
    {
        // Fetch refresh rate and notification time frame settings from the database

        var refreshRateSetting = DataAccess.GetSetting("RefreshRate").Option;
        var notificationTimeFrameSetting = DataAccess.GetSetting("NotificationTimeFrame").Option;
        var deleteTimeFrameSetting = DataAccess.GetSetting("ClearDataTimeFrame").Option;

        // Set the ComboBoxes to reflect these settings
        SetComboBoxSelection(refreshRateComboBox, refreshRateSetting);
        SetComboBoxSelection(notificationTimeFrameComboBox, notificationTimeFrameSetting);
        SetComboBoxSelection(clearDataTimeFrameComboBox, deleteTimeFrameSetting);
    }

    private void SetComboBoxSelection(ComboBox comboBox, string settingValue)
    {
        foreach (ComboBoxItem item in comboBox.Items)
        {
            if ((string)item.Tag == settingValue)
            {
                comboBox.SelectedItem = item;
                break;
            }
        }
    }

    private void visit_our_site(object sender, RoutedEventArgs e)
    {
        if (sender is HyperlinkButton hyperlinkButton && hyperlinkButton.Content is string urlString)
        {

            var navigationService = App.GetService<INavigationService>();
            if (Uri.TryCreate(urlString, UriKind.Absolute, out var uri))
            {
                _ = navigationService.NavigateToWebView(typeof(WebViewViewModel).FullName, uri);
            }
            else
            {
                // Handle the error, perhaps log it or show a user-friendly message
            }
        }
    }
}
