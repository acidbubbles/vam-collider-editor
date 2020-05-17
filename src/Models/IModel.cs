using SimpleJSON;

public interface IModel
{
    string Type { get; }
    Group Group { get; }
    string Id { get; }
    string Label { get; }
    bool IsDuplicate { get; }
    bool Shown { get; set; }
    bool Selected { get; set; }
    bool Modified { get; }

    void UpdatePreviewFromConfig();
    void SyncPreview();
    void DestroyPreview();
    void ResetToInitial();

    void LoadJson(JSONClass jc);
    void AppendJson(JSONClass jc);
}
