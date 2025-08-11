using System;
using UnityEngine;
using UnityEngine.U2D;

public class AtlasChecker : MonoBehaviour
{
    [SerializeField] private SpriteAtlas _atlas;

    private SpriteRenderer _spriteRenderer;

    private void Awake()
    {
        _spriteRenderer = GetComponent<SpriteRenderer>();
    }

    private void Start()
    {
        if (_atlas.spriteCount == 0) 
        {
            Debug.LogWarning($"Nessuna sprite trovata nell'atlas {_atlas.name}");
            return; 
        }

        Sprite[] sprites = new Sprite[_atlas.spriteCount];
        _atlas.GetSprites(sprites);

        _spriteRenderer.sprite = _atlas.GetSprite("cows_sprites_0");
    }
}
