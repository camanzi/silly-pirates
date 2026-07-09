using System.Collections.Generic;
using PrimeTween;
using UnityEngine;
using UnityEngine.UIElements;

public class ActionPointController : MonoBehaviour
{
    [Header("UI Assets")]
    [SerializeField] private Sprite _apAvailableSprite;
    [SerializeField] private Sprite _apConsumedSprite;

    [Header("Dependences")]
    [SerializeField] private UIDocument _hudDocument;
    [SerializeField] private BoolEventChannel _targetingStateChannel;

    private VisualElement _apContainer;
    private List<ActionPointElement> _apElements = new();
    private int _currentHoverCost = 0;
    private bool _isInTargetingState = false;

    private void Awake()
    {
        var root = _hudDocument.rootVisualElement;
        _apContainer = root.Q<VisualElement>("action-points-container");
    }

    public void OnTargetingStateChanged(bool isTargeting)
    {
        _isInTargetingState = isTargeting;
        if (!isTargeting) HandleAbilityHoverPreview(AbilityCostPayload.Empty);
    }

    public void HandleAgentActivated(ITurnAgent agent)
    {
        if (!agent.CompareTag("Player")) 
        {
            ClearActionPoints();
            return;
        }

        int maxAp = agent.AgentData.MaxActionPointsPerTurn;
        int currentAp = agent.RemainingActionPoints;

        SpawnActionPoints(maxAp, currentAp);
    }

    private void SpawnActionPoints(int maxAp, int currentAp)
    {
        ClearActionPoints();

        for (int i = 0; i < maxAp; i++)
        {
            var apElement = new ActionPointElement();
            
            apElement.Setup(_apAvailableSprite, _apConsumedSprite);
            
            _apContainer.Add(apElement);
            _apElements.Add(apElement);

            bool isConsumed = i >= currentAp;
            apElement.SetState(isConsumed);

            apElement.style.scale = new StyleScale(new Scale(Vector3.zero));
            Tween.Custom(Vector3.zero, Vector3.one, 0.3f, ease: Ease.OutBack, 
                onValueChange: v => apElement.style.scale = new StyleScale(new Scale(new Vector3(v.x, v.y, 1f))));
        }
    }

    public void UpdateActionPoints(int newCurrentAp)
    {
        for (int i = 0; i < _apElements.Count; i++)
        {
            bool shouldBeConsumed = i >= newCurrentAp;

            if (_apElements[i].IsConsumed != shouldBeConsumed)
            {
                var apElement = _apElements[i];

                apElement.SetState(shouldBeConsumed);

                if (shouldBeConsumed)
                {
                    Tween.Custom(apElement.resolvedStyle.scale.value, new Vector3(1.3f, 1.3f, 1f), duration: 0.15f, ease: Ease.OutQuad, cycleMode: CycleMode.Rewind, cycles: 2, onValueChange: newVal => {
                        apElement.style.scale = new StyleScale(new Scale(new Vector3(newVal.x, newVal.y, 1f)));
                    });
                }
                else
                {
                    apElement.style.scale = new StyleScale(new Scale(Vector3.zero));

                    Tween.Custom(Vector3.zero, Vector3.one, duration: 0.3f, ease: Ease.OutBack, onValueChange: newVal => {
                        apElement.style.scale = new StyleScale(new Scale(new Vector3(newVal.x, newVal.y, 1f)));
                    });
                }
            }
        }

        HandleAbilityHoverPreview(new AbilityCostPayload(_currentHoverCost, 0));
    }

    public void HandleAbilityHoverPreview(AbilityCostPayload payload)
    {
        if (_isInTargetingState) return;
        _currentHoverCost = payload.ApCost;

        foreach (var el in _apElements)
            el.StopHoverGlow();

        if (payload.ApCost <= 0) return;

        int glowCount = 0;
        for (int i = _apElements.Count - 1; i >= 0; i--)
        {
            var el = _apElements[i];
            if (!el.IsConsumed && glowCount < payload.ApCost)
            {
                el.StartHoverGlow();
                glowCount++;
            }
        }
    }

    private void ClearActionPoints()
    {
        _apContainer.Clear();
        _apElements.Clear();
    }
}