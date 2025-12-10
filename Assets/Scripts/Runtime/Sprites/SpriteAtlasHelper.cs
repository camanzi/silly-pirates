using System.Collections.Generic;
using UnityEngine;
using UnityEngine.U2D;

public class SpriteAtlasHelper : MonoBehaviour
{
    [Header("Atlas Configuration")]
    [SerializeField] private SpriteAtlas _spriteAtlas;

    // Cache per evitare lookup ripetuti sull'atlas
    private Dictionary<string, Sprite> _atlasCache = new Dictionary<string, Sprite>();

    void Awake()
    {
        PreloadAtlasCache();
    }

    void PreloadAtlasCache()
    {
        if (_spriteAtlas == null) return;

        Sprite[] allSprites = new Sprite[_spriteAtlas.spriteCount];
        _spriteAtlas.GetSprites(allSprites);

        foreach (var sprite in allSprites)
        {
            if (sprite != null)
            {
                _atlasCache[sprite.name] = sprite;
            }
        }

        Debug.Log($"Atlas cache precaricata con {_atlasCache.Count} sprite");
    }

    public Sprite GetSprite(string spriteName)
    {
        if (_atlasCache.ContainsKey(spriteName))
        {
            return _atlasCache[spriteName];
        }

        if (_spriteAtlas != null)
        {
            Sprite sprite = _spriteAtlas.GetSprite(spriteName);
            if (sprite != null)
            {
                _atlasCache[spriteName] = sprite;
            }
            return sprite;
        }

        return null;
    }

    public bool HasSprite(string spriteName)
    {
        return _atlasCache.ContainsKey(spriteName);
    }

    public int GetCachedSpriteCount()
    {
        return _atlasCache.Count;
    }
}
