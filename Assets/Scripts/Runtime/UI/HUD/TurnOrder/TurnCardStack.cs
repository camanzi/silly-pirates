using System.Collections.Generic;
using PrimeTween;
using UnityEngine;
using UnityEngine.UIElements;

[UxmlElement]
public partial class TurnCardStack : VisualElement
{
    private static readonly string USS_CLASS_STACK = "turn-card-stack";
    private static readonly string USS_CLASS_SUB_CARD = "turn-card-stack__sub-card";

    private const float SubCardOffsetX = 12f;
    private const float WobbleAmount = 15f;
    private const float WobbleDuration = .75f;
    private const float ShakeAmplitude = 20f;
    private const float ShakeDuration = 0.8f;
    private const int ShakeCycles = 4;
    private const float SlideDuration = 0.35f;

    private TurnCard _mainCard;
    private readonly List<TurnCard> _subCards = new();

    private bool _isHovered;
    private Tween _wobbleTween;
    private Tween _shakeTween;
    private Tween _slideTween;

    public ITurnAgent Agent { get; private set; }

    public TurnCardStack()
    {
        AddToClassList(USS_CLASS_STACK);
    }

    public void Bind(EntityTurnState state, int subTurnCount, bool isActive, VisualTreeAsset cardTemplate)
    {
        if (Agent?.Health != null)
        {
            Agent.Health.OnTakeDamage -= PlayDamageShake;
            Agent.Health.OnHpChanged -= HandleHpChanged;
        }

        Agent = state.Agent;

        if (_mainCard == null)
        {
            _mainCard = new TurnCard();
            cardTemplate.CloneTree(_mainCard);
            _mainCard.InitElements();
        }

        _mainCard.Data = new CharacterTurnData(
            state.Agent,
            state.CurrentAV,
            state.Agent.RenderingData.TurnAgentIcon,
            false
        );
        _mainCard.IsActive = isActive;

        // Grow/shrink the sub-card pool to match subTurnCount.
        while (_subCards.Count < subTurnCount)
        {
            var subCard = new TurnCard();
            cardTemplate.CloneTree(subCard);
            subCard.InitElements();
            subCard.AddToClassList(USS_CLASS_SUB_CARD);
            _subCards.Add(subCard);
        }
        while (_subCards.Count > subTurnCount)
        {
            var surplus = _subCards[_subCards.Count - 1];
            _subCards.RemoveAt(_subCards.Count - 1);
            surplus.Unbind();
            surplus.RemoveFromHierarchy();
        }

        for (int i = 0; i < _subCards.Count; i++)
        {
            var subCard = _subCards[i];
            subCard.Data = new CharacterTurnData(
                state.Agent,
                state.CurrentAV,
                state.Agent.RenderingData.TurnAgentIcon,
                true
            );
            subCard.IsActive = isActive;
            subCard.style.translate = new StyleTranslate(new Translate((i + 1) * SubCardOffsetX, 0f));
        }

        // Paint order: farthest sub-card first, nearest sub-card next, main card last (on top).
        for (int i = _subCards.Count - 1; i >= 0; i--)
            Add(_subCards[i]);
        Add(_mainCard);

        style.marginRight = subTurnCount * SubCardOffsetX;

        if (Agent?.Health != null)
        {
            Agent.Health.OnTakeDamage += PlayDamageShake;
            Agent.Health.OnHpChanged += HandleHpChanged;
            HandleHpChanged(Agent.Health.CurrentHp);
        }
    }

    public void Unbind()
    {
        _wobbleTween.Stop();
        _shakeTween.Stop();
        _slideTween.Stop();

        if (Agent?.Health != null)
        {
            Agent.Health.OnTakeDamage -= PlayDamageShake;
            Agent.Health.OnHpChanged -= HandleHpChanged;
        }
        Agent = null;

        style.translate = StyleKeyword.Initial;

        if (_mainCard != null)
        {
            _mainCard.Unbind();
            _mainCard.RemoveFromHierarchy();
            _mainCard = null;
        }

        foreach (var subCard in _subCards)
        {
            subCard.Unbind();
            subCard.RemoveFromHierarchy();
        }
        _subCards.Clear();
    }

    public void SetHovered(bool hovered)
    {
        _isHovered = hovered;
        if (_shakeTween.isAlive || _slideTween.isAlive) return;
        _wobbleTween.Stop();
        style.translate = StyleKeyword.Initial;
        if (!hovered) return;
        _wobbleTween = Tween.Custom(this, 0, WobbleAmount, WobbleDuration,
            (self, v) => self.style.translate = new StyleTranslate(new Translate(0f, v)),
            cycles: -1, cycleMode: CycleMode.Rewind, ease: Ease.InOutSine);
    }

    public void PlaySlideFrom(float offsetX)
    {
        // Damage shake always wins; let it play out undisturbed.
        if (_shakeTween.isAlive) return;
        _wobbleTween.Stop();
        _slideTween.Stop();
        style.translate = new StyleTranslate(new Translate(offsetX, 0f));
        _slideTween = Tween.Custom(this, offsetX, 0f, SlideDuration,
            (self, v) => self.style.translate = new StyleTranslate(new Translate(v, 0f)),
            ease: Ease.OutQuad);
    }

    private void HandleHpChanged(float currentHp)
    {
        float maxHp = Agent.Health.MaxHp;
        _mainCard?.UpdateHealth(currentHp, maxHp);
        foreach (var subCard in _subCards)
            subCard.UpdateHealth(currentHp, maxHp);
    }

    private void PlayDamageShake()
    {
        // Shake takes priority over both hover-wobble and an in-flight slide.
        _wobbleTween.Stop();
        _slideTween.Stop();
        _shakeTween = Tween.Custom(this, 0f, 1f, ShakeDuration, (self, t) =>
        {
            float shake = Mathf.Sin(t * Mathf.PI * 2f * ShakeCycles) * (1f - t) * ShakeAmplitude;
            self.style.translate = new StyleTranslate(new Translate(shake, 0f));
        }, ease: Ease.Linear).OnComplete(() => SetHovered(_isHovered));
    }
}
