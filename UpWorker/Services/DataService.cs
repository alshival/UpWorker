using System.Windows.Input;
using Microsoft.Data.Sqlite;
using UpWorker.Models;
using Windows.Storage;

namespace UpWorker.Services;
public class DataAccess
{

    //#####################################################################################
    // Settings Data 
    //#####################################################################################

    static Windows.Storage.ApplicationDataContainer localSettings = Windows.Storage.ApplicationData.Current.LocalSettings;
    static Windows.Storage.StorageFolder localFolder = Windows.Storage.ApplicationData.Current.LocalFolder;

    public static Dictionary<string, Setting> Settings
    {
        get; private set;
    }

    public static void InitializeSettings()
    {
        // Default values
        Settings = new Dictionary<string, Setting>()
        {
            {"RefreshRate", new Setting("1", true)},
            {"NotificationTimeFrame", new Setting("-1 hours", true)},
            {"ClearDataTimeFrame", new Setting("-3 days", true)}
        };

        // Ensure the settings are saved in local settings
        foreach (var setting in Settings)
        {
            if (localSettings.Values[setting.Key] == null)
            {
                localSettings.Values[setting.Key] = setting.Value.Option;
                // Optionally handle other aspects of the Setting object if needed, such as storing its 'IsEnabled' state
            }
            else
            {
                // Optionally update the Settings dictionary with actual stored values
                setting.Value.Option = localSettings.Values[setting.Key].ToString();
            }
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

        // Save the changes to localSettings
        if (localSettings.Values.ContainsKey(key))
        {
            // If the setting already exists, update it
            localSettings.Values[key] = setting.Option;
        }
        else
        {
            // If the setting does not exist, create a new entry
            localSettings.Values.Add(key, setting.Option);
        }

        // Optionally, save other aspects like IsEnabled if necessary
        // For example, saving it as a separate key-value pair
        localSettings.Values[key + "Enabled"] = setting.Enabled;
    }


    //#####################################################################################
    // SQL Data 
    //#####################################################################################
    static string dbpath = Path.Combine(ApplicationData.Current.LocalFolder.Path, "UpworkER.db");
    private static string connectionstring = $"Data Source={dbpath};";

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
                        job_description TEXT,
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

            var cmd = new SqliteCommand("SELECT id, name, url FROM RssURLs WHERE enabled = 1", connection);

            SqliteDataReader query = cmd.ExecuteReader();

            while (query.Read())
            {
                rssUrls.Add(new RssURL
                {
                    Id = query.GetInt32(0),
                    Name = query.GetString(1),
                    Url = query.GetString(2)
                });
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
                (title, category, job_description, posted_on, skills, location_requirement, country, payment, link, notified, url, insert_datetime)
                VALUES
                (@title, @category, @job_description, @posted_on, @skills, @location, @country, @payment, @link, @notified, @url, @insert_datetime)
                "; ;

                // Adding parameters and handling nulls
                cmd.Parameters.AddWithValue("@title", job.Title ?? string.Empty); // Assuming empty string is acceptable for 'title'
                cmd.Parameters.AddWithValue("@category", job.Category ?? string.Empty); // Ditto for 'category'
                cmd.Parameters.AddWithValue("@job_description", job.JobDescription ?? string.Empty);
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
        catch
        {
            Console.WriteLine("Skipping duplicate entry: " + job.Title);
        }
    }


    public static List<Job> GetUnnotifiedJobs()
    {
        var jobs = new List<Job>();
        var notificationTimeFrame = DataAccess.GetSetting("NotificationTimeFrame").Option;

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
                    Title = query["title"].ToString() ?? string.Empty,
                    Category = query["category"].ToString() ?? string.Empty,
                    PostedOn = query["posted_on"].ToString() ?? string.Empty,
                    Skills = query["skills"]?.ToString()?.Split(new string[] { ", " }, StringSplitOptions.RemoveEmptyEntries).ToList() ?? new List<string>(),
                    LocationRequirement = query["location_requirement"].ToString() ?? string.Empty,
                    Country = query["country"].ToString() ?? string.Empty,
                    Payment = query["payment"].ToString() ?? string.Empty,
                    Link = query["link"].ToString() ?? string.Empty,
                    Notified = Convert.ToBoolean(query["notified"]),
                    Url = query["url"].ToString() ?? string.Empty,
                    InsertDateTime = query["insert_datetime"].ToString() ?? string.Empty
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
