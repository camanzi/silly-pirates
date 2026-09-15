using System;
using System.Collections.Generic;
using PrimeTween;
using UnityEngine;
using UnityEngine.UIElements;

/// <summary>
/// Tiene la voce di menu attualmente selezionata e ne aggiorna i due segnali visivi.
///
/// I due segnali si comportano in modo DIVERSO di proposito:
/// - la piuma d'oca scorre da una riga all'altra, quindi è un elemento solo che si sposta;
/// - le sottolineature non viaggiano: quella della voce abbandonata si cancella da destra verso
///   sinistra mentre quella della voce scelta si scrive da sinistra verso destra. Sono due
///   animazioni opposte nello stesso istante, quindi servono due elementi distinti — una linea
///   sola non potrebbe fare entrambe le cose.
///
/// La selezione è SEMPRE valorizzata, anche quando il mouse non è sopra nulla: i segnali non
/// compaiono e scompaiono a seconda del puntatore, seguono la voce attiva.
///
/// Non è un MonoBehaviour: non ha bisogno di un ciclo di vita Unity e così resta posseduta da
/// <see cref="MainMenuController"/>, che è l'unico a decidere quando fermarla.
/// </summary>
public sealed class MainMenuSelection
{
    /// <summary>Una voce di menu. Le voci non selezionabili restano a schermo ma vengono scavalcate.</summary>
    public readonly struct Entry
    {
        public readonly Label Label;
        public readonly VisualElement Underline;
        public readonly bool Selectable;
        public readonly Action Activate;

        public Entry(Label label, VisualElement underline, bool selectable, Action activate)
        {
            Label = label;
            Underline = underline;
            Selectable = selectable;
            Activate = activate;
        }
    }

    private readonly VisualElement _marker;
    private readonly IReadOnlyList<Entry> _entries;
    private readonly MainMenuMotionSO.SelectionMotion _motion;

    // Un tween per sottolineatura e non due condivisi ("quella che entra" / "quella che esce"):
    // cambiando voce in fretta la stessa linea può dover invertire il verso a metà corsa, e solo
    // un tween per elemento permette di fermare esattamente quello giusto.
    private readonly Tween[] _underlineTweens;
    private Tween _quillTween;
    private int _index = -1;

    public MainMenuSelection(VisualElement marker, IReadOnlyList<Entry> entries,
        MainMenuMotionSO.SelectionMotion motion)
    {
        _marker = marker;
        _entries = entries;
        _motion = motion;
        _underlineTweens = new Tween[entries.Count];
    }

    public int Index => _index;

    public void Select(int index, bool animated)
    {
        if (index < 0 || index >= _entries.Count) return;
        if (!_entries[index].Selectable) return;
        if (index == _index && animated) return;

        int previous = _index;
        _index = index;

        MoveQuillTo(_entries[index].Label, animated);

        if (previous >= 0 && previous != index) EraseUnderline(previous, animated);
        DrawUnderline(index, animated);
    }

    /// <summary>
    /// Sposta la selezione di <paramref name="delta"/> posizioni saltando le voci non selezionabili,
    /// con avvolgimento agli estremi. Il limite del ciclo è il numero di voci: senza, una lista di
    /// sole voci disabilitate girerebbe all'infinito.
    /// </summary>
    public void Move(int delta)
    {
        if (_entries.Count == 0 || delta == 0) return;

        int step = delta > 0 ? 1 : -1;
        int candidate = _index;

        for (int i = 0; i < _entries.Count; i++)
        {
            candidate = (candidate + step + _entries.Count) % _entries.Count;

            if (!_entries[candidate].Selectable) continue;

            Select(candidate, animated: true);
            return;
        }
    }

    public void ActivateCurrent()
    {
        if (_index < 0 || _index >= _entries.Count) return;

        _entries[_index].Activate?.Invoke();
    }

    /// <summary>
    /// Riallinea piuma e sottolineature senza animarle. Serve al primo layout (quando la larghezza
    /// del testo diventa misurabile) e a ogni cambio di risoluzione.
    /// </summary>
    public void Refresh()
    {
        if (_index < 0) return;

        MoveQuillTo(_entries[_index].Label, animated: false);

        for (int i = 0; i < _entries.Count; i++)
        {
            PlaceUnderline(i);

            _underlineTweens[i].Stop();
            _entries[i].Underline.style.width = i == _index ? MeasureTextWidth(_entries[i].Label) : 0f;
        }
    }

    public void Stop()
    {
        _quillTween.Stop();

        for (int i = 0; i < _underlineTweens.Length; i++)
            _underlineTweens[i].Stop();
    }

    private void MoveQuillTo(Label label, bool animated)
    {
        float target = label.layout.y;

        _quillTween.Stop();

        if (!animated || _motion.quillDuration <= 0f)
        {
            _marker.style.top = target;
            return;
        }

        _quillTween = Tween.Custom(_marker, _marker.resolvedStyle.top, target, _motion.quillDuration,
            static (element, value) => element.style.top = value, _motion.quillEase,
            useUnscaledTime: true);
    }

    /// <summary>La linea si ritira verso sinistra: il bordo sinistro resta fermo e la larghezza va a zero.</summary>
    private void EraseUnderline(int index, bool animated)
    {
        VisualElement underline = _entries[index].Underline;

        _underlineTweens[index].Stop();

        if (!animated || _motion.underlineOutDuration <= 0f)
        {
            underline.style.width = 0f;
            return;
        }

        _underlineTweens[index] = Tween.Custom(underline, underline.resolvedStyle.width, 0f,
            _motion.underlineOutDuration, static (element, value) => element.style.width = value,
            _motion.underlineOutEase, useUnscaledTime: true);
    }

    /// <summary>La linea si scrive verso destra a partire dal bordo sinistro del testo.</summary>
    private void DrawUnderline(int index, bool animated)
    {
        VisualElement underline = _entries[index].Underline;
        float target = MeasureTextWidth(_entries[index].Label);

        // Riposizionata prima di crescere: la voce potrebbe non essere mai stata disegnata, e in
        // ogni caso il suo rettangolo cambia a ogni riassetto del layout.
        PlaceUnderline(index);

        _underlineTweens[index].Stop();

        if (!animated || _motion.underlineInDuration <= 0f)
        {
            underline.style.width = target;
            return;
        }

        _underlineTweens[index] = Tween.Custom(underline, underline.resolvedStyle.width, target,
            _motion.underlineInDuration, static (element, value) => element.style.width = value,
            _motion.underlineInEase, startDelay: Mathf.Max(0f, _motion.underlineInDelay),
            useUnscaledTime: true);
    }

    /// <summary>
    /// Ancora la sottolineatura al bordo sinistro del testo e appena sotto la sua base. Il testo è
    /// centrato in una cassa da 453px molto più larga di lui, quindi partire dal bordo della cassa
    /// darebbe una linea lunghissima e disallineata.
    /// </summary>
    private void PlaceUnderline(int index)
    {
        Label label = _entries[index].Label;
        VisualElement underline = _entries[index].Underline;

        float textWidth = MeasureTextWidth(label);
        float thickness = underline.resolvedStyle.height > 0f ? underline.resolvedStyle.height : 3f;

        underline.style.left = label.layout.x + (label.layout.width - textWidth) * 0.5f;
        underline.style.top = label.layout.yMax - thickness;
    }

    /// <summary>
    /// La sottolineatura copre il testo, non la cassa della voce: "Play" e "Options" hanno larghezze
    /// molto diverse dentro lo stesso box da 453px.
    /// </summary>
    private static float MeasureTextWidth(Label label)
    {
        float measured = label.MeasureTextSize(label.text,
            0f, VisualElement.MeasureMode.Undefined,
            0f, VisualElement.MeasureMode.Undefined).x;

        // Prima che il font sia risolto la misura torna zero o NaN: in quel caso si ripiega sulla
        // cassa della voce, e il valore giusto arriva al Refresh del primo layout.
        return float.IsNaN(measured) || measured <= 0f ? label.contentRect.width : measured;
    }
}
