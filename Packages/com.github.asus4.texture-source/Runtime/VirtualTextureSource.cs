namespace TextureSource
{
    using UnityEngine;
    using UnityEngine.Events;
    using UnityEngine.Scripting;

    /// <summary>
    /// Virtual Texture Source
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
        private bool trimToScreenAspect = false;

        [Tooltip("Event called when texture updated")]
        public TextureEvent OnTexture = new TextureEvent();

        [Tooltip("Event called when the aspect ratio changed")]
        public AspectChangeEvent OnAspectChange = new AspectChangeEvent();

        private ITextureSource activeSource;
        private float aspect = float.NegativeInfinity;
        private TextureTransformer transformer;

        public bool DidUpdateThisFrame => activeSource.DidUpdateThisFrame;
        public Texture Texture => activeSource.Texture;

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
            activeSource?.Stop();
            transformer?.Dispose();
            transformer = null;
        }

        private void Update()
        {
            if (!activeSource.DidUpdateThisFrame)
            {
                return;
            }

            Texture tex = trimToScreenAspect
                ? TrimToScreen(Texture)
                : Texture;
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

        private RenderTexture TrimToScreen(Texture texture)
        {
            float cameraAspect = (float)texture.width / texture.height;
            float targetAspect = (float)Screen.width / Screen.height;

            int width, height;
            Vector2 scale;
            if (cameraAspect > targetAspect)
            {
                width = RoundToEven(texture.height * targetAspect);
                height = texture.height;
                scale = new Vector2((float)texture.width / width, 1);
            }
            else
            {
                width = texture.width;
                height = RoundToEven(texture.width / targetAspect);
                scale = new Vector2(1, (float)texture.height / height);
            }

            bool needInitialize = transformer == null || width != transformer.width || height != transformer.height;
            if (needInitialize)
            {
                transformer?.Dispose();
                transformer = new TextureTransformer(width, height);
            }

            return transformer.Transform(texture, Vector2.zero, 0, scale);
        }

        private static int RoundToEven(float n)
        {
            return Mathf.RoundToInt(n / 2) * 2;
        }
    }
}
