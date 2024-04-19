using System.Windows.Input;

namespace UpWorker.Core.Models;

// Model for the SampleDataService. Replace with your own model.
public class SettingsEntry
{

    public string Setting
    {
        get; set;
    }

    public string Option
    {
        get; set;
    }

    public ICommand DeleteCommand
    {
        get; set;
    }
}
