using SimpleJSON;

public interface IModel
{
    string Id { get; }
    string Label { get; }
    bool IsDuplicate { get; }
    bool Selected { get; set; }

    void LoadJson(JSONClass jc);
    void AppendJson(JSONClass jc);
}
