namespace TextureSource
{
    using System;
    using System.Linq;
    using UnityEngine;

    /// <summary>
    /// Source from WebCamTexture
    /// </summary>
    [CreateAssetMenu(menuName = "ScriptableObject/Texture Source/WebCam", fileName = "WebCamTextureSource")]
    public sealed class WebCamTextureSource : BaseTextureSource
    {
        [Flags]
        public enum WebCamKindFlag
        {
            WideAngle = 1 << 0,
            Telephoto = 1 << 1,
            ColorAndDepth = 1 << 2,
            UltraWideAngle = 1 << 3,
        }

        [Flags]
        public enum FacingFlag
        {
            Front = 1 << 0,
            Back = 1 << 1,
        }

        [SerializeField]
        private WebCamKindFlag kindFilter = WebCamKindFlag.WideAngle | WebCamKindFlag.Telephoto | WebCamKindFlag.UltraWideAngle;

        [SerializeField]
        private FacingFlag facingFilter = FacingFlag.Front | FacingFlag.Back;

        [SerializeField]
        private Vector2Int resolution = new Vector2Int(1270, 720);

        [SerializeField]
        private int frameRate = 60;

        public override bool DidUpdateThisFrame
        {
            get
            {
                if (webCamTexture == null || webCamTexture.width < 20)
                {
                    // On macOS, it returns the 10x10 texture at first several frames.
                    return false;
                }
                return webCamTexture.didUpdateThisFrame;
            }
        }

        public override Texture Texture => NormalizeWebCam();

        private WebCamDevice[] devices;
        private WebCamTexture webCamTexture;
        private int currentIndex;
        private TextureTransformer transformer;
        private int lastUpdatedFrame = -1;
        private bool isFrontFacing;

        public WebCamKindFlag KindFilter
        {
            get => kindFilter;
            set => kindFilter = value;
        }

        public FacingFlag FacingFilter
        {
            get => facingFilter;
            set => facingFilter = value;
        }

        public Vector2Int Resolution
        {
            get => resolution;
            set => resolution = value;
        }

        public bool IsFrontFacing => isFrontFacing;

        public int FrameRate
        {
            get => frameRate;
            set => frameRate = value;
        }

        public override void Start()
        {
            devices = WebCamTexture.devices.Where(IsMatchFilter).ToArray();
            StartCamera(currentIndex);
        }

        private void StartCamera(int index)
        {
            Stop();
            WebCamDevice device = devices[index];
            webCamTexture = new WebCamTexture(device.name, resolution.x, resolution.y, frameRate);
            webCamTexture.Play();
            isFrontFacing = device.isFrontFacing;
            lastUpdatedFrame = -1;
        }

        public override void Stop()
        {
            if (webCamTexture != null)
            {
                webCamTexture.Stop();
                webCamTexture = null;
            }
            transformer?.Dispose();
            transformer = null;
        }

        public override void Next()
        {
            currentIndex = (currentIndex + 1) % devices.Length;
            StartCamera(currentIndex);
        }

        private RenderTexture NormalizeWebCam()
        {
            if (webCamTexture == null)
            {
                return null;
            }

            if (lastUpdatedFrame == Time.frameCount)
            {
                return transformer.Texture;
            }

            bool isPortrait = webCamTexture.videoRotationAngle == 90 || webCamTexture.videoRotationAngle == 270;
            int width = webCamTexture.width;
            int height = webCamTexture.height;
            if (isPortrait)
            {
                (width, height) = (height, width); // swap
            }

            bool needInitialize = transformer == null || width != transformer.width || height != transformer.height;
            if (needInitialize)
            {
                transformer?.Dispose();
                transformer = new TextureTransformer(width, height);
            }

            Vector2 scale;
            if (isPortrait)
            {
                scale = new Vector2(webCamTexture.videoVerticallyMirrored ^ isFrontFacing ? -1 : 1, 1);
            }
            else
            {
                scale = new Vector2(isFrontFacing ? -1 : 1, webCamTexture.videoVerticallyMirrored ? -1 : 1);
            }
            transformer.Transform(webCamTexture, Vector2.zero, -webCamTexture.videoRotationAngle, scale);

            // Debug.Log($"mirrored: {webCamTexture.videoVerticallyMirrored}, angle: {webCamTexture.videoRotationAngle}, isFrontFacing: {isFrontFacing}");

            lastUpdatedFrame = Time.frameCount;
            return transformer.Texture;
        }

        private bool IsMatchFilter(WebCamDevice device)
        {
            WebCamKindFlag kind = device.kind switch
            {
                WebCamKind.WideAngle => WebCamKindFlag.WideAngle,
                WebCamKind.Telephoto => WebCamKindFlag.Telephoto,
                WebCamKind.ColorAndDepth => WebCamKindFlag.ColorAndDepth,
                WebCamKind.UltraWideAngle => WebCamKindFlag.UltraWideAngle,
                _ => throw new NotImplementedException($"Unknown WebCamKind: {device.kind}"),
            };
            FacingFlag facing = device.isFrontFacing
                ? FacingFlag.Front
                : FacingFlag.Back;

            return kindFilter.HasFlag(kind)
                && facingFilter.HasFlag(facing);
        }
    }
}
