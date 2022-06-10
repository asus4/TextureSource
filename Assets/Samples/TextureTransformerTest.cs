using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TextureSource;

public class TextureTransformerTest : MonoBehaviour
{
    [SerializeField]
    private Texture2D source;

    [SerializeField]
    private RawImage frame;

    [SerializeField]
    private Vector2Int size = new Vector2Int(512, 512);

    [SerializeField]
    private Vector2 offset = new Vector2(0, 0);

    [SerializeField]
    private Vector2 scale = new Vector2(1, 1);

    [SerializeField]
    [Range(-360, 360)]
    private float rotation = 0;

    private TextureTransformer transformer;

    private void Start()
    {
        transformer = new TextureTransformer(size.x, size.y);
    }

    private void OnDestroy()
    {
        transformer?.Dispose();
    }

    private void Update()
    {
        frame.texture = transformer.Transform(source, offset, rotation, scale);
    }
}
