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
        public enum Facing
        {
            Front,
            Back,
        }

        [SerializeField]
        private Facing[] facingPriorities = new Facing[] {
            Facing.Back, Facing.Front
        };

        [SerializeField]
        private WebCamKind[] kindPriority = new WebCamKind[] {
            WebCamKind.WideAngle, WebCamKind.Telephoto, WebCamKind.UltraWideAngle,
        };

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

        public Facing[] FacingPriorities
        {
            get => facingPriorities;
            set => facingPriorities = value;
        }

        public WebCamKind[] KindPriorities
        {
            get => kindPriority;
            set => kindPriority = value;
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
            static Facing GetFacing(WebCamDevice device)
            {
                return device.isFrontFacing ? Facing.Front : Facing.Back;
            }
            devices = WebCamTexture.devices
                .Where(d => facingPriorities.Contains(GetFacing(d)) && kindPriority.Contains(d.kind))
                .OrderBy(d => Array.IndexOf(facingPriorities, GetFacing(d)))
                .ThenBy(d => Array.IndexOf(kindPriority, d.kind))
                .ToArray();
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
    }
}
