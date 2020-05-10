using SimpleJSON;

public interface IModel
{
    string Id { get; }
    string Label { get; }

    void DestroyControls();
    void LoadJson(JSONClass asObject);
    void AppendJson(JSONClass editablesJsonClass);
}
