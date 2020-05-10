using System.Text.RegularExpressions;

public class RigidbodyGroupModel
{
    public string Id { get; set; }
    public string Name { get; set; }
    public Regex Pattern { get; set; }

    public RigidbodyGroupModel(string name, string pattern)
    {
        Id = name;
        Name = name;
        Pattern = new Regex(pattern, RegexOptions.Compiled | RegexOptions.ExplicitCapture);
    }
}
