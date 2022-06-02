namespace TextureSource
{
    using UnityEngine;

    public abstract class BaseTextureSource : ScriptableObject, ITextureSource
    {
        public abstract bool DidUpdateThisFrame { get; }
        public abstract Texture Texture { get; }
        public abstract void Start();
        public abstract void Stop();
        public abstract void Next();
    }
}
