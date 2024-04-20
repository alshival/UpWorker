using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using UpWorker.Contracts.ViewModels;
using UpWorker.Models;
using UpWorker.Services;

namespace UpWorker.ViewModels;

public partial class FeedViewModel : ObservableRecipient, INavigationAware
{


    [ObservableProperty]
    private Job? selected;

    public ObservableCollection<Job> SampleItems { get; private set; } = new ObservableCollection<Job>();


    public async void OnNavigatedTo(object parameter)
    {
        SampleItems.Clear();
        // pull first few items from database
        using (var conn = DataAccess.GetConnection())
        {
            conn.Open();
            string query = @"
            select 
                *
            from Jobs
            order by posted_on desc
            limit 25
            ";
            using (var cmd = conn.CreateCommand())
            {
                cmd.CommandText = query;
                using (var reader = cmd.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        int ShowSymbolCode;
                        string ShowSymbolName;

                        if (reader["location_requirement"] != DBNull.Value && !string.IsNullOrEmpty(reader["location_requirement"].ToString()))
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
                            Title = reader["title"].ToString(),
                            Category = reader["category"].ToString(),
                            PostedOn = reader["posted_on"].ToString(),
                            Skills = reader["skills"].ToString().Split(", ").ToList(),
                            LocationRequirement = reader["location_requirement"].ToString(),
                            Country = reader["country"].ToString(),
                            Payment = reader["payment"].ToString(),
                            Link = reader["link"].ToString(),
                            Notified = Convert.ToBoolean(reader["notified"]),
                            SymbolName = ShowSymbolName,
                            SymbolCode = ShowSymbolCode
                        };
                        SampleItems.Add(job);
                    }
                }
            }
        }
    }

    public void OnNavigatedFrom()
    {
    }

    public void EnsureItemSelected()
    {
        if (Selected == null && SampleItems.Any()) // Check if there are any items before accessing
        {
            Selected = SampleItems.First();
        }
    }

}
