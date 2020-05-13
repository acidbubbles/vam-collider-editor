public class ColliderPreviewConfig
{
    public const bool DefaultPreviewsEnabled = false;
    public const bool DefaultXRayPreviews = true;
    public const float DefaultPreviewsOpacity = 0.001f;
    public const float DefaultSelectedPreviewOpacity = 0.3f;

    public bool PreviewsEnabled { get; set; } = DefaultPreviewsEnabled;
    public bool XRayPreviews { get; set; } = DefaultXRayPreviews;
    public float PreviewsOpacity {get;set;} = DefaultPreviewsOpacity;
    public float SelectedPreviewsOpacity {get;set;} = DefaultSelectedPreviewOpacity;
}
