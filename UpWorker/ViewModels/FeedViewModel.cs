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
    }
    public async void LoadData()
    {
        var newItems = new List<Job>();
        using (var conn = DataAccess.GetConnection())
        {
            conn.Open();
            var cmd = new SqliteCommand(
                @"select 
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
            limit 25", conn);

            using (var reader = cmd.ExecuteReader())
            {
                while (reader.Read())
                {
                    var job = new Job
                    {
                        Title = reader["title"]?.ToString() ?? string.Empty,
                        Category = reader["category"]?.ToString() ?? string.Empty,
                        JobDescription = reader["job_description"]?.ToString() ?? string.Empty,
                        PostedOn = reader["posted_on"]?.ToString() ?? string.Empty,
                        Skills = reader["skills"]?.ToString()?.Split(new string[] { ", " }, StringSplitOptions.RemoveEmptyEntries).ToList() ?? new List<string>(),
                        LocationRequirement = reader["location_requirement"]?.ToString() ?? string.Empty,
                        Country = reader["country"]?.ToString() ?? string.Empty,
                        Payment = reader["payment"]?.ToString() ?? string.Empty,
                        Link = reader["link"]?.ToString() ?? string.Empty,
                        Url = reader["url"]?.ToString() ?? string.Empty,
                        SymbolName = string.IsNullOrEmpty(reader["location_requirement"]?.ToString()) ? "World" : "Home",
                        SymbolCode = string.IsNullOrEmpty(reader["location_requirement"]?.ToString()) ? 57640 : 57615
                    };
                    newItems.Add(job);
                }
            }
        }
        if (SampleItems.Any())
        {
            UpdateSampleItems(newItems);
        }
        else
        {
            foreach (var newItem in newItems)
            {
                SampleItems.Add(newItem);
            }
        }
    }

    private void UpdateSampleItems(List<Job> newItems)
    {
        var existingTitles = new HashSet<string>(SampleItems.Select(x => x.Title));

        foreach (var newItem in newItems)
        {
            if (!existingTitles.Contains(newItem.Title))
            {
                SampleItems.Insert(0, newItem); // Prepend new item to the collection
                existingTitles.Add(newItem.Title); // Add to set for future checks
            }
        }
    }



    public void OnNavigatedFrom()
    {
        App.DataRefreshed -= OnDataRefreshed;  // Unsubscribe when the view model is not visible
    }

    public async void OnNavigatedTo(object parameter)
    {
        LoadData();
    }

    public void EnsureItemSelected()
    {
        if (Selected == null && SampleItems.Any()) // Check if there are any items before accessing
        {
            Selected = SampleItems.First();
        }
    }

}
