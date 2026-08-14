using System;
using System.Collections.Generic;
using System.Linq;
using AYellowpaper.SerializedCollections;
using PrimeTween;
using UnityEngine;

public class DirectionalSpriteController : MonoBehaviour
{
    [Header("Sprite Configuration")]
    [SerializeField] private SpriteRenderer _spriteRenderer;
    [SerializeField] private SerializedDictionary<EAnimation, AnimationConfig> _animations;

    [Header("Performance Settings")]
    [SerializeField] private float _directionUpdateRate = 10f;
    [SerializeField] private float _minAngleChange = 15f;

    [Header("Anchors")]
    [SerializeField] private MainCameraAnchorSO _mainCameraAnchor;

    public event Action<EAnimation> OnAnimationComplete;

    private Camera _mainCamera;
    private Transform _characterTransform;

    private void OnEnable()
    {
        if (_mainCameraAnchor == null)
        {
            Debug.LogError($"{nameof(DirectionalSpriteController)}: nessun {nameof(MainCameraAnchorSO)} assegnato.", this);
            return;
        }

        _mainCamera = _mainCameraAnchor.Value;
        _mainCameraAnchor.OnValueChanged += HandleCameraChanged;
    }

    private void OnDisable()
    {
        if (_mainCameraAnchor != null) _mainCameraAnchor.OnValueChanged -= HandleCameraChanged;
    }

    private void HandleCameraChanged(Camera camera) => _mainCamera = camera;

    private Dictionary<EAnimation, Sprite[,]> _spriteCache;

    private EAnimation? _currentAnimation;
    private int _currentFrame = 0;
    private int _currentDirection = 0;
    private float _animationTimer = 0f;
    private bool _isPlaying = false;

    private float _lastDirectionUpdate;
    private float _lastAngle;

    private SpriteAtlasHelper _atlasHelper;
    private Tween _colorTween;

    private bool _isInitialized;

    void Start()
    {
        if (!InitializeComponents()) return;
        if (!InitializeSpriteCache()) return;

        _isInitialized = true;

        if (_animations.Count > 0)
            PlayAnimation(_animations.Keys.First());
    }

    #region Initializations
    private bool InitializeComponents()
    {
        _characterTransform = transform;
        _spriteCache = new Dictionary<EAnimation, Sprite[,]>();

        if (_spriteRenderer == null)
            _spriteRenderer = GetComponent<SpriteRenderer>();

        if (_spriteRenderer == null)
        {
            Debug.LogError($"[DirectionalSpriteController] SpriteRenderer mancante su '{name}': animazioni disabilitate.", this);
            return false;
        }

        _atlasHelper = GetComponent<SpriteAtlasHelper>();

        if (_atlasHelper == null)
        {
            Debug.LogError($"[DirectionalSpriteController] SpriteAtlasHelper mancante su '{name}': animazioni disabilitate.", this);
            return false;
        }

        if (_animations == null || _animations.Count == 0)
        {
            Debug.LogWarning($"[DirectionalSpriteController] Nessuna animazione configurata su '{name}'.", this);
            return false;
        }

        return true;
    }

    private bool InitializeSpriteCache()
    {
        try
        {
            foreach (var kvp in _animations)
            {
                LoadAnimationToCache(kvp.Key, kvp.Value);
            }
        }
        catch (Exception e)
        {
            Debug.LogError($"[DirectionalSpriteController] Errore caricando la sprite cache su '{name}': {e}", this);
            return false;
        }

        Debug.Log($"Cache inizializzata con {_spriteCache.Count} animazioni");
        return true;
    }

    private void LoadAnimationToCache(EAnimation anim, AnimationConfig config)
    {
        Sprite[,] animationSprites = new Sprite[Mathf.Max(config.frameCount, 0), 8];

        int missingCount = 0;
        string firstMissingName = null;

        for (int frame = 0; frame < config.frameCount; frame++)
        {
            foreach (EDirection direction in Enum.GetValues(typeof(EDirection)))
            {
                string spriteName = GetSpriteName(anim, frame, direction);
                animationSprites[frame, (int)direction] = _atlasHelper.GetSprite(spriteName);

                if (animationSprites[frame, (int)direction] == null)
                {
                    missingCount++;
                    firstMissingName ??= spriteName;
                }
            }
        }

        if (missingCount > 0)
            Debug.LogWarning($"[DirectionalSpriteController] '{name}': {missingCount}/{config.frameCount * 8} sprite mancanti per l'animazione {anim} (es. {firstMissingName}).", this);

        _spriteCache[anim] = animationSprites;
    }

    private string GetSpriteName(EAnimation animation, int frame, EDirection direction)
    {
        return $"{animation.GetCode()}_dir_{direction.GetCode()}_{frame}";
    }
    #endregion

    void Update()
    {
        if (!_isInitialized) return;

        UpdateDirection();
        UpdateAnimation();
        UpdateSprite();
    }

    #region Update
    private void UpdateDirection()
    {
        if (Time.time - _lastDirectionUpdate < 1f / _directionUpdateRate)
            return;

        if (_mainCamera == null) return;

        Vector3 directionToCamera = _mainCamera.transform.position - _characterTransform.position;
        Vector3 characterForward = _characterTransform.forward;

        float angle = GetAngleWithAtan2(directionToCamera, characterForward);

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
        float normalizedAngle = (angle + 22.5f) % 360f;
        return Mathf.FloorToInt(normalizedAngle / 45f);
    }

    private void UpdateAnimation()
    {
        if (!_isPlaying || !_currentAnimation.HasValue) return;

        if (!_animations.TryGetValue(_currentAnimation.Value, out var currentAnimData)) return;

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
                    OnAnimationComplete?.Invoke(_currentAnimation.Value);
                }
            }
        }
    }

    private void UpdateSprite()
    {
        if (!_currentAnimation.HasValue) return;
        if (!_spriteCache.TryGetValue(_currentAnimation.Value, out var animationSprites)) return;

        if (_currentFrame < animationSprites.GetLength(0) && _currentDirection < animationSprites.GetLength(1))
        {
            Sprite targetSprite = animationSprites[_currentFrame, _currentDirection];
            if (targetSprite != null && _spriteRenderer.sprite != targetSprite)
                _spriteRenderer.sprite = targetSprite;
        }
    }
    #endregion

    #region Public APIs
    public void PlayAnimation(EAnimation animation)
    {
        if (!_isInitialized) return;

        if (!_spriteCache.ContainsKey(animation))
        {
            Debug.LogError($"Animazione '{animation}' non trovata nella cache!");
            return;
        }

        if (_currentAnimation != animation)
        {
            _currentAnimation = animation;
            _currentFrame = 0;
            _animationTimer = 0f;
        }

        _isPlaying = true;
    }

    public void StopAnimation()  => _isPlaying = false;
    public void PauseAnimation() => _isPlaying = false;
    public void ResumeAnimation() => _isPlaying = true;

    public bool IsPlaying() => _isPlaying;
    public EAnimation? GetCurrentAnimation() => _currentAnimation;
    public int GetCurrentFrame() => _currentFrame;
    public int GetCurrentDirection() => _currentDirection;

    public void SetActive(bool active) => enabled = active;

    public void SetDeadVisual()
    {
        if (_spriteRenderer == null) return;

        _colorTween.Stop();
        _colorTween = Tween.Color(_spriteRenderer, new Color(0.3f, 0.3f, 0.3f, 1f), duration: 0.3f);
    }

    public void ResetVisual()
    {
        if (_spriteRenderer == null) return;

        _colorTween.Stop();
        _colorTween = Tween.Color(_spriteRenderer, Color.white, duration: 0.3f);
    }
    #endregion
}
