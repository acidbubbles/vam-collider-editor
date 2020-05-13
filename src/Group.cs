using System.Collections.Generic;
using System.Text.RegularExpressions;

public class Group
{
    public string Name { get; set; }
    public Regex Pattern { get; set; }
    public List<IModel> Editables { get; } = new List<IModel>();

    public Group(string name, string pattern)
    {
        Name = name;
        Pattern = new Regex(pattern, RegexOptions.Compiled | RegexOptions.ExplicitCapture);
    }

    public bool Test(string name)
    {
        return Pattern.IsMatch(name);
    }
}
