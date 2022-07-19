public class ColliderPreviewConfig
{
    public const float ExponentialScaleMiddle = 0.1f;

    public const bool DefaultPreviewsEnabled = false;
    public const float DefaultPreviewProtrusionsOpacity = 0.4f;
    public const float DefaultPreviewXRayOpacity = 0.1f;
    public const float DefaultSelectedPreviewsProtrusionsOpacity = 0.9f;
    public const float DefaultSelectedPreviewsXRayOpacity = 0.7f;
    public const bool DefaultSyncSymmetry = false;

    public bool PreviewsEnabled { get; set; } = DefaultPreviewsEnabled;
    public bool SyncSymmetry { get; set; } = DefaultSyncSymmetry;
    public float PreviewsProtrusionsOpacity { get; set; } = DefaultPreviewProtrusionsOpacity.ExponentialScale(ExponentialScaleMiddle, 1f);
    public int PreviewsProtrusionsRenderQueue = 3002;
    public int PreviewsXRayRenderQueue = 3001;
    public float PreviewsXRayOpacity { get; set; } = DefaultPreviewXRayOpacity.ExponentialScale(ExponentialScaleMiddle, 1f);
    public float SelectedPreviewsProtrusionsOpacity { get; set; } = DefaultSelectedPreviewsProtrusionsOpacity.ExponentialScale(ExponentialScaleMiddle, 1f);
    public float SelectedPreviewsXRayOpacity { get; set; } = DefaultSelectedPreviewsXRayOpacity.ExponentialScale(ExponentialScaleMiddle, 1f);
}
