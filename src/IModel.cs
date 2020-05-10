using SimpleJSON;

public interface IModel
{
    string Id { get; }
    string Label { get; }
    bool Selected { get; set; }

    // TODO: Replace by deselecting, this should not be public
    void DestroyControls();
    void LoadJson(JSONClass jc);
    void AppendJson(JSONClass jc);
}
