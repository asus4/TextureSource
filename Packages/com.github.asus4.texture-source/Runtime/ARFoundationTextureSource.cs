// Only available with AR Foundation
#if MODULE_ARFOUNDATION_ENABLED
namespace TextureSource
{
    using System;
    using System.Collections.Generic;
    using UnityEngine;
    using UnityEngine.XR.ARFoundation;

    /// <summary>
    /// Source from ARFoundation
    /// </summary>
    [CreateAssetMenu(menuName = "ScriptableObject/Texture Source/ARFoundation", fileName = "ARFoundationTextureSource")]
    public class ARFoundationTextureSource : BaseTextureSource
    {
        private static readonly int _DisplayTransformID = Shader.PropertyToID("_UnityDisplayTransform");

        private ARCameraManager cameraManager;
        private RenderTexture texture;
        private int lastUpdatedFrame = -1;

        protected Material material;

        public override bool DidUpdateThisFrame => lastUpdatedFrame == Time.frameCount;
        public override Texture Texture => texture;

        protected virtual RenderTextureFormat PreferredRenderTextureFormat => RenderTextureFormat.ARGB32;

        protected virtual Shader ARCameraBackgroundShader
        {
            get
            {
                string shaderName = Application.platform switch
                {
                    RuntimePlatform.Android => "Unlit/ARCoreBackground",
                    RuntimePlatform.IPhonePlayer => "Unlit/ARKitBackground",
#if UNITY_ANDROID
                    _ => "Unlit/ARCoreBackground",
#elif UNITY_IOS
                    _ => "Unlit/ARKitBackground",
#else
                    _ => throw new System.NotSupportedException($"ARFoundationTextureSource is not supported on {Application.platform}"),
#endif
                };
                return Shader.Find(shaderName);
            }
        }

        public override void Start()
        {
            cameraManager = FindAnyObjectByType<ARCameraManager>();
            if (cameraManager == null)
            {
                throw new InvalidOperationException("ARCameraManager is not found");
            }
            material = new Material(ARCameraBackgroundShader);

            cameraManager.frameReceived += OnFrameReceived;
        }

        public override void Stop()
        {
            if (cameraManager != null)
            {
                cameraManager.frameReceived -= OnFrameReceived;
            }

            if (texture != null)
            {
                texture.Release();
                Destroy(texture);
                texture = null;
            }

            if (material != null)
            {
                Destroy(material);
                material = null;
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
            // The shader doesn't work for some reason when set ARKIT_BACKGROUND_URP 
            // SetMaterialKeywords(material, args.enabledMaterialKeywords, args.disabledMaterialKeywords);

            // Find best texture size
            int bestWidth = 0;
            int bestHeight = 0;
            int count = args.textures.Count;
            for (int i = 0; i < count; i++)
            {
                var tex = args.textures[i];
                bestWidth = Math.Max(bestWidth, tex.width);
                bestHeight = Math.Max(bestHeight, tex.height);
                material.SetTexture(args.propertyNameIds[i], tex);
            }

            // Swap if screen is portrait
            float screenAspect = (float)Screen.width / Screen.height;
            if (bestWidth > bestHeight && screenAspect < 1f)
            {
                (bestWidth, bestHeight) = (bestHeight, bestWidth);
            }

            // Create render texture
            Utils.GetTargetSizeScale(
                new Vector2Int(bestWidth, bestHeight),
                screenAspect,
                out Vector2Int dstSize,
                out Vector2 scale);
            EnsureRenderTexture(ref texture, dstSize.x, dstSize.y, 24, PreferredRenderTextureFormat);

            if (args.displayMatrix.HasValue)
            {
                material.SetMatrix(_DisplayTransformID, args.displayMatrix.Value);
            }

            Graphics.Blit(null, texture, material);

            lastUpdatedFrame = Time.frameCount;
        }

        protected static void SetMaterialKeywords(Material material, IReadOnlyList<string> enabledKeywords, IReadOnlyList<string> disabledKeywords)
        {
            if (enabledKeywords != null)
            {
                foreach (var keyword in enabledKeywords)
                {
                    material.EnableKeyword(keyword);
                }
            }
            if (disabledKeywords != null)
            {
                foreach (var keyword in disabledKeywords)
                {
                    material.DisableKeyword(keyword);
                }
            }
        }

        public static void EnsureRenderTexture(ref RenderTexture texture,
            int width, int height,
            int depth, RenderTextureFormat format)
        {
            if (texture == null || texture.width != width || texture.height != height)
            {
                if (texture != null)
                {
                    texture.Release();
                }
                texture = new RenderTexture(width, height, depth, format);
                texture.Create();
            }
        }
    }
}
#endif // MODULE_ARFOUNDATION_ENABLED
