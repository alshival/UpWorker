namespace UpWorker.Core.Models;

public class Setting
{
    public string Option
    {
        get; set;
    }
    public bool Enabled
    {
        get; set;
    }

    public Setting(string option, bool enabled)
    {
        Option = option;
        Enabled = enabled;
    }
}
