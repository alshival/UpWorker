using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using Microsoft.Data.Sqlite;
using UpWorker.Contracts.ViewModels;
using UpWorker.Models;
using UpWorker.Services;

namespace UpWorker.ViewModels;

public partial class FeedViewModel : ObservableRecipient, INavigationAware
{


    [ObservableProperty]
    private Job? selected;

    public ObservableCollection<Job> SampleItems { get; private set; } = new ObservableCollection<Job>();
    public FeedViewModel()
    {
        App.DataRefreshed += OnDataRefreshed;
    }
    private void OnDataRefreshed(object sender, EventArgs e)
    {
        LoadData();
        EnsureItemSelected();
    }
    public async void LoadData()
    {
        // Existing code to load data from the database.
        OnNavigatedTo(null);
    }
    public void OnNavigatedFrom()
    {
        App.DataRefreshed -= OnDataRefreshed;  // Unsubscribe when the view model is not visible
    }

    public async void OnNavigatedTo(object parameter)
    {
        SampleItems.Clear();
        // pull first few items from database
        using (var conn = DataAccess.GetConnection())
        {
            conn.Open();
            string sql = @"
            select 
                title,
                category, 
                job_description,
                posted_on,
                skills,
                location_requirement,
                country,
                payment,
                link,
                url
            from Jobs
            order by posted_on desc
            limit 25
            ";

            var cmd = new SqliteCommand();
            cmd.Connection = conn;
            cmd.CommandText = sql;

            SqliteDataReader reader = cmd.ExecuteReader();

            while (reader.Read())
            {
                int ShowSymbolCode;
                string ShowSymbolName;

                if (reader["location_requirement"] != null && !string.IsNullOrEmpty(reader["location_requirement"].ToString()))
                {
                    ShowSymbolCode = 57615;
                    ShowSymbolName = "Home";
                }
                else
                {
                    ShowSymbolCode = 57640;
                    ShowSymbolName = "World";
                }

                var job = new Job
                {

                    Title = reader["title"].ToString() ?? string.Empty,
                    Category = reader["category"].ToString() ?? string.Empty,
                    JobDescription = reader["job_description"].ToString() ?? string.Empty,
                    PostedOn = reader["posted_on"].ToString() ?? string.Empty,
                    Skills = reader["skills"]?.ToString()?.Split(new string[] { ", " }, StringSplitOptions.RemoveEmptyEntries).ToList() ?? new List<string>(),
                    LocationRequirement = reader["location_requirement"].ToString() ?? string.Empty,
                    Country = reader["country"]?.ToString() ?? string.Empty,
                    Payment = reader["payment"]?.ToString() ?? string.Empty,
                    Link = reader["link"]?.ToString() ?? string.Empty,
                    Url = reader["url"]?.ToString() ?? string.Empty,
                    SymbolName = ShowSymbolName,
                    SymbolCode = ShowSymbolCode

                };
                SampleItems.Add(job);
            }
        }
    }

    public void EnsureItemSelected()
    {
        if (Selected == null && SampleItems.Any()) // Check if there are any items before accessing
        {
            Selected = SampleItems.First();
        }
    }

}
