using SimpleJSON;

public interface IModel
{
    string Type { get; }
    Group Group { get; }
    string Id { get; }
    string Label { get; }
    bool IsDuplicate { get; }
    bool Selected { get; set; }

    void SetXRayPreview(bool value);
    void SetSelectedPreviewOpacity(float value);
    void SetPreviewOpacity(float value);
    void SetShowPreview(bool value);
    void DestroyPreview();

    void LoadJson(JSONClass jc);
    void AppendJson(JSONClass jc);
}
