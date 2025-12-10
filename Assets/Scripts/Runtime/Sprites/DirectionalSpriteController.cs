using System;
using System.Collections.Generic;
using UnityEngine;

public class DirectionalSpriteController : MonoBehaviour
{
    [Header("Sprite Configuration")]
    [SerializeField] private SpriteRenderer _spriteRenderer;
    [SerializeField] private AnimationData[] _animations;

    [Header("Performance Settings")]
    [SerializeField] private float _directionUpdateRate = 10f;
    [SerializeField] private float _minAngleChange = 15f;

    private Camera _mainCamera;
    private Transform _characterTransform;

    // Cache 3D: [animazione][frame][direzione] = Sprite
    private Dictionary<string, Sprite[,]> _spriteCache;

    // Sistema di animazione
    private string _currentAnimation = "";
    private int _currentFrame = 0;
    private int _currentDirection = 0;
    private float _animationTimer = 0f;
    private bool _isPlaying = false;

    private float _lastDirectionUpdate;
    private float _lastAngle;

    private SpriteAtlasHelper _atlasHelper;

    void Start()
    {
        InitializeComponents();
        InitializeSpriteCache();

        if (_animations.Length > 0)
            PlayAnimation(_animations[0].animationName);
    }

    #region Initializations
    private void InitializeComponents()
    {
        _mainCamera = Camera.main;
        _characterTransform = transform;
        _atlasHelper = GetComponent<SpriteAtlasHelper>();
        _spriteCache = new Dictionary<string, Sprite[,]>();
    }

    private void InitializeSpriteCache()
    {
        foreach (AnimationData animData in _animations)
        {
            LoadAnimationToCache(animData);
        }

        Debug.Log($"Cache inizializzata con {_spriteCache.Count} animazioni");
    }

    private void LoadAnimationToCache(AnimationData animData)
    {
        // Crea matrice [frame][direzione]
        Sprite[,] animationSprites = new Sprite[animData.frameCount, 8];

        // Carica tutte le sprite per questa animazione
        for (int frame = 0; frame < animData.frameCount; frame++)
        {
            foreach (EDirection direction in Enum.GetValues(typeof(EDirection))) 
            {
                string spriteName = GetSpriteName(animData.animationName, frame, direction);
                animationSprites[frame, (int) direction] = _atlasHelper.GetSprite(spriteName);

                if (animationSprites[frame, (int) direction] == null)
                {
                    Debug.LogWarning($"Sprite mancante: {spriteName}");
                }
            }
        }

        _spriteCache[animData.animationName] = animationSprites;
    }

    private string GetSpriteName(string animation, int frame, EDirection direction)
    {
        // Convezione: "idle_dir_N_0", "walk_dir_SE_0", ecc.
        return $"{animation}_dir_{direction.GetCode()}_{frame}";
    }
    #endregion

    void Update()
    {
        UpdateDirection();
        UpdateAnimation();
        UpdateSprite();
    }

    #region Update
    private void UpdateDirection()
    {
        // Throttling per l'update della direzione
        if (Time.time - _lastDirectionUpdate < 1f / _directionUpdateRate)
            return;

        if (_mainCamera == null) return;

        Vector3 directionToCamera = _mainCamera.transform.position - _characterTransform.position;
        Vector3 characterForward = _characterTransform.forward;

        float angle = GetAngleWithAtan2(directionToCamera, characterForward);

        // Controlla se il cambiamento è significativo
        if (Mathf.Abs(angle - _lastAngle) < _minAngleChange)
            return;

        int newDirection = GetDirectionFromAngle(angle);

        if (newDirection != _currentDirection)
        {
            _currentDirection = newDirection;
            _lastAngle = angle;
        }

        _lastDirectionUpdate = Time.time;
    }

    private float GetAngleWithAtan2(Vector3 directionToCamera, Vector3 characterForward)
    {
        Vector2 cameraDir2D = new Vector2(directionToCamera.x, directionToCamera.z).normalized;
        Vector2 charForward2D = new Vector2(characterForward.x, characterForward.z).normalized;

        float angle = Mathf.Atan2(cameraDir2D.x, cameraDir2D.y) - Mathf.Atan2(charForward2D.x, charForward2D.y);

        angle *= Mathf.Rad2Deg;

        if (angle < 0) angle += 360f;

        return angle;
    }

    private int GetDirectionFromAngle(float angle)
    {
        // Metodo veloce: divide in 8 settori
        float normalizedAngle = (angle + 22.5f) % 360f;
        int intDirection = Mathf.FloorToInt(normalizedAngle / 45f);
        return intDirection;
    }

    private void UpdateAnimation()
    {
        if (!_isPlaying || string.IsNullOrEmpty(_currentAnimation)) return;

        AnimationData currentAnimData = GetAnimationData(_currentAnimation);
        if (currentAnimData == null) return;

        _animationTimer += Time.deltaTime;
        float frameDuration = 1f / currentAnimData.frameRate;

        if (_animationTimer >= frameDuration)
        {
            _animationTimer -= frameDuration;
            _currentFrame++;

            if (_currentFrame >= currentAnimData.frameCount)
            {
                if (currentAnimData.loop)
                {
                    _currentFrame = 0;
                }
                else
                {
                    _currentFrame = currentAnimData.frameCount - 1;
                    _isPlaying = false;
                }
            }
        }
    }

    private void UpdateSprite()
    {
        if (_spriteCache.ContainsKey(_currentAnimation))
        {
            Sprite[,] animationSprites = _spriteCache[_currentAnimation];

            // Bounds checking
            if (_currentFrame < animationSprites.GetLength(0) && _currentDirection < animationSprites.GetLength(1))
            {
                Sprite targetSprite = animationSprites[_currentFrame, _currentDirection];

                if (targetSprite != null && _spriteRenderer.sprite != targetSprite)
                {
                    _spriteRenderer.sprite = targetSprite;
                }
            }
        }
    }
    #endregion

    #region Public APIs
    // API pubblica per controllare le animazioni
    public void PlayAnimation(string animationName)
    {
        if (!_spriteCache.ContainsKey(animationName))
        {
            Debug.LogError($"Animazione '{animationName}' non trovata nella cache!");
            return;
        }

        if (_currentAnimation != animationName)
        {
            _currentAnimation = animationName;
            _currentFrame = 0;
            _animationTimer = 0f;
        }

        _isPlaying = true;
    }

    public void StopAnimation()
    {
        _isPlaying = false;
    }

    public void PauseAnimation()
    {
        _isPlaying = false;
    }

    public void ResumeAnimation()
    {
        _isPlaying = true;
    }

    public bool IsPlaying() => _isPlaying;
    public string GetCurrentAnimation() => _currentAnimation;
    public int GetCurrentFrame() => _currentFrame;
    public int GetCurrentDirection() => _currentDirection;

    AnimationData GetAnimationData(string animName)
    {
        foreach (var anim in _animations)
        {
            if (anim.animationName == animName)
                return anim;
        }
        return null;
    }

    public void SetActive(bool active)
    {
        enabled = active;
    }
    #endregion
}
