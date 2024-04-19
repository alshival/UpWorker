using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Controls;
using UpWorker.Core.Services;
using UpWorker.ViewModels;
using static UpWorker.Core.Services.SQLiteDataAccess;
using System.Collections.ObjectModel;
using UpWorker.Core.Models;
using UpWorker.Core.Helpers;
using UpWorker.Notifications;
using UpWorker.Contracts.Services;
using UpWorker.Views;
using System.Data.SQLite;
using Microsoft.UI.Xaml.Navigation;

namespace UpWorker.Views;

// TODO: Set the URL for your privacy policy by updating SettingsPage_PrivacyTermsLink.NavigateUri in Resources.resw.
public sealed partial class SettingsPage : Page
{
    private IAppNotificationService appNotificationService;
    public ObservableCollection<UrlEntry> UrlEntries { get; set; } = new ObservableCollection<UrlEntry>();
    public SettingsViewModel ViewModel
    {
        get;
    }

    public SettingsPage()
    {
        ViewModel = App.GetService<SettingsViewModel>();
        InitializeComponent();
        urlListView.ItemsSource = UrlEntries;
        LoadUrls();
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
                List<JobListing> notifydata;
                using (var conn = SQLiteDataAccess.GetConnection())
                {
                    SQLiteDataAccess.InsertSearchUrl(conn, entryName, url);
                    statusTextBlock.Text = "Entry Saved Successfully!";
                    statusTextBlock.Foreground = new SolidColorBrush(Microsoft.UI.Colors.Green);
                    ReloadUrls();
                    urlInput.Text = "";
                    entryNameInput.Text = "";
                    await InitialFetchAndProcessRSS(url);

                    notifydata = SQLiteDataAccess.GetUnnotifiedJobs(conn);
                }
                // Send a notification
                foreach (var job in notifydata)
                {
                    appNotificationService.Show(job.notificationPayload);
                    using (var conn = SQLiteDataAccess.GetConnection())
                    {
                        SQLiteDataAccess.MarkJobAsNotified(conn, job.Title);
                    }
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

        ((App)Application.Current).RefreshFeed();
    }

    private void ReloadUrls()
    {
        UrlEntries.Clear();  // Clear the existing list
        LoadUrls();          // Reload the list from the database
    }

    private void LoadUrls()
    {
        using (var conn = SQLiteDataAccess.GetConnection())
        {
            var urls = SQLiteDataAccess.GetSearchUrls(conn);
            foreach (var url in urls)
            {
                url.DeleteCommand = new RelayCommand(param => DeleteUrl((int)param));
                UrlEntries.Add(url);
            }
        }
    }

    public async Task InitialFetchAndProcessRSS(string url)
    {
        var feed = await RssParser.FetchRssDataAsync(url);
        using (var conn = SQLiteDataAccess.GetConnection())
        {
            foreach (var item in feed.Items)
            {
                var jobListing = RssParser.FromRssItem(item);
                if (jobListing != null)
                {
                    // SQLiteDataAccess.InsertJobListingAsNotified(conn, jobListing);
                    SQLiteDataAccess.InsertJobListing(conn, jobListing);
                }
            }
        }
    }

    public void DeleteUrl(int id)
    {
        using (var conn = SQLiteDataAccess.GetConnection())
        {
            SQLiteDataAccess.DeleteSearchUrl(conn, id); // Implement this method
            var item = UrlEntries.FirstOrDefault(x => x.Id == id);
            if (item != null)
                UrlEntries.Remove(item);
        }
        statusTextBlock.Text = "Entry Deleted Successfully!";
        statusTextBlock.Foreground = new SolidColorBrush(Microsoft.UI.Colors.Green);
    }

    private void RefreshRateComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (refreshRateComboBox.SelectedItem is ComboBoxItem selectedRefreshRate)
        {
            var refreshRate = selectedRefreshRate.Tag.ToString();
            using (var conn = SQLiteDataAccess.GetConnection())
            {
                // SQL command to update the refresh rate setting in the app_settings table
                string sql = "UPDATE app_settings SET option = @Option WHERE setting = 'RefreshRate'";

                using (var cmd = new SQLiteCommand(sql, conn))
                {
                    // Use parameterized queries to prevent SQL injection
                    cmd.Parameters.AddWithValue("@Option", refreshRate);

                    // Execute the command
                    cmd.ExecuteNonQuery();
                }
            }
            Console.WriteLine("Refresh rate setting updated to: " + refreshRate);
        }
    }

    private void NotificationTimeFrameComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (notificationTimeFrameComboBox.SelectedItem is ComboBoxItem selectedTimeFrame)
        {
            var timeFrame = selectedTimeFrame.Tag.ToString();
            using (var conn = SQLiteDataAccess.GetConnection())
            {
                // SQL command to update the refresh rate setting in the app_settings table
                string sql = "UPDATE app_settings SET option = @Option WHERE setting = 'NotificationTimeFrame'";

                using (var cmd = new SQLiteCommand(sql, conn))
                {
                    // Use parameterized queries to prevent SQL injection
                    cmd.Parameters.AddWithValue("@Option", timeFrame);

                    // Execute the command
                    cmd.ExecuteNonQuery();
                }
            }
            Console.WriteLine("Refresh rate setting updated to: " + timeFrame);
        }
    }

    private void clearDataTimeFrameComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (clearDataTimeFrameComboBox.SelectedItem is ComboBoxItem selectedTimeFrame)
        {
            var timeFrame = selectedTimeFrame.Tag.ToString();
            using (var conn = SQLiteDataAccess.GetConnection())
            {
                string sql;
                if (timeFrame == "None")
                {
                    // Disable the setting if the time frame is "None"
                    sql = "UPDATE app_settings SET enabled = 0 WHERE setting = 'ClearDataTimeFrame'";
                }
                else
                {
                    // Update the option and enable the setting
                    sql = "UPDATE app_settings SET option = @Option, enabled = 1 WHERE setting = 'ClearDataTimeFrame'";
                }

                using (var cmd = new SQLiteCommand(sql, conn))
                {
                    // Add the parameter for the option if not "None"
                    if (timeFrame != "None")
                    {
                        cmd.Parameters.AddWithValue("@Option", timeFrame);
                    }

                    // Execute the command
                    cmd.ExecuteNonQuery();
                }
            }
            Console.WriteLine("Data purge updated to: " + timeFrame);
        }
    }


    protected override async void OnNavigatedTo(NavigationEventArgs e)
    {
        base.OnNavigatedTo(e);
        await InitializeSettingsAsync();
    }

    private async Task InitializeSettingsAsync()
    {
        // Fetch refresh rate and notification time frame settings from the database
        string refreshRateSetting = await SQLiteDataAccess.GetSettingValueAsync("RefreshRate");
        string notificationTimeFrameSetting = await SQLiteDataAccess.GetSettingValueAsync("NotificationTimeFrame");
        string deleteTimeFrameSetting = await SQLiteDataAccess.GetSettingValueAsync("ClearDataTimeFrame");

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


}
