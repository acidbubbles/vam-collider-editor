public class ColliderPreviewConfig
{
    public const float ExponentialScaleMiddle = 0.1f;

    public const bool DefaultPreviewsEnabled = false;
    public const bool DefaultXRayPreviews = true;
    public const float DefaultPreviewsOpacity = 0.1f;
    public const float DefaultSelectedPreviewOpacity = 0.6f;

    public bool PreviewsEnabled { get; set; } = DefaultPreviewsEnabled;
    public bool XRayPreviews { get; set; } = DefaultXRayPreviews;
    public float PreviewsOpacity {get;set;} = DefaultPreviewsOpacity.ExponentialScale(ExponentialScaleMiddle, 1f);
    public float SelectedPreviewsOpacity {get;set;} = DefaultSelectedPreviewOpacity.ExponentialScale(ExponentialScaleMiddle, 1f);
}
