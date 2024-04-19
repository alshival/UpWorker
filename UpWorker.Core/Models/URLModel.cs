using System.Windows.Input;

namespace UpWorker.Core.Models;

// Model for the SampleDataService. Replace with your own model.
public class UrlEntry
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

    public ICommand DeleteCommand
    {
        get; set;
    }
}
