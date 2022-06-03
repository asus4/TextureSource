namespace TextureSource
{
    using UnityEngine;
    using UnityEngine.Events;

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

        [Tooltip("Event called when texture updated")]
        public TextureEvent OnTexture = new TextureEvent();
        [Tooltip("Event called when the aspect ratio changed")]
        public AspectChangeEvent OnAspectChange = new AspectChangeEvent();

        private ITextureSource activeSource;
        private float aspect = float.NegativeInfinity;

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
        }

        private void Update()
        {
            if (!activeSource.DidUpdateThisFrame)
            {
                return;
            }

            Texture tex = activeSource.Texture;
            OnTexture?.Invoke(tex);

            float aspect = (float)tex.width / tex.height;
            if (aspect != this.aspect)
            {
                OnAspectChange?.Invoke(aspect);
                this.aspect = aspect;
            }
        }

        public void Next()
        {
            activeSource?.Next();
        }
    }
}
