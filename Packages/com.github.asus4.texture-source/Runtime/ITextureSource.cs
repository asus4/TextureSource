namespace TextureSource
{
    using UnityEngine;

    public interface ITextureSource
    {
        bool DidUpdateThisFrame { get; }
        Texture Texture { get; }

        void Start();
        void Stop();
        void Next();
    }
}
