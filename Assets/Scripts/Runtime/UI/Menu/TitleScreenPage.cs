using System;
using System.Text;
using System.Threading;
using PrimeTween;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.Utilities;
using UnityEngine.UIElements;

/// <summary>
/// La title screen: la pagina-proxy che sta davanti al main menu sia all'avvio sia al ritorno da una
/// partita. NON e' una scena a se': e' un secondo stage dentro il documento del menu, che si congeda
/// con un fade scoprendo il menu gia' montato sotto. Cosi' il passaggio title -> menu non passa da
/// SceneFlowDirector, che mostrerebbe l'overlay di caricamento per una transizione da nulla.
///
/// Non e' un MonoBehaviour, per lo stesso motivo di <see cref="MainMenuSelection"/>: non ha bisogno
/// di un ciclo di vita Unity, resta posseduta da <see cref="MainMenuController"/> che e' l'unico a
/// sapere quando fermarla, e soprattutto non c'e' nulla da agganciare a mano in Editor.
///
/// Il prompt "Press any button" riusa <see cref="MainMenuSelection"/> con una voce sola: e' la stessa
/// coppia piuma + sottolineatura del menu, quindi si comporta come una voce selezionata per
/// costruzione invece che per somiglianza.
/// </summary>
public sealed class TitleScreenPage
{
    private const string HexDigits = "0123456789ABCDEF";

    private readonly VisualElement _stage;
    private readonly Label _title;
    private readonly VisualElement _promptGroup;
    private readonly Label _promptLabel;

    private readonly MainMenuMotionSO.TitleMotion _motion;
    private readonly MainMenuSelection _selection;

    // Il testo del titolo viene letto UNA volta qui e mai piu' dalla Label: da questo momento
    // 'Label.text' contiene i tag alpha e non e' piu' una fonte di verita'.
    private readonly string _text;
    private readonly StringBuilder _builder = new();

    // Centro orizzontale di ogni glifo, in coordinate del testo. E' su questi che si calcola l'alpha:
    // la maschera e' una funzione della posizione, non dell'indice del carattere.
    private float[] _glyphCentres;
    private float _textWidth;

    // Colore del titolo in RRGGBB, letto dallo stile risolto invece che scritto qui: l'alpha per-glifo
    // viaggia dentro un tag <color=#RRGGBBAA>, che ridichiara anche il colore, e inchiodarlo al nero
    // significherebbe che restilare la Label smetterebbe silenziosamente di avere effetto.
    private string _colour = "000000";

    private Tween _sweep;
    private Tween _promptFade;
    private Tween _pulse;
    private Tween _dismiss;
    private IDisposable _anyButton;
    private bool _dismissRequested;

    public TitleScreenPage(VisualElement stage, Label title, VisualElement promptGroup,
        Label promptLabel, VisualElement promptMarker, VisualElement promptUnderline,
        MainMenuMotionSO motion)
    {
        _stage = stage;
        _title = title;
        _promptGroup = promptGroup;
        _promptLabel = promptLabel;

        _motion = motion != null ? motion.Title : MainMenuMotionSO.TitleMotion.Default;
        _text = title.text ?? string.Empty;

        MainMenuSelection.Entry entry = new(promptLabel, promptUnderline, selectable: true, Dismiss);
        _selection = new MainMenuSelection(promptMarker, new[] { entry },
            motion != null ? motion.Selection : MainMenuMotionSO.SelectionMotion.Default);

        // Stato di partenza scritto subito, non al primo layout: aspettare significherebbe mostrare
        // per un frame il titolo gia' opaco e il prompt gia' a posto, cioe' la fine dell'animazione
        // prima del suo inizio.
        _stage.style.display = DisplayStyle.Flex;
        _stage.style.opacity = 1f;
        // Con alpha 00 le componenti RGB sono irrilevanti, quindi qui il colore vero non serve
        // ancora: lo stile e' risolto solo dal primo layout in poi.
        _title.text = "<color=#00000000>" + _text;

        // 'visibility' e non 'display': il layout deve restare misurabile, perche' e' da li' che
        // MainMenuSelection ricava dove mettere la sottolineatura e quanto e' larga. Hidden blocca
        // anche il picking, quindi il prompt non e' cliccabile prima di esistere davvero.
        _promptGroup.style.visibility = Visibility.Hidden;
        _promptGroup.style.opacity = 0f;
    }

    /// <summary>
    /// Va chiamata a layout risolto: prima, la misura del testo torna zero e i glifi finirebbero
    /// tutti nello stesso punto. Ritorna quando la title e' stata congedata.
    /// </summary>
    public async Awaitable PlayAsync(CancellationToken token)
    {
        MeasureGlyphs();
        ResolveColour();

        await RevealTitleAsync(token);
        if (token.IsCancellationRequested) return;

        await RevealPromptAsync(token);
        if (token.IsCancellationRequested) return;

        StartPulse();
        ArmInput();

        while (!_dismissRequested)
            await Awaitable.NextFrameAsync(token);

        await DismissAsync(token);
    }

    public void Stop()
    {
        _sweep.Stop();
        _promptFade.Stop();
        _pulse.Stop();
        _dismiss.Stop();
        _selection.Stop();

        DisarmInput();
    }

    // --- Comparsa del titolo -------------------------------------------------------------------

    /// <summary>
    /// Larghezze cumulative dei prefissi del testo: la posizione di ogni glifo e' quella che avrebbe
    /// nella stringa intera, kerning compreso. Spezzare il titolo in una Label per carattere darebbe
    /// invece una spaziatura sbagliata, che con un font calligrafico come Italianno si vede.
    /// </summary>
    private void MeasureGlyphs()
    {
        _glyphCentres = new float[_text.Length];

        float previous = 0f;
        bool measurable = _text.Length > 0;

        for (int i = 0; i < _text.Length; i++)
        {
            float upTo = Measure(_text.Substring(0, i + 1));

            if (upTo <= 0f)
            {
                measurable = false;
                break;
            }

            _glyphCentres[i] = (previous + upTo) * 0.5f;
            previous = upTo;
        }

        if (measurable && previous > 0f)
        {
            _textWidth = previous;
            return;
        }

        // Font non ancora risolto: si ripiega su una spaziatura uniforme sulla cassa della Label.
        // L'effetto resta una maschera che scorre, solo con i glifi distribuiti a occhio.
        _textWidth = _title.contentRect.width;
        float step = _text.Length > 0 ? _textWidth / _text.Length : 0f;

        for (int i = 0; i < _text.Length; i++)
            _glyphCentres[i] = (i + 0.5f) * step;
    }

    private float Measure(string text)
    {
        float measured = _title.MeasureTextSize(text,
            0f, VisualElement.MeasureMode.Undefined,
            0f, VisualElement.MeasureMode.Undefined).x;

        return float.IsNaN(measured) ? 0f : measured;
    }

    /// <summary>
    /// Una maschera con il bordo sfumato che passa da sinistra a destra. Non e' una macchina da
    /// scrivere: l'alpha di ogni glifo dipende da DOVE si trova rispetto al bordo, non da quando
    /// tocca al suo indice, quindi con un falloff ampio si hanno sempre piu' lettere in dissolvenza
    /// insieme e la transizione e' continua.
    ///
    /// Una maschera vera sovrapposta non era praticabile: per nascondere il testo dovrebbe essere
    /// color pergamena, ma la pergamena e' una texture e una toppa di tinta piatta si vedrebbe.
    /// </summary>
    /// <remarks>
    /// Il tag e' <c>&lt;color=#RRGGBBAA&gt;</c> e NON <c>&lt;alpha=#AA&gt;</c>: in UI Toolkit
    /// &lt;alpha&gt; viene riconosciuto come tag (sparisce dal testo) ma non ha alcun effetto, quindi
    /// usarlo darebbe un titolo perfettamente opaco senza un solo errore in console. Verificato a
    /// schermo, non dedotto.
    /// </remarks>
    private async Awaitable RevealTitleAsync(CancellationToken token)
    {
        float falloff = Mathf.Max(1f, _motion.sweepFalloff);

        _sweep.Stop();
        _sweep = Tween.Custom(this, -falloff, _textWidth + falloff,
            Mathf.Max(0.01f, _motion.sweepDuration),
            static (page, edge) => page.ApplySweep(edge), _motion.sweepEase, useUnscaledTime: true);

        await _sweep;

        if (token.IsCancellationRequested) return;

        // A sweep finito il testo torna nudo: lasciare un tag alpha davanti funzionerebbe, ma
        // significherebbe tenere per sempre del markup in una Label che non ne ha piu' bisogno.
        _title.text = _text;
    }

    /// <summary>
    /// Ricostruisce la stringa con un tag alpha per glifo. Gira a ogni frame per la durata del sweep:
    /// lo StringBuilder e' riusato e il tag si emette solo quando il valore CAMBIA, quindi nella zona
    /// gia' opaca e in quella ancora invisibile non se ne scrive nessuno.
    /// </summary>
    private void ApplySweep(float edgeX)
    {
        float falloff = Mathf.Max(1f, _motion.sweepFalloff);

        _builder.Clear();
        int lastAlpha = -1;

        for (int i = 0; i < _text.Length; i++)
        {
            float alpha = Mathf.Clamp01((edgeX - _glyphCentres[i]) / falloff);
            int quantised = Mathf.RoundToInt(alpha * 255f);

            if (quantised != lastAlpha)
            {
                _builder.Append("<color=#");
                _builder.Append(_colour);
                AppendHex(_builder, quantised);
                _builder.Append('>');
                lastAlpha = quantised;
            }

            _builder.Append(_text[i]);
        }

        _title.text = _builder.ToString();
    }

    /// <summary>
    /// Va chiamata a stile risolto: prima del primo layout resolvedStyle.color non e' quello del
    /// foglio e il titolo comparirebbe del colore sbagliato.
    /// </summary>
    private void ResolveColour()
    {
        Color32 colour = _title.resolvedStyle.color;

        _builder.Clear();
        AppendHex(_builder, colour.r);
        AppendHex(_builder, colour.g);
        AppendHex(_builder, colour.b);
        _colour = _builder.ToString();
    }

    private static void AppendHex(StringBuilder builder, int value)
    {
        builder.Append(HexDigits[(value >> 4) & 0xF]);
        builder.Append(HexDigits[value & 0xF]);
    }

    // --- Prompt --------------------------------------------------------------------------------

    private async Awaitable RevealPromptAsync(CancellationToken token)
    {
        _promptGroup.style.visibility = Visibility.Visible;

        // La sottolineatura si scrive da sinistra a destra e la piuma si allinea: e' lo stesso
        // codice che usa il menu, quindi il prompt non 'somiglia' a una voce selezionata, lo e'.
        _selection.Select(0, animated: true);

        _promptFade.Stop();
        _promptFade = Tween.Custom(_promptGroup, 0f, 1f, Mathf.Max(0.01f, _motion.promptFadeDuration),
            static (element, value) => element.style.opacity = value, Ease.OutQuad,
            startDelay: Mathf.Max(0f, _motion.promptDelay), useUnscaledTime: true);

        await _promptFade;
    }

    /// <summary>
    /// Pulsazione infinita del GRUPPO, non della sola Label: piuma, testo e sottolineatura devono
    /// respirare insieme. Il valore configurato e' il tempo di una sola direzione, quindi un ciclo
    /// Rewind completo (andata e ritorno) dura il doppio.
    /// </summary>
    private void StartPulse()
    {
        _pulse.Stop();
        _pulse = Tween.Custom(_promptGroup, 1f, Mathf.Clamp01(_motion.promptPulseMinOpacity),
            Mathf.Max(0.01f, _motion.promptPulseDuration),
            static (element, value) => element.style.opacity = value, Ease.InOutSine,
            cycles: -1, cycleMode: CycleMode.Rewind, useUnscaledTime: true);
    }

    // --- Input ---------------------------------------------------------------------------------

    /// <summary>
    /// 'Un tasto qualsiasi' non e' esprimibile come action nell'actions asset, e in questo progetto
    /// non esiste nessun EventSystem da cui passare: onAnyButtonPress e' l'unico appiglio che copre
    /// tastiera, mouse e gamepad senza toccare GameInput.inputactions.
    ///
    /// L'iscrizione avviene solo ORA e non all'inizio: un tasto premuto mentre il titolo compare
    /// verrebbe altrimenti mangiato in silenzio, saltando l'animazione senza che nulla lo segnali.
    /// </summary>
    private void ArmInput()
    {
        DisarmInput();

        _anyButton = InputSystem.onAnyButtonPress.CallOnce(_ => Dismiss());

        // Il click vale come pressione: il prompt e' pickabile solo da quando il gruppo e' Visible,
        // quindi non puo' scattare prima.
        _promptLabel.RegisterCallback<ClickEvent>(OnPromptClicked);
    }

    private void DisarmInput()
    {
        // CallOnce si disiscrive da solo alla prima pressione, ma se la scena viene scaricata prima
        // che qualcuno prema qualcosa l'iscrizione resterebbe appesa a una pagina morta.
        _anyButton?.Dispose();
        _anyButton = null;

        _promptLabel.UnregisterCallback<ClickEvent>(OnPromptClicked);
    }

    private void OnPromptClicked(ClickEvent evt)
    {
        _selection.ActivateCurrent();
        evt.StopPropagation();
    }

    private void Dismiss() => _dismissRequested = true;

    // --- Congedo -------------------------------------------------------------------------------

    private async Awaitable DismissAsync(CancellationToken token)
    {
        DisarmInput();

        // La pulsazione va fermata PRIMA del fade: e' un tween infinito sullo stesso gruppo, e
        // continuerebbe a riscrivere l'opacita' mentre lo stage sotto svanisce.
        _pulse.Stop();
        _selection.Stop();

        _dismiss.Stop();
        _dismiss = Tween.Custom(_stage, _stage.resolvedStyle.opacity, 0f,
            Mathf.Max(0.01f, _motion.dismissDuration),
            static (element, value) => element.style.opacity = value, Ease.InQuad,
            useUnscaledTime: true);

        await _dismiss;

        if (token.IsCancellationRequested) return;

        // Spento davvero e non solo trasparente: a tutto schermo e pickabile, continuerebbe a
        // rubare i click alle voci di menu appena scoperte.
        _stage.style.display = DisplayStyle.None;
    }
}
