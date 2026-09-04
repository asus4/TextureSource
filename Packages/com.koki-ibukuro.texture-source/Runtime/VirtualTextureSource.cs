namespace TextureSource
{
    using UnityEngine;
    using UnityEngine.Events;
    using UnityEngine.Scripting;

    /// <summary>
    /// Invokes texture update event from the provided texture source ScriptableObject asset.
    /// </summary>
    public class VirtualTextureSource : MonoBehaviour
    {
        [System.Serializable]
        public class TextureEvent : UnityEvent<Texture> { }
        [System.Serializable]
        public class AspectChangeEvent : UnityEvent<float> { }

        [SerializeField]
        [Tooltip("A texture source scriptable object")]
        private BaseTextureSource source = default;

        [SerializeField]
        [Tooltip("A texture source scriptable object for Editor. If it is null, used source in Editor")]
        private BaseTextureSource sourceForEditor = null;

        [SerializeField]
        [Tooltip("If true, the texture is trimmed to the screen aspect ratio. Use this to show in full screen")]
        private bool trimToScreenAspect = false;

        [Tooltip("Event called when texture updated")]
        public TextureEvent OnTexture = new TextureEvent();

        [Tooltip("Event called when the aspect ratio changed")]
        public AspectChangeEvent OnAspectChange = new AspectChangeEvent();

        private BaseTextureSource activeSource;
        private float aspect = float.NegativeInfinity;
        private TextureTransformer transformer;

        public bool DidUpdateThisFrame => activeSource.DidUpdateThisFrame;
        public Texture Texture => activeSource.Texture;

        public BaseTextureSource Source
        {
            get => source;
            set => source = value;
        }
        public BaseTextureSource SourceForEditor
        {
            get => sourceForEditor;
            set => sourceForEditor = value;
        }

        private void OnEnable()
        {
            activeSource = sourceForEditor != null && Application.isEditor
                ? sourceForEditor
                : source;

            if (activeSource == null)
            {
                Debug.LogError("Source is not set.", this);
                enabled = false;
                return;
            }
            activeSource.Start();
        }

        private void OnDisable()
        {
            if (activeSource != null)
            {
                activeSource.Stop();
                activeSource = null;
            }
            transformer?.Dispose();
            transformer = null;
        }

        private void Update()
        {
            if (!activeSource.DidUpdateThisFrame)
            {
                return;
            }

            Texture tex = Transform();
            OnTexture?.Invoke(tex);

            float aspect = (float)tex.width / tex.height;
            if (aspect != this.aspect)
            {
                OnAspectChange?.Invoke(aspect);
                this.aspect = aspect;
            }
        }

        // Invoked by UI Events
        [Preserve]
        public void NextSource()
        {
            activeSource?.Next();
        }

        private Texture Transform()
        {
            Texture originalTex = activeSource.Texture;
            Matrix4x4 transformMatrix = activeSource.TransformMatrix;
            Vector2Int transformSize = activeSource.TransformSize;

            bool needTrim = false;
            Vector2Int dstSize = transformSize;
            Matrix4x4 matrix = transformMatrix;

            if (trimToScreenAspect)
            {
                float srcAspect = (float)transformSize.x / transformSize.y;
                float dstAspect = (float)Screen.width / Screen.height;
                // Allow 1% mismatch
                needTrim = Mathf.Abs(srcAspect - dstAspect) >= 0.01f;
                if (needTrim)
                {
                    Utils.GetTargetSizeScale(transformSize, dstAspect, out dstSize, out Vector2 scale);
                    var trimMatrix = TextureTransformer.BuildMatrix(Vector2.zero, 0, scale);
                    matrix = transformMatrix * trimMatrix;
                }
            }

            if (!needTrim && transformMatrix.isIdentity)
            {
                return originalTex;
            }

            EnsureTransformer(dstSize, originalTex);
            return transformer.Transform(originalTex, matrix);
        }

        private void EnsureTransformer(Vector2Int size, Texture tex)
        {
            if (transformer != null && transformer.width == size.x && transformer.height == size.y)
            {
                // No need to recreate
                return;
            }
            // Recreate transformer with new size
            transformer?.Dispose();
            RenderTextureFormat format = (tex is RenderTexture renderTex)
                ? renderTex.format
                : RenderTextureFormat.ARGB32;
            transformer = new TextureTransformer(size.x, size.y, format);
        }
    }
}
