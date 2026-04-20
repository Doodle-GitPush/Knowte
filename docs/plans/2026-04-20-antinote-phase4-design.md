# Antinote Phase 4 — Design

## Goal

Generalise the "first-line mode" pattern introduced in Phase 3, add list/checklist continuation on Enter, fix popup positioning, tighten UI sizing, and improve the math evaluator.

---

## 1. Architecture — `DocumentModeDetector`

Replace `MathModeDetector` with a general `DocumentModeDetector`:

```csharp
public enum DocumentMode { None, Math, List, Checklist }

public static class DocumentModeDetector
{
    public static DocumentMode Detect(string documentText);
}
```

First-line rules (case-insensitive, trimmed):
- `"math"` → `Math`
- `"list"` → `List`
- `"checklist"` → `Checklist`
- anything else → `None`

All callers of `MathModeDetector.IsActive()` switch to `DocumentModeDetector.Detect() == DocumentMode.Math`. `MathModeDetector` is deleted.

---

## 2. Slash Command Registry — Remove Mode Commands

Remove from `SlashCommandRegistry`: `math`, `list`, `checklist`, `todo` — these are now document modes, not insertable snippets.

Remaining commands: `x`, `date`, `time`, `divider`, `code`, `heading`.

Update registry count tests accordingly (6 commands).

---

## 3. List / Checklist Mode — Enter Key Behaviour

`HandleEnterKey(DocumentMode mode)` signature updated. Logic:

| Mode | Current line prefix | Enter inserts |
|------|---------------------|---------------|
| `List` | starts with `- ` | `\n- ` |
| `List` | empty (`- ` exactly) | removes prefix, plain `\n` |
| `List` | free text | plain `\n` |
| `Checklist` | starts with `- [ ] ` or `- [x] ` | `\n- [ ] ` |
| `Checklist` | empty (`- [ ] ` exactly) | removes prefix, plain `\n` |
| `Checklist` | free text | plain `\n` |
| `Math` | existing behaviour | format line → `\n` |
| `None` | any | plain `\n` |

**Shift+Enter** — always plain `\n`, regardless of mode. Detected in `Editor_PreviewKeyDown` before delegating to `HandleEnterKey`.

---

## 4. Popup Positioning Fix

**Root cause:** `PointToScreen` returns physical pixels; `Popup.HorizontalOffset`/`VerticalOffset` with `Placement="Absolute"` expects device-independent units (DIPs).

**Fix:** After calling `PointToScreen`, divide by the DPI scale factor:

```csharp
var source = PresentationSource.FromVisual(_editor);
var dpiX = source?.CompositionTarget?.TransformToDevice.M11 ?? 1.0;
var dpiY = source?.CompositionTarget?.TransformToDevice.M22 ?? 1.0;
return (screenPos.X / dpiX, screenPos.Y / dpiY);
```

---

## 5. UI Tweaks

| Property | Old | New |
|----------|-----|-----|
| Window width | 450 | 300 |
| Window height | 630 | 420 |
| Font size | 14px | 10px |
| Editor padding left | 24px | 20px |
| Editor padding top | 20px | 28px |
| Placeholder margin left | 28px | 24px |
| Placeholder margin top | 20px | 28px |
| `#000000` foreground | everywhere | `#333333` |

---

## 6. Math Evaluator Improvements

Expand `NaturalLanguageMath.Normalize` to pre-process before NCalc:

- `a ^ b` → `Pow(a, b)` — NCalc uses `Pow`, not `^`
- `sqrt(...)` → `Sqrt(...)` — NCalc requires capital S
- `x% of y` — already handled; also handle bare `x%` → `x/100`
- Broaden NaturalSentence regex to tolerate multiple filler words between operands

`MathEvaluator.Evaluate` already rounds to 2dp — no change needed there.

---

## Files Changed

| File | Change |
|------|--------|
| `src/Antinote/Commands/DocumentModeDetector.cs` | **new** — replaces MathModeDetector |
| `src/Antinote/Commands/MathModeDetector.cs` | **delete** |
| `src/Antinote/Commands/NaturalLanguageMath.cs` | extend Normalize |
| `src/Antinote/Commands/SlashCommandEngine.cs` | HandleEnterKey takes DocumentMode |
| `src/Antinote/Commands/SlashCommandRegistry.cs` | remove 4 mode commands |
| `src/Antinote/Views/NoteWindow.xaml` | window size, font, padding, colour |
| `src/Antinote/Views/NoteWindow.xaml.cs` | use DocumentModeDetector, Shift+Enter |
| `tests/…/DocumentModeDetectorTests.cs` | **new** |
| `tests/…/NaturalLanguageMathTests.cs` | extend |
| `tests/…/SlashCommandRegistryTests.cs` | update count |
| `tests/…/SlashCommandEngineTests.cs` | update HandleEnterKey tests |
