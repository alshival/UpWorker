﻿using System.Globalization;
using System.ServiceModel.Syndication;
using System.Text.RegularExpressions;
using System.Xml;
using UpWorker.Models;
using UpWorker.Services;

namespace UpWorker.Helpers;

public class RssParser
{
    public static async Task<SyndicationFeed> FetchRssDataAsync(string url)
    {
        using (var client = new HttpClient())
        {
            var response = await client.GetAsync(url);
            if (response.IsSuccessStatusCode)
            {
                var data = await response.Content.ReadAsStringAsync();
                return ParseRssData(data);
            }
            throw new HttpRequestException($"Failed to retrieve RSS data: {response.StatusCode}");
        }
    }

    public static SyndicationFeed ParseRssData(string rssData)
    {
        using (var reader = XmlReader.Create(new System.IO.StringReader(rssData)))
        {
            return SyndicationFeed.Load(reader);
        }
    }

    public static string StripHtmlTags(string input)
    {
        string result = Regex.Replace(input, "<.*?>", string.Empty);
        result = Regex.Replace(result, "click to apply", "");
        result = Regex.Replace(result, "&#039;", "'");
        result = Regex.Replace(result, "&amp;", "&");
        result = Regex.Replace(result, "&rdquo;", "\"");
        result = Regex.Replace(result, "&quot;", "\"");
        result = Regex.Replace(result, "quot;", "\"");
        result = Regex.Replace(result, "&nbsp;", " ");
        result = Regex.Replace(result, "&bull;", "•");
        result = Regex.Replace(result, "&oacute;", "ó");
        result = Regex.Replace(result, "&ograve;", "ò");
        result = Regex.Replace(result, "&aacute;", "á");
        result = Regex.Replace(result, "&agrave;", "à");
        result = Regex.Replace(result, "&eacute;", "é");
        result = Regex.Replace(result, "&ugrave;", "ù");
        result = Regex.Replace(result, "&uacute;", "ú");
        result = Regex.Replace(result, "&yacute;", "ý");
        result = Regex.Replace(result, "&iacute;", "í");



        return result;
    }


    public static Job FromRssItem(SyndicationItem item)
    {
        var title = Regex.Replace(item.Title.Text,"- Upwork$",string.Empty);
        // Correctly extracting the first link as a string
        var link = item.Links.FirstOrDefault()?.Uri.ToString() ?? "";
        var description = item.Summary?.Text ?? "";

        var jobdescription = StripHtmlTags(Regex.Replace(description, "<b>.*", ""));
        var category = ExtractField(description, "<b>Category</b>:");
        var skills = description.Split("<b>Skills</b>:")
                                .Skip(1)
                                .FirstOrDefault()?
                                .Split(new[] { "<br />" }, StringSplitOptions.RemoveEmptyEntries) // Split by <br /> to separate the sections
                                .FirstOrDefault() // Get the first section which is the skills
                                ?.Split(new[] { ',', '\n' }, StringSplitOptions.RemoveEmptyEntries) // Split skills by commas
                                .Select(skill => skill.Trim()) // Trim each skill
                                .ToList() ?? new List<string>(); // Default to an empty list if null


        var locationRequirement = ExtractField(description, "<b>Location Requirement</b>:");
        var country = ExtractField(description, "<b>Country</b>:");
        var payment = ExtractPayment(description);

        var postedOnString = ExtractField(description, "<b>Posted On</b>:");
        DateTime postedOnDate;
        string formattedPostedOn = null;
        // Assuming the format "April 18, 2024 19:00 UTC"
        if (DateTime.TryParseExact(postedOnString, "MMMM d, yyyy HH:mm UTC", CultureInfo.InvariantCulture, DateTimeStyles.AssumeUniversal, out postedOnDate))
        {
            // Convert to a SQLite-compatible format (ISO 8601)
            formattedPostedOn = postedOnDate.ToLocalTime().ToString("yyyy-MM-dd HH:mm:ss");
        }
        else if (DateTime.TryParseExact(postedOnString, "MMMM dd, yyyy HH:mm UTC", CultureInfo.InvariantCulture, DateTimeStyles.AssumeUniversal, out postedOnDate))
        {
            // Convert to a SQLite-compatible format (ISO 8601)
            formattedPostedOn = postedOnDate.ToLocalTime().ToString("yyyy-MM-dd HH:mm:ss");
        }

        return new Job
        {
            Title = title ?? "",
            Category = category,
            JobDescription = jobdescription,
            PostedOn = formattedPostedOn,
            Skills = skills,
            LocationRequirement = locationRequirement,
            Country = country,
            Payment = payment,
            Link = link,
            Notified = false
        };
    }


    private static string ExtractField(string description, string fieldName)
    {
        return description.Split(fieldName)
                          .Skip(1)
                          .FirstOrDefault()?
                          .Split(new[] { "<br />", "\n" }, StringSplitOptions.RemoveEmptyEntries)
                          .FirstOrDefault()?
                          .Trim();
    }

    private static string ExtractPayment(string description)
    {
        var hourly = ExtractField(description, "<b>Hourly Range</b>:");
        if (!string.IsNullOrEmpty(hourly))
            return $"Hourly {hourly}";

        var budget = ExtractField(description, "<b>Budget</b>:");
        if (!string.IsNullOrEmpty(budget))
            return $"Budget {budget}";

        return null;
    }

    public async Task FetchAndProcessRSS(string url)
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
}