# Antinote Phase 3 — Design Document

**Date:** 2026-04-20  
**Approach:** Option B — separate renderer per concern

---

## Overview

Phase 3 adds math mode with ghost text, real inline checkboxes, targeted animations, a resized window, a repositioned date label, and a redesigned slash command popup.

---

## Architecture

Six focused components, all fitting into the existing AvalonEdit renderer pipeline:

| Component | Responsibility |
|---|---|
| `MathModeDetector` | Returns `true` if line 1 of the document is `math` (case-insensitive) |
| `NaturalLanguageMath` | Normalizes natural-language phrases to NCalc expressions before evaluation |
| `GhostTextRenderer` | `IBackgroundRenderer` — draws inline grey `→ result` preview after cursor |
| `CheckboxElementGenerator` | `VisualLineElementGenerator` — replaces `- [ ]` / `- [x]` tokens with real WPF checkboxes |
| `SlashCommandPopup` (redesign) | Rounded, no-scrollbar, animated, cursor-following autocomplete popup |
| `NoteWindow` (updates) | 450×630, date at bottom-right, wires all new renderers and animations |

Storage stays plain text throughout. No changes to `NoteStorage`, `HotkeyManager`, or startup.

---

## Math Mode + Ghost Text

**Activation:** First non-empty line equals `math` (case-insensitive). Re-checked on every `TextChanged`.

**Natural language normalization** — regex keyword map applied before NCalc:
- `divided by` / `divided in` → `/`
- `times` / `multiplied by` → `*`
- `plus` / `added to` → `+`
- `minus` / `subtracted from` → `-`
- `percent of` → `/100 *`
- Written numbers one–ten → digits

Unrecognized text passes through unchanged; NCalc returns null and nothing shows.

**Ghost text:** `GhostTextRenderer` implements `IBackgroundRenderer`. On every keystroke it silently evaluates the current line. If a result exists and the line doesn't already contain `→`, it draws `→ {result}` immediately after the last character using AvalonEdit's `DrawingContext`, colour `#CCCCCC`, same font and size. Fades in over 150ms via opacity interpolation driven by `DispatcherTimer`. Tab accepts (inserts the text); any navigation key dismisses.

**Auto-evaluation on Enter:** When math mode is active, Enter still triggers the existing `→ result` append behaviour via `SlashCommandEngine.HandleEnterKey`.

---

## Checkboxes

**`CheckboxElementGenerator`** extends `VisualLineElementGenerator`. On each line render it scans for `- [ ] ` (unchecked) and `- [x] ` (checked) at line start and replaces that 6-character token with an `InlineObjectElement` wrapping a WPF `CheckBox`.

- Size: 14×14px, vertically centred with text baseline
- Unchecked: white fill, 1.5px `#CCCCCC` border
- Checked: `#1A1A1A` fill, white checkmark drawn as a `Path`
- Toggle: click calls `Document.Replace` to swap `[ ]` ↔ `[x]` in underlying text
- Check animation: checkmark `Path` strokes in over 120ms via `StrokeDashOffset` animation (`CubicEase Out`); uncheck resets instantly

**`/x` command:** New slash command `x` in `SlashCommandRegistry`. When confirmed on a checklist/todo line, `SlashCommandEngine` detects it and replaces `[ ]` with `[x]` on the current line.

**Storage:** `- [ ] text` / `- [x] text` — fully readable outside the app.

---

## Animations

| Trigger | Animation |
|---|---|
| Slash popup appears | `Opacity` 0→1 + `TranslateY` +8→0, 120ms `CubicEase Out` |
| Slash popup dismisses | Reverse, 80ms |
| Checkbox checked | Checkmark `StrokeDashOffset` full→0, 120ms `CubicEase Out` |
| Checkbox unchecked | Instant reset |
| Ghost text appears | `_ghostOpacity` 0→1 over 150ms via `DispatcherTimer` ticks |
| Ghost text dismisses | Instant reset to 0 |

All `DoubleAnimation` instances use `FillBehavior = Stop`.

---

## UI Layout

**Window:** `Width="450" Height="630"`, `MinWidth="300" MinHeight="400"`.

**Header removed:** The 40px date header row is deleted. The editor fills the full window height. Drag-to-move attaches to the editor background (unfocused clicks).

**Date label:** `TextBlock` overlaid at bottom-right of editor — `HorizontalAlignment="Right"`, `VerticalAlignment="Bottom"`, `Margin="0,0,20,16"`, `FontSize="10"`, `Foreground="#DDDDDD"`.

**Slash command popup redesign:**
- `Background="White"`, `CornerRadius="10"`, shadow `BlurRadius=12 Opacity=0.10`
- No `MaxHeight` cap — all matches shown (max 9, fits without scroll)
- No scrollbar — `ScrollViewer.VerticalScrollBarVisibility="Hidden"`
- Per-item: `CornerRadius="6"` hover background `#F5F5F5`, padding `10,6`
- Selected: `#F0F0F0` background
- `/name` in `#1A1A1A`, description in `#AAAAAA` at smaller size, single row
- Position: 6px below cursor baseline, updated on every `SuggestionsChanged`
- Entrance/exit animation from above
