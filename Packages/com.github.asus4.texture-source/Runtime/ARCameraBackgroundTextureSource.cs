// Only available with AR Foundation
#if MODULE_ARFOUNDATION_ENABLED
namespace TextureSource
{
    using System;
    using UnityEngine;
    using UnityEngine.Assertions;
    using UnityEngine.XR.ARFoundation;

    [CreateAssetMenu(menuName = "ScriptableObject/Texture Source/ARFoundation", fileName = "ARFoundationTextureSource")]
    public sealed class ARFoundationTextureSource : BaseTextureSource
    {
        private ARCameraManager cameraManager;
        private TextureTransformer transformer;

        private int[] propertyIds = new int[2];
        private Texture[] textures = new Texture[2];
        private int lastUpdatedFrame = -1;

        private static readonly Matrix4x4 PopMatrix = Matrix4x4.Translate(new Vector3(0.5f, 0.5f, 0));
        private static readonly Matrix4x4 PushMatrix = Matrix4x4.Translate(new Vector3(-0.5f, -0.5f, 0));

        public static readonly Lazy<ComputeShader> ARCameraComputeShader = new(() =>
        {
            string shaderName = Application.platform switch
            {
                RuntimePlatform.Android => "TextureTransformARCore",
                RuntimePlatform.IPhonePlayer => "TextureTransformARKit",
#if UNITY_ANDROID
                _ => "TextureTransformARCore",
#elif UNITY_IOS
                _ => "TextureTransformARKit",
#else
                _ => throw new NotSupportedException($"ARFoundationTextureSource is not supported on {Application.platform}"),
#endif
            };
            return Resources.Load<ComputeShader>($"com.github.asus4.texture-source/{shaderName}");
        });

        public override bool DidUpdateThisFrame => lastUpdatedFrame == Time.frameCount;
        public override Texture Texture => transformer?.Texture;

        public override void Start()
        {
            cameraManager = FindAnyObjectByType<ARCameraManager>();
            if (cameraManager == null)
            {
                throw new InvalidOperationException("ARCameraManager is not found");
            }
            cameraManager.frameReceived += OnFrameReceived;
        }

        public override void Stop()
        {
            if (cameraManager != null)
            {
                cameraManager.frameReceived -= OnFrameReceived;
            }
        }

        public override void Next()
        {
            if (cameraManager == null)
            {
                return;
            }
            // Switch the camera facing direction.
            cameraManager.requestedFacingDirection = cameraManager.currentFacingDirection switch
            {
                CameraFacingDirection.World => CameraFacingDirection.User,
                CameraFacingDirection.User => CameraFacingDirection.World,
                _ => CameraFacingDirection.World,
            };
        }



        private void OnFrameReceived(ARCameraFrameEventArgs args)
        {
            Assert.IsTrue(args.displayMatrix.HasValue, "displayMatrix is null");

            // Ensure the array length
            int count = args.textures.Count;
            if (count > propertyIds.Length)
            {
                Array.Resize(ref propertyIds, count);
                Array.Resize(ref textures, count);
            }
            var propertyIdsSpan = new Span<int>(propertyIds, 0, count);
            var texturesSpan = new Span<Texture>(textures, 0, count);

            // Set texture
            int width = 0;
            int height = 0;
            for (int i = 0; i < count; i++)
            {
                propertyIdsSpan[i] = args.propertyNameIds[i];
                texturesSpan[i] = args.textures[i];
                width = Math.Max(width, args.textures[i].width);
                height = Math.Max(height, args.textures[i].height);
            }

            float screenAspect = (float)Screen.width / Screen.height;
            // Swap if screen is portrait
            if (width > height && screenAspect < 1f)
            {
                (width, height) = (height, width);
            }

            Utils.GetTargetSizeScale(
                new Vector2Int(width, height), screenAspect,
                out Vector2Int dstSize, out Vector2 scale);

            if (transformer == null || dstSize.x != transformer.width || dstSize.y != transformer.height)
            {
                transformer?.Dispose();
                transformer = new TextureTransformer(dstSize.x, dstSize.y, ARCameraComputeShader.Value);
            }

            Matrix4x4 mtx = args.displayMatrix.Value;
            mtx = PopMatrix * mtx.transpose * PushMatrix;

            transformer.Transform(propertyIdsSpan, texturesSpan, mtx);

            lastUpdatedFrame = Time.frameCount;

            // Debug.Log($"OnFrameReceived: matrix={mtx}, width={width}, height={height}");
        }
    }
}
#endif // MODULE_ARFOUNDATION_ENABLED
