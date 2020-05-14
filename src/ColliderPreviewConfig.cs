public class ColliderPreviewConfig
{
    public const bool DefaultPreviewsEnabled = false;
    public const bool DefaultXRayPreviews = true;
    public const float DefaultPreviewsOpacity = 0.006898069f;
    public const float DefaultSelectedPreviewOpacity = 0.1620826f;

    public bool PreviewsEnabled { get; set; } = DefaultPreviewsEnabled;
    public bool XRayPreviews { get; set; } = DefaultXRayPreviews;
    public float PreviewsOpacity {get;set;} = DefaultPreviewsOpacity;
    public float SelectedPreviewsOpacity {get;set;} = DefaultSelectedPreviewOpacity;
}
