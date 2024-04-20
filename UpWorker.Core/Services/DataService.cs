﻿using System.Reflection;
using System.Windows.Input;
using Microsoft.Data.Sqlite;
using Newtonsoft.Json;
using UpWorker.Core.Models;

namespace UpWorker.Core.Services;

public class DataAccess
{
    private static readonly string executableLocation = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);

    //#####################################################################################
    // Settings Data 
    //#####################################################################################
    private static readonly string SettingsFileName = "settings.json"; // Path to the settings file
    private static readonly string SettingsFilePath = Path.Combine(executableLocation, SettingsFileName);

    public static Dictionary<string, Setting> Settings
    {
        get; private set;
    } = new Dictionary<string, Setting>()
    {
        {"RefreshRate", new Setting("1", true)},
        {"NotificationTimeFrame", new Setting("-1 hours", true)},
        {"ClearDataTimeFrame", new Setting("-3 days", true)}
    };

    public static void InitializeSettings()
    {
        // Check if the settings file exists
        if (File.Exists(SettingsFilePath))
        {
            // Read the settings from file
            var jsonSettings = File.ReadAllText(SettingsFilePath);
            Settings = JsonConvert.DeserializeObject<Dictionary<string, Setting>>(jsonSettings);
        }
        else
        {
            // If the file does not exist, serialize and save the default settings
            SaveSettings();
        }
    }

    public static Setting GetSetting(string key)
    {
        // Check if the setting exists
        if (Settings.ContainsKey(key))
        {
            return Settings[key];
        }
        return null; // or throw an exception or handle however you see fit
    }

    public static void SetSetting(string key, Setting setting)
    {
        // Update or add the setting
        Settings[key] = setting;

        // Save changes to file
        SaveSettings();
    }

    private static void SaveSettings()
    {
        var jsonSettings = JsonConvert.SerializeObject(Settings, Formatting.Indented); // Use Formatting.Indented for better readability
        File.WriteAllText(SettingsFilePath, jsonSettings);
    }

    //#####################################################################################
    // SQL Data 
    //#####################################################################################
    private static string databaseFile = "UpWorker.db"; // Path to the settings file
    private static string databaseFilePath = Path.Combine(executableLocation, databaseFile);
    private static string connectionstring = $"Data Source={databaseFilePath};";
    public static SqliteConnection GetConnection()
    {
        var conn = new SqliteConnection(connectionstring);
        return conn;
    }


    public static void InitializeDatabase()
    {
        // create database if it does not exist
        
        using (var conn = DataAccess.GetConnection())
        {
            conn.Open();
            using (var transaction = conn.BeginTransaction())
            {
                try
                {
                    // Create RssURLs table for storing feeds
                    var createRssTableCmd = conn.CreateCommand();
                    createRssTableCmd.Transaction = transaction;
                    createRssTableCmd.CommandText = @"
                    CREATE TABLE IF NOT EXISTS RssURLs (
                        id INTEGER PRIMARY KEY,
                        name TEXT NOT NULL,
                        url TEXT NOT NULL,
                        enabled BOOLEAN NOT NULL DEFAULT 1
                    );
                ";
                    createRssTableCmd.ExecuteNonQuery();

                    // Create Jobs table for storing Rss feed data
                    var createJobsTableCmd = conn.CreateCommand();
                    createJobsTableCmd.Transaction = transaction;
                    createJobsTableCmd.CommandText = @"
                    CREATE TABLE IF NOT EXISTS Jobs (
                        id INTEGER PRIMARY KEY,
                        title TEXT NULL,
                        category TEXT,
                        posted_on DATETIME,
                        skills TEXT,
                        location_requirement TEXT,
                        country TEXT,
                        payment TEXT,
                        link TEXT NULL,
                        notified BOOLEAN NULL DEFAULT 0,
                        url TEXT NULL,
                        insert_datetime DATETIME,
                        UNIQUE(title)
                    );
                ";
                    createJobsTableCmd.ExecuteNonQuery();

                    // Commit the transaction
                    transaction.Commit();
                }
                catch (Exception ex)
                {
                    Console.WriteLine("Failed to initialize database: " + ex.Message);
                    transaction.Rollback();
                    throw; // Optionally rethrow to handle this exception outside of this method
                }
            }
        }
    }

    public static void AddRssUrl(RssURL rssUrl)
    {
        using (var connection = DataAccess.GetConnection())
        {
            connection.Open();
            try
            {
                var cmd = new SqliteCommand();
                cmd.Connection = connection;
                cmd.CommandText = "INSERT INTO RssURLs (name, url, enabled) VALUES (@name, @url, @enabled)";
                cmd.Parameters.AddWithValue("@name", rssUrl.Name);
                cmd.Parameters.AddWithValue("@url", rssUrl.Url);
                cmd.Parameters.AddWithValue("@enabled", rssUrl.Enabled ? 1 : 0);

                cmd.ExecuteNonQuery();

            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error adding RSS URL: {ex.Message}");
                throw;  // Optionally rethrow to allow higher-level handlers to manage the exception
            }
        }
    }


    public static List<RssURL> GetRssUrls()
    {
        var rssUrls = new List<RssURL>();

        using (var connection = DataAccess.GetConnection())
        {
            connection.Open();

            var cmd = new SqliteCommand("SELECT Name, Url FROM RssURLs WHERE enabled = 1",connection);

            SqliteDataReader query = cmd.ExecuteReader();

            while (query.Read())
            {
                rssUrls.Add(new RssURL { Name = query.GetString(0), Url = query.GetString(1) });
            }
        }

        return rssUrls;
    }

    public static void DeleteRssUrl(int id)
    {
        using (var connection = DataAccess.GetConnection())
        {
            connection.Open();
            var cmd = new SqliteCommand();
            cmd.Connection = connection;

            cmd.CommandText = "DELETE FROM RssURLs where id = @id";
            cmd.Parameters.AddWithValue("@id", id);
            cmd.ExecuteNonQuery();
        }
    }



    public static void DeleteOldJobs()
    {
        var deleteSetting = DataAccess.GetSetting("ClearDataTimeFrame");
        if (deleteSetting.Enabled)
        {
            var timeFrame = deleteSetting.Option;
            using (var connection = DataAccess.GetConnection())
            {
                connection.Open();
                var cmd = new SqliteCommand();
                cmd.Connection = connection;

                cmd.CommandText = $@"
                DELETE FROM Jobs
                WHERE posted_on < datetime('now','{timeFrame}','localtime')
                ";
                
                cmd.ExecuteNonQuery();
            }

        }
    }

    public static void InsertJobListing(Job job)
    {
        try
        {
            using (var conn = DataAccess.GetConnection())
            {
                conn.Open();

                var insertDateTime = DateTime.Now.ToLocalTime().ToString("yyyy-MM-dd HH:mm:ss");

                var cmd = new SqliteCommand();
                cmd.Connection = conn;
                cmd.CommandText = @"
                INSERT INTO Jobs
                (title, category, posted_on, skills, location_requirement, country, payment, link, notified, url, insert_datetime)
                VALUES
                (@title, @category, @posted_on, @skills, @location, @country, @payment, @link, @notified, @url, @insert_datetime)
                "; ;

                // Adding parameters and handling nulls
                cmd.Parameters.AddWithValue("@title", job.Title ?? string.Empty); // Assuming empty string is acceptable for 'title'
                cmd.Parameters.AddWithValue("@category", job.Category ?? string.Empty); // Ditto for 'category'
                cmd.Parameters.AddWithValue("@posted_on", job.PostedOn ?? (object)DBNull.Value);
                cmd.Parameters.AddWithValue("@skills", job.Skills != null ? string.Join(", ", job.Skills) : (object)DBNull.Value);
                cmd.Parameters.AddWithValue("@location", job.LocationRequirement ?? (object)DBNull.Value);
                cmd.Parameters.AddWithValue("@country", job.Country ?? string.Empty);
                cmd.Parameters.AddWithValue("@payment", job.Payment ?? (object)DBNull.Value);
                cmd.Parameters.AddWithValue("@link", job.Link ?? string.Empty);
                cmd.Parameters.AddWithValue("@notified", job.Notified ? 1 : 0);
                cmd.Parameters.AddWithValue("@url", job.Url ?? string.Empty);
                cmd.Parameters.AddWithValue("@insert_datetime", insertDateTime);


                cmd.ExecuteNonQuery();
            }
        }
        catch (SqliteException)
        {
            Console.WriteLine("Skipping duplicate entry: " + job.Title);
        }
    }


    public static List<Job> GetUnnotifiedJobs()
    {
        var jobs = new List<Job>();
        var notificationTimeFrame =DataAccess.GetSetting("NotificationTimeFrame").Option;
       
        using (var conn = DataAccess.GetConnection())
        {
            conn.Open();

            var cmd = new SqliteCommand();
            cmd.Connection = conn;
            cmd.CommandText = $@"
            SELECT 
                    title, 
                    category,
                    skills, 
                    posted_on,
                    location_requirement, 
                    country, 
                    payment, 
                    link, 
                    notified,
                    url,
                    insert_datetime 
                FROM Jobs 
                WHERE notified = 0 
                and posted_on >= datetime('now','{notificationTimeFrame}', 'localtime') 
                order by posted_on desc limit 3";
            SqliteDataReader query = cmd.ExecuteReader();
            while (query.Read())
            {
                var job = new Job
                {
                    Title = query["title"].ToString(),
                    Category = query["category"].ToString(),
                    PostedOn = query["posted_on"].ToString(),
                    Skills = query["skills"].ToString().Split(", ").ToList(),
                    LocationRequirement = query["location_requirement"].ToString(),
                    Country = query["country"].ToString(),
                    Payment = query["payment"].ToString(),
                    Link = query["link"].ToString(),
                    Notified = Convert.ToBoolean(query["notified"]),
                    Url = query["url"].ToString(),
                    InsertDateTime = query["insert_datetime"].ToString()
                };

                jobs.Add(job);
            }
            
        }
        return jobs;
    }

    public static void MarkJobAsNotified(Job job)
    {

        using (var conn = DataAccess.GetConnection())
        {
            conn.Open();

            var cmd = new SqliteCommand();
            cmd.Connection = conn;
            cmd.CommandText = "UPDATE Jobs SET notified = 1 WHERE title = @title";
            cmd.Parameters.AddWithValue("@title", job.Title);
            cmd.ExecuteNonQuery();
        }
    }


    public class RelayCommand : ICommand
    {
        private Action<object> execute;
        private Func<object, bool> canExecute;

        public event EventHandler CanExecuteChanged;

        public RelayCommand(Action<object> execute, Func<object, bool> canExecute = null)
        {
            this.execute = execute;
            this.canExecute = canExecute;
        }

        public bool CanExecute(object parameter)
        {
            return canExecute == null || canExecute(parameter);
        }

        public void Execute(object parameter)
        {
            execute(parameter);
        }

        public void RaiseCanExecuteChanged()
        {
            CanExecuteChanged?.Invoke(this, EventArgs.Empty);
        }
    }


}
