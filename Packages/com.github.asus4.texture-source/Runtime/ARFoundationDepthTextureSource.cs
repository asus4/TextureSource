// Only available with AR Foundation
#if MODULE_ARFOUNDATION_ENABLED
namespace TextureSource
{
    using System.Collections;
    using System.Collections.Generic;
    using UnityEngine;
    using UnityEngine.XR.ARFoundation;

    /// <summary>
    /// The same with ARFoundationTextureSource but depth is encoded into alpha channel
    /// (Experimental feature, only available with the latest AR Foundation)
    /// </summary>
    [CreateAssetMenu(menuName = "ScriptableObject/Texture Source/ARFoundation Depth", fileName = "ARFoundationDepthTextureSource")]
    public sealed class ARFoundationDepthTextureSource : ARFoundationTextureSource
    {
        public enum DepthMode
        {
            Depth01,
            RawDistance,
        }

        [SerializeField]
        private DepthMode depthMode = DepthMode.Depth01;

        // [HideInInspector]
        [SerializeField]
        private Shader shaderForARCore;
        // [HideInInspector]
        [SerializeField]
        private Shader shaderForARKit;

        private AROcclusionManager occlusionManager;

        protected override RenderTextureFormat PreferredRenderTextureFormat => RenderTextureFormat.ARGBHalf;

        protected override Shader ARCameraBackgroundShader
        {
            get
            {
                return Application.platform switch
                {
                    RuntimePlatform.Android => shaderForARCore,
                    RuntimePlatform.IPhonePlayer => shaderForARKit,
#if UNITY_ANDROID
                    _ => shaderForARCore,
#elif UNITY_IOS
                    _ => shaderForARKit,
#else
                    _ => throw new System.NotSupportedException($"ARFoundationTextureSource is not supported on {Application.platform}"),
#endif
                };
            }
        }

        public override void Start()
        {
            occlusionManager = FindAnyObjectByType<AROcclusionManager>();
            if (occlusionManager == null)
            {
                throw new System.InvalidOperationException("Requires AROcclusionManager to use ARFoundationDepthTextureSource");
            }
            occlusionManager.frameReceived += OnOcclusionFrameReceived;

            base.Start();

            if (depthMode == DepthMode.RawDistance)
            {
                material.EnableKeyword("TEXTURE_SOURCE_RAW_DISTANCE");
            }
        }

        public override void Stop()
        {
            if (occlusionManager != null)
            {
                occlusionManager.frameReceived -= OnOcclusionFrameReceived;
            }

            base.Stop();
        }

        private void OnOcclusionFrameReceived(AROcclusionFrameEventArgs args)
        {
            if (args.textures.Count == 0)
            {
                return;
            }

            SetMaterialKeywords(material, args.enabledMaterialKeywords, args.disabledMaterialKeywords);

            int count = args.textures.Count;
            for (int i = 0; i < count; i++)
            {
                var tex = args.textures[i];
                material.SetTexture(args.propertyNameIds[i], tex);
            }
        }
    }
}
#endif // MODULE_ARFOUNDATION_ENABLED
