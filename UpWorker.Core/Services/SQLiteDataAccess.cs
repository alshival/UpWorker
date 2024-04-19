using System;
using System.Collections.Generic;
using System.Data.SQLite;
using System.Linq;
using System.Windows.Input;
using UpWorker.Core.Models;

namespace UpWorker.Core.Services;

public class SQLiteDataAccess
{
    //private static string dbFolder = @"C:\Users\samue\source\repos\Upwork Job Listing Notifications";
    //stores in "C:\Users\samue\AppData\Local\VirtualStore\Windows\SysWOW64\rss_feed_jobs.db"
    private static string dbName = "rss_feed_jobs.db";
    //private static string dbPath = System.IO.Path.Combine(dbFolder, dbName);
    private static string connectionString = $"Data Source={dbName};Version=3;";

    public static void CreateDatabase()
    {
        using (var conn = new SQLiteConnection(connectionString))
        {
            conn.Open();
            // Create tables if they don't exist
            string sql = @"
        CREATE TABLE IF NOT EXISTS job_listings (
            id INTEGER PRIMARY KEY,
            title TEXT NOT NULL,
            category TEXT,
            posted_on DATETIME,
            skills TEXT,
            location_requirement TEXT,
            country TEXT,
            payment TEXT,
            link TEXT NOT NULL,
            notified BOOLEAN NOT NULL DEFAULT 0,
            search_url TEXT NOT NULL,
            insert_datetime DATETIME,
            UNIQUE(title)
        );
        CREATE TABLE IF NOT EXISTS search_urls (
            id INTEGER PRIMARY KEY,
            name TEXT NOT NULL,
            url TEXT NOT NULL,
            enabled BOOLEAN NOT NULL DEFAULT 1
        );
        CREATE TABLE IF NOT EXISTS app_settings (
            id INTEGER PRIMARY KEY,
            setting TEXT NOT NULL,
            option TEXT NOT NULL,
            enabled BOOLEAN NOT NULL DEFAULT 1
        );";

            using (var cmd = new SQLiteCommand(sql, conn))
            {
                cmd.ExecuteNonQuery();
            }

            // Check if the app_settings table is empty
            sql = "SELECT COUNT(*) FROM app_settings";
            using (var cmd = new SQLiteCommand(sql, conn))
            {
                int count = Convert.ToInt32(cmd.ExecuteScalar());
                if (count == 0)
                {
                    // Populate the app_settings table with default values
                    sql = @"
                INSERT INTO app_settings (setting, option, enabled) VALUES
                ('RefreshRate', '1', 1),
                ('NotificationTimeFrame', '-1 hours', 1),
                ('ClearDataTimeFrame', '-3 days', 1);
                ";
                    using (var insertCmd = new SQLiteCommand(sql, conn))
                    {
                        insertCmd.ExecuteNonQuery();
                    }
                    Console.WriteLine("Default settings inserted into app_settings table.");
                }
            }
        }
        Console.WriteLine("Database and tables created successfully.");
    }

    
    public static SQLiteConnection GetConnection()
    {
        var conn = new SQLiteConnection(connectionString);
        conn.Open(); // Open the connection here
        return conn;
    }

    public static List<UrlEntry> GetSearchUrls(SQLiteConnection conn)
    {
        var list = new List<UrlEntry>();
        string sql = "SELECT id, name, url FROM search_urls WHERE enabled = 1";
        using (var cmd = new SQLiteCommand(sql, conn))
        {
            using (var reader = cmd.ExecuteReader())
            {
                while (reader.Read())
                {
                    list.Add(new UrlEntry
                    {
                        Id = Convert.ToInt32(reader["id"]),
                        Name = reader["name"].ToString(),
                        Url = reader["url"].ToString()
                    });
                }
            }
        }
        return list;
    }

    public static void DeleteSearchUrl(SQLiteConnection conn, int id)
    {
        string sql = "DELETE FROM search_urls WHERE id = @id";
        using (var cmd = new SQLiteCommand(sql, conn))
        {
            cmd.Parameters.AddWithValue("@id", id);
            cmd.ExecuteNonQuery();
        }
    }

    public static void DeleteOldJobs()
    {
        SQLiteConnection conn = GetConnection();
        // First, check if the deletion is enabled and retrieve the time frame
        string checkSql = @"
        SELECT option, enabled 
        FROM app_settings 
        WHERE setting = 'ClearDataTimeFrame' 
        LIMIT 1";

        string timeFrame = "";
        bool isEnabled = false;

        using (var checkCmd = new SQLiteCommand(checkSql, conn))
        {
            using (var reader = checkCmd.ExecuteReader())
            {
                if (reader.Read())
                {
                    isEnabled = Convert.ToBoolean(reader["enabled"]);
                    if (isEnabled)
                    {
                        timeFrame = reader["option"].ToString();
                    }
                }
            }
        }

        // If enabled, proceed with the deletion
        if (isEnabled && !string.IsNullOrEmpty(timeFrame))
        {
            string sql = $@"
            DELETE FROM job_listings
            WHERE posted_on < datetime('now', '-{timeFrame}', 'localtime')";

            using (var cmd = new SQLiteCommand(sql, conn))
            {
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

    public static void UpdateSetting(SQLiteConnection conn, SettingsEntry setting)
    {
        string sql = @"
    test
     ";
    }

    public static void InsertJobListing(SQLiteConnection conn, JobListing job)
    {
        var insertDateTime = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
        string sql = @"
        INSERT INTO job_listings 
        (title, category, posted_on, skills, location_requirement, country, payment, link, notified, search_url, insert_datetime) 
        VALUES 
        (@title, @category, @posted_on, @skills, @location, @country, @payment, @link, @notified, @search_url, @insert_datetime)";
        try
        {
            using (var cmd = new SQLiteCommand(sql, conn))
            {
                cmd.Parameters.AddWithValue("@title", job.Title);
                cmd.Parameters.AddWithValue("@category", job.Category);
                cmd.Parameters.AddWithValue("@posted_on",job.PostedOn);
                cmd.Parameters.AddWithValue("@skills", string.Join(", ", job.Skills));
                cmd.Parameters.AddWithValue("@location", job.LocationRequirement);
                cmd.Parameters.AddWithValue("@country", job.Country);
                cmd.Parameters.AddWithValue("@payment", job.Payment);
                cmd.Parameters.AddWithValue("@link", job.Link);
                cmd.Parameters.AddWithValue("@notified", job.Notified ? 1 : 0);
                cmd.Parameters.AddWithValue("@search_url", job.SearchUrl);
                cmd.Parameters.AddWithValue("@insert_datetime", insertDateTime);
                cmd.ExecuteNonQuery();
            }
        }
        catch (SQLiteException ex)
        {
            if (ex.ResultCode == SQLiteErrorCode.Constraint)
            {
                // Log this error or handle it accordingly, e.g., notify that the entry already exists
                Console.WriteLine("Skipping duplicate job listing: " + job.Title);
            }
            else
            {
                // If the exception is not related to a UNIQUE constraint violation, rethrow it
                throw;
            }
        }
    }
    public static void InsertJobListingAsNotified(SQLiteConnection conn, JobListing job)
    {
        var insertDateTime = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
        string sql = @"
    INSERT INTO job_listings 
    (title, category, posted_on, skills, location_requirement, country, payment, link, notified, search_url, insert_datetime) 
    VALUES 
    (@title, @category, @posted_on, @skills, @location, @country, @payment, @link, @notified, @search_url, @insert_datetime)";
        try
        {
            using (var cmd = new SQLiteCommand(sql, conn))
            {
                cmd.Parameters.AddWithValue("@title", job.Title);
                cmd.Parameters.AddWithValue("@category", job.Category);
                cmd.Parameters.AddWithValue("@posted_on",job.PostedOn);
                cmd.Parameters.AddWithValue("@skills", string.Join(", ", job.Skills));
                cmd.Parameters.AddWithValue("@location", job.LocationRequirement);
                cmd.Parameters.AddWithValue("@country", job.Country);
                cmd.Parameters.AddWithValue("@payment", job.Payment);
                cmd.Parameters.AddWithValue("@link", job.Link);
                // Set notified as 1 regardless of the job.Notified value
                cmd.Parameters.AddWithValue("@notified", 1);
                cmd.Parameters.AddWithValue("@search_url", job.SearchUrl);  
                cmd.Parameters.AddWithValue("@insert_datetime", insertDateTime);
                cmd.ExecuteNonQuery();
            }
        }
        catch (SQLiteException ex)
        {
            if (ex.ResultCode == SQLiteErrorCode.Constraint)
            {
                // Log this error or handle it accordingly, e.g., notify that the entry already exists
                Console.WriteLine("Skipping duplicate job listing: " + job.Title);
            }
            else
            {
                // If the exception is not related to a UNIQUE constraint violation, rethrow it
                throw;
            }
        }
    }

    public static List<JobListing> GetUnnotifiedJobs(SQLiteConnection conn)
    {
        var jobs = new List<JobListing>();
        
        string sql = @"
        SELECT 
                title, 
                category, 
                skills, 
                location_requirement, 
                country, 
                payment, 
                link, 
                notified,
                search_url,
                insert_datetime 
            FROM job_listings 
            WHERE notified = 0 
            and posted_on >= datetime('now', (SELECT OPTION FROM app_settings
        WHERE setting  = 'NotificationTimeFrame' LIMIT 1), 'localtime') 
            order by posted_on desc limit 3";
        using (var cmd = new SQLiteCommand(sql, conn))
        {
            using (var reader = cmd.ExecuteReader())
            {
                while (reader.Read())
                {
                    var job = new JobListing
                    {
                        Title = reader["title"].ToString(),
                        Category = reader["category"].ToString(),
                        Skills = reader["skills"].ToString().Split(", ").ToList(),
                        LocationRequirement = reader["location_requirement"].ToString(),
                        Country = reader["country"].ToString(),
                        Payment = reader["payment"].ToString(),
                        Link = reader["link"].ToString(),
                        Notified = Convert.ToBoolean(reader["notified"]),
                        SearchUrl = reader["search_url"].ToString(),
                        InsertDateTime = reader["insert_datetime"].ToString(),
                    };
                    jobs.Add(job);
                }
            }
        }
        return jobs;
    }

    public static void MarkJobAsNotified(SQLiteConnection conn, string title)
    {
        string sql = "UPDATE job_listings SET notified = 1 WHERE title = @title";
        using (var cmd = new SQLiteCommand(sql, conn))
        {
            cmd.Parameters.AddWithValue("@title", title);
            cmd.ExecuteNonQuery();
        }
    }

    public static void InsertSearchUrl(SQLiteConnection conn, string name, string url)
    {
        string sql = @"
        INSERT INTO search_urls (name, url, enabled)
        VALUES (@name, @url, 1)";  // Assumes new URLs are enabled by default
        using (var cmd = new SQLiteCommand(sql, conn))
        {
            cmd.Parameters.AddWithValue("@name", name);
            cmd.Parameters.AddWithValue("@url", url);
            cmd.ExecuteNonQuery();
        }
    }

    public static string GetSettingValue(string settingKey)
    {
        string value = null; // Default to null if the setting is not found

        using (var conn = GetConnection()) // Using the provided GetConnection method
        {
            // SQL command to select the option value for a specific setting
            string sql = "SELECT option FROM app_settings WHERE setting = @Setting AND enabled = 1 LIMIT 1";

            using (var cmd = new SQLiteCommand(sql, conn))
            {
                // Use parameterized queries to prevent SQL injection
                cmd.Parameters.AddWithValue("@Setting", settingKey);

                // Execute the command and use ExecuteScalar to fetch the single result
                var result = cmd.ExecuteScalar();
                if (result != null)
                {
                    value = result.ToString();
                }
            }
        }

        return value; // Return the found value or null
    }

    public static async Task<string> GetSettingValueAsync(string settingKey)
    {
        using (var conn = GetConnection())
        {
            string sql = "SELECT option FROM app_settings WHERE setting = @Setting AND enabled = 1 LIMIT 1";
            using (var cmd = new SQLiteCommand(sql, conn))
            {
                cmd.Parameters.AddWithValue("@Setting", settingKey);
                var result = cmd.ExecuteScalar();
                return result?.ToString();
            }
        }
    }


}
