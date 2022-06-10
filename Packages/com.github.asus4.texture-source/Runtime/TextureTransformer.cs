namespace TextureSource
{
    using UnityEngine;

    public class TextureTransformer : System.IDisposable
    {
        private static ComputeShader compute;
        private static int kernel;
        private static readonly int _InputTex = Shader.PropertyToID("_InputTex");
        private static readonly int _OutputTex = Shader.PropertyToID("_OutputTex");
        private static readonly int _OutputTexSize = Shader.PropertyToID("_OutputTexSize");
        private static readonly int _TransformMatrix = Shader.PropertyToID("_TransformMatrix");

        private static readonly Matrix4x4 PopMatrix = Matrix4x4.Translate(new Vector3(0.5f, 0.5f, 0));
        private static readonly Matrix4x4 PushMatrix = Matrix4x4.Translate(new Vector3(-0.5f, -0.5f, 0));

        public readonly RenderTexture texture;

        public TextureTransformer(int width, int height)
        {
            var desc = new RenderTextureDescriptor(width, height, RenderTextureFormat.ARGB32)
            {
                enableRandomWrite = true,
                useMipMap = false,
                depthBufferBits = 0,
            };
            texture = new RenderTexture(desc);
            texture.Create();

            if (compute == null)
            {
                compute = Resources.Load<ComputeShader>("com.github.asus4.texture-source/TextureTransform");
                kernel = compute.FindKernel("TextureTransform");
            }

            compute.SetTexture(kernel, _OutputTex, texture);
            compute.SetInts(_OutputTexSize, texture.width, texture.height);
        }

        public void Dispose()
        {
            texture.Release();
            Object.Destroy(texture);
        }

        public RenderTexture Transform(Texture input, Matrix4x4 t)
        {
            compute.SetTexture(kernel, _InputTex, input);
            compute.SetMatrix(_TransformMatrix, t);
            compute.Dispatch(kernel, Mathf.CeilToInt(texture.width / 8f), Mathf.CeilToInt(texture.height / 8f), 1);
            return texture;
        }

        public RenderTexture Transform(Texture input, Vector2 offset, float eulerRotation, Vector2 scale)
        {
            Matrix4x4 trs = Matrix4x4.TRS(
                new Vector3(-offset.x, -offset.y, 0),
                Quaternion.Euler(0, 0, -eulerRotation),
                new Vector3(1f / scale.x, 1f / scale.y, 1));
            return Transform(input, PopMatrix * trs * PushMatrix);
        }
    }
}
