namespace TextureSource
{
    using UnityEngine;

    /// <summary>
    /// Abstract class for the source.
    /// </summary>
    public abstract class BaseTextureSource : ScriptableObject, ITextureSource
    {
        public abstract bool DidUpdateThisFrame { get; }
        public abstract Texture Texture { get; }
        public abstract void Start();
        public abstract void Stop();
        public abstract void Next();

        /// <summary>
        /// The transform matrix that is applied in the <see cref="TextureTransformer"/>.
        /// Defaults to identity matrix.
        /// </summary>
        public virtual Matrix4x4 TransformMatrix => Matrix4x4.identity;

        /// <summary>
        /// The size after transform. Defaults to the texture size.
        /// </summary>
        public virtual Vector2Int TransformSize
        {
            get
            {
                Texture tex = Texture;
                return tex == null ? Vector2Int.zero : new Vector2Int(tex.width, tex.height);
            }
        }
    }
}
