namespace TextureSource
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using UnityEngine;

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
                if (webCamTexture == null || !webCamTexture.didUpdateThisFrame)
                {
                    return false;
                }
                return true;
            }
        }

        public override Texture Texture => webCamTexture;

        private WebCamDevice[] devices;
        private WebCamTexture webCamTexture;
        private int currentIndex;

        public override void Start()
        {
            devices = WebCamTexture.devices.Where(IsMatchFilter).ToArray();
            StartCamera(currentIndex);
        }

        public override void Stop()
        {
            if (webCamTexture != null)
            {
                webCamTexture.Stop();
                webCamTexture = null;
            }
        }

        public override void Next()
        {
            currentIndex = (currentIndex + 1) % devices.Length;
            StartCamera(currentIndex);
        }

        private void StartCamera(int index)
        {
            Stop();
            WebCamDevice device = devices[index];
            webCamTexture = new WebCamTexture(device.name, resolution.x, resolution.y, frameRate);
            webCamTexture.Play();
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
