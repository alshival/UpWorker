﻿using System.Windows.Input;

namespace UpWorker.Models;

// Model for the SampleDataService. Replace with your own model.
public class RssURL
{
    public int Id
    {
        get; set;
    }

    public string Name
    {
        get; set;
    }

    public string Url
    {
        get; set;
    }

    public bool Enabled
    {
        get; set;
    }

    public ICommand? DeleteCommand
    {
        get; set;
    }
}
