using System;
using System.Collections.Generic;
using System.Threading;
using PrimeTween;
using UnityEngine;
using UnityEngine.UIElements;

/// <summary>
/// Main menu. Vive nella scena MainMenu, non nella persistente: ogni scena possiede la propria UI,
/// così non resta stato di schermata da azzerare fra un caricamento e l'altro.
///
/// Non carica nulla da sé: alza un canale e basta. Chi risponde è SceneFlowDirector, nella scena
/// persistente, che è l'unico a sapere cosa è caricato in questo momento.
///
/// Qui vive anche l'orchestrazione della sequenza di apertura, perché è l'unico punto che vede sia
/// l'illustrazione (delegata a <see cref="MainMenuDrakeAnimator"/>) sia le voci di menu: il drago si
/// compone, poi entrano le voci, poi si accettano input.
/// </summary>
public class MainMenuController : MonoBehaviour
{
    [SerializeField] private UIDocument _document;
    [SerializeField] private MainMenuDrakeAnimator _drakeAnimator;
    [SerializeField] private VoidEventChannel _startCombatRequested;

    private VisualElement _root;
    private VisualElement _stage;
    private VisualElement _itemsRoot;
    private VisualElement _marker;

    private MainMenuSelection _selection;
    private readonly List<MainMenuSelection.Entry> _entries = new();

    private Sequence _menuEntry;
    private bool _introStarted;
    private bool _transitionRequested;

    private void OnEnable()
    {
        _root = _document != null ? _document.rootVisualElement : null;

        if (_root == null)
        {
            Debug.LogError($"[{nameof(MainMenuController)}] nessun UIDocument: il menu non può funzionare.", this);
            return;
        }

        _stage = _root.Q<VisualElement>("menu-stage");
        _itemsRoot = _root.Q<VisualElement>("menu-items");
        _marker = _root.Q<VisualElement>("menu-marker");

        Label play = _root.Q<Label>("menu-play");
        Label options = _root.Q<Label>("menu-options");
        Label quit = _root.Q<Label>("menu-quit");

        VisualElement playLine = _root.Q<VisualElement>("underline-play");
        VisualElement optionsLine = _root.Q<VisualElement>("underline-options");
        VisualElement quitLine = _root.Q<VisualElement>("underline-quit");

        if (_stage == null || _itemsRoot == null || _marker == null
            || play == null || options == null || quit == null
            || playLine == null || optionsLine == null || quitLine == null)
        {
            Debug.LogError(
                $"[{nameof(MainMenuController)}] il documento non ha la struttura attesa " +
                "(menu-stage / menu-items / menu-marker / le tre voci con la loro sottolineatura): " +
                "il menu resta inerte.", this);
            return;
        }

        _entries.Clear();
        _entries.Add(new MainMenuSelection.Entry(play, playLine, selectable: true, StartCombat));
        // Options è disegnata come da mockup ma non porta da nessuna parte: non selezionabile, così
        // le frecce la scavalcano invece di fermarsi su una voce che non fa nulla. La sottolineatura
        // ce l'ha comunque: il giorno in cui diventerà attiva non c'è nulla da aggiungere.
        _entries.Add(new MainMenuSelection.Entry(options, optionsLine, selectable: false, null));
        _entries.Add(new MainMenuSelection.Entry(quit, quitLine, selectable: true, Quit));

        MainMenuMotionSO motionAsset = _drakeAnimator != null ? _drakeAnimator.Motion : null;
        _selection = new MainMenuSelection(_marker, _entries,
            motionAsset != null ? motionAsset.Selection : MainMenuMotionSO.SelectionMotion.Default);

        // Nascosti prima che il pannello disegni il primo frame: 'visibility' e non 'display' perché
        // il layout deve restare misurabile — è da lì che il marker ricava dove andare e quanto è
        // larga la sottolineatura. Hidden blocca anche il picking, quindi durante l'intro non si può
        // cliccare una voce invisibile.
        _itemsRoot.style.visibility = Visibility.Hidden;
        _itemsRoot.style.opacity = 0f;
        _marker.style.visibility = Visibility.Hidden;
        _marker.style.opacity = 0f;

        _drakeAnimator?.Bind(_stage);

        for (int i = 0; i < _entries.Count; i++)
        {
            if (!_entries[i].Selectable) continue;

            Label label = _entries[i].Label;
            int index = i;
            label.RegisterCallback<MouseEnterEvent>(_ => _selection.Select(index, animated: true));
            label.RegisterCallback<ClickEvent>(_ => _selection.ActivateCurrent());
        }

        _root.focusable = true;
        _root.RegisterCallback<NavigationMoveEvent>(OnNavigationMove);
        _root.RegisterCallback<NavigationSubmitEvent>(OnNavigationSubmit);

        // La selezione parte su Play e non si azzera mai: il marker deve essere sempre da qualche
        // parte, anche a mouse fermo lontano dal menu.
        _selection.Select(0, animated: false);

        _stage.RegisterCallback<GeometryChangedEvent>(OnStageGeometryChanged);
    }

    private void OnDisable()
    {
        _menuEntry.Stop();
        _selection?.Stop();

        if (_stage != null) _stage.UnregisterCallback<GeometryChangedEvent>(OnStageGeometryChanged);

        if (_root != null)
        {
            _root.UnregisterCallback<NavigationMoveEvent>(OnNavigationMove);
            _root.UnregisterCallback<NavigationSubmitEvent>(OnNavigationSubmit);
        }
    }

    /// <summary>
    /// Primo layout: solo qui i rettangoli delle voci e la misura del testo esistono davvero. Prima
    /// di questo momento l'ordine di entrata dei pezzi e la larghezza della sottolineatura sarebbero
    /// calcolati su degli zeri.
    /// </summary>
    private void OnStageGeometryChanged(GeometryChangedEvent evt)
    {
        _selection.Refresh();

        if (_introStarted) return;

        _introStarted = true;
        _ = PlayOpeningAsync(destroyCancellationToken);
    }

    private async Awaitable PlayOpeningAsync(CancellationToken token)
    {
        try
        {
            if (_drakeAnimator != null)
                await _drakeAnimator.PlayIntroAsync(token);

            if (token.IsCancellationRequested || !isActiveAndEnabled) return;

            MainMenuMotionSO motion = _drakeAnimator != null ? _drakeAnimator.Motion : null;
            float delay = motion != null ? motion.MenuDelay : 0f;
            float duration = motion != null ? motion.MenuDuration : 0.4f;
            Ease ease = motion != null ? motion.MenuEase : Ease.OutQuad;

            _itemsRoot.style.visibility = Visibility.Visible;
            _marker.style.visibility = Visibility.Visible;

            _menuEntry.Stop();
            _menuEntry = Sequence.Create(useUnscaledTime: true);
            _menuEntry = _menuEntry.Insert(delay, Tween.Custom(_itemsRoot, 0f, 1f, duration,
                static (element, value) => element.style.opacity = value, ease, useUnscaledTime: true));
            _menuEntry = _menuEntry.Insert(delay, Tween.Custom(_marker, 0f, 1f, duration,
                static (element, value) => element.style.opacity = value, ease, useUnscaledTime: true));

            await _menuEntry;

            if (token.IsCancellationRequested || !isActiveAndEnabled) return;

            // Il focus arriva a intro finita: prenderlo prima significherebbe accettare una freccia
            // o un Invio mentre le voci non sono ancora a schermo.
            _root.Focus();
        }
        catch (OperationCanceledException)
        {
            // Scena scaricata a metà apertura: non c'è nulla da riportare.
        }
    }

    private void OnNavigationMove(NavigationMoveEvent evt)
    {
        switch (evt.direction)
        {
            case NavigationMoveEvent.Direction.Up:
                _selection.Move(-1);
                break;
            case NavigationMoveEvent.Direction.Down:
                _selection.Move(1);
                break;
            default:
                return;
        }

        evt.StopPropagation();
    }

    private void OnNavigationSubmit(NavigationSubmitEvent evt)
    {
        _selection.ActivateCurrent();
        evt.StopPropagation();
    }

    private void StartCombat()
    {
        // La scena resta viva ancora qualche frame mentre parte la transizione: senza questa guardia
        // un doppio click alzerebbe il canale due volte.
        if (_transitionRequested) return;
        _transitionRequested = true;

        _startCombatRequested?.RaiseEvent();
    }

    private void Quit()
    {
        if (_transitionRequested) return;
        _transitionRequested = true;

#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }
}
