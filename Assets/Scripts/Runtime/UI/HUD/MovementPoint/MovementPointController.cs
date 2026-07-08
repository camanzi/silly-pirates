using System.Collections.Generic;
using PrimeTween;
using UnityEngine;
using UnityEngine.UIElements;

public class MovementPointController : MonoBehaviour
{
    [Header("UI Assets")]
    [SerializeField] private Sprite _mpAvailableSprite;
    [SerializeField] private Sprite _mpConsumedSprite;

    [Header("Dependences")]
    [SerializeField] private UIDocument _hudDocument;

    private VisualElement _mpContainer;
    private List<ActionPointElement> _mpElements = new();
    private IMovable _cachedMovable;

    private void Awake()
    {
        var root = _hudDocument.rootVisualElement;
        _mpContainer = root.Q<VisualElement>("movement-points-container");
    }

    public void HandleAgentActivated(ITurnAgent agent)
    {
        UnsubscribeFromAgent();

        if (agent is not IMovable movable)
        {
            ClearMovementPoints();
            return;
        }

        _cachedMovable = movable;

        int maxMp = movable.RemainingMovementPoints;
        int currentMp = movable.RemainingMovementPoints;

        SpawnMovementPoints(maxMp, currentMp);

        movable.OnMovementPointsChanged.OnEventRaised += UpdateMovementPoints;
    }

    private void SpawnMovementPoints(int maxMp, int currentMp)
    {
        ClearMovementPoints();

        for (int i = 0; i < maxMp; i++)
        {
            var mpElement = new ActionPointElement();

            mpElement.Setup(_mpAvailableSprite, _mpConsumedSprite);

            _mpContainer.Add(mpElement);
            _mpElements.Add(mpElement);

            bool isConsumed = i >= currentMp;
            mpElement.SetState(isConsumed);

            mpElement.style.scale = new StyleScale(new Scale(Vector3.zero));
            Tween.Custom(Vector3.zero, Vector3.one, 0.3f, ease: Ease.OutBack,
                onValueChange: v => mpElement.style.scale = new StyleScale(new Scale(new Vector3(v.x, v.y, 1f))));
        }
    }

    public void UpdateMovementPoints(int newCurrentMp)
    {
        for (int i = 0; i < _mpElements.Count; i++)
        {
            bool shouldBeConsumed = i >= newCurrentMp;

            if (_mpElements[i].IsConsumed != shouldBeConsumed)
            {
                var mpElement = _mpElements[i];

                mpElement.SetState(shouldBeConsumed);

                if (shouldBeConsumed)
                {
                    Tween.Custom(mpElement.resolvedStyle.scale.value, new Vector3(1.3f, 1.3f, 1f), duration: 0.15f, ease: Ease.OutQuad, cycleMode: CycleMode.Rewind, cycles: 2, onValueChange: newVal => {
                        mpElement.style.scale = new StyleScale(new Scale(new Vector3(newVal.x, newVal.y, 1f)));
                    });
                }
                else
                {
                    mpElement.style.scale = new StyleScale(new Scale(Vector3.zero));

                    Tween.Custom(Vector3.zero, Vector3.one, duration: 0.3f, ease: Ease.OutBack, onValueChange: newVal => {
                        mpElement.style.scale = new StyleScale(new Scale(new Vector3(newVal.x, newVal.y, 1f)));
                    });
                }
            }
        }
    }

    private void ClearMovementPoints()
    {
        _mpContainer.Clear();
        _mpElements.Clear();
    }

    private void UnsubscribeFromAgent()
    {
        if (_cachedMovable != null)
            _cachedMovable.OnMovementPointsChanged.OnEventRaised -= UpdateMovementPoints;
        _cachedMovable = null;
    }

    private void OnDestroy()
    {
        UnsubscribeFromAgent();
    }
}
