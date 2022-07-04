public class ColliderPreviewConfig
{
    public const float ExponentialScaleMiddle = 0.1f;

    public const bool DefaultPreviewsEnabled = false;
    public const bool DefaultXRayPreviews = true;
    public const float DefaultPreviewsOpacity = 0.5f;
    public const float DefaultSelectedPreviewOpacity = 1.0f;
    public const float DefaultRelativeXRayOpacity = 0.5f;
    public const bool DefaultSyncSymmetry = false;

    public bool PreviewsEnabled { get; set; } = DefaultPreviewsEnabled;
    public bool XRayPreviews { get; set; } = DefaultXRayPreviews;
    public bool SyncSymmetry { get; set; } = DefaultSyncSymmetry;
    public float PreviewsOpacity { get; set; } = DefaultPreviewsOpacity.ExponentialScale(ExponentialScaleMiddle, 1f);
    public float SelectedPreviewsOpacity { get; set; } = DefaultSelectedPreviewOpacity.ExponentialScale(ExponentialScaleMiddle, 1f);
    public float RelativeXRayOpacity { get; set; } = DefaultRelativeXRayOpacity;
}
