# Antinote Phase 2 — Slash Commands Design

**Date:** 2026-04-20  
**Stack:** .NET 8, WPF, C#, AvalonEdit, NCalc

---

## Overview

Phase 2 replaces the `RichTextBox` editor with **AvalonEdit** and introduces a slash command system. Typing `/` triggers an autocomplete popup; selecting a command inserts a Markdown snippet. Math lines evaluate expressions inline via NCalc.

---

## Architecture

Four new components layered on top of the existing `NoteWindow`:

- **SlashCommandEngine** — watches keystrokes, detects `/` word starts, filters commands by typed characters, coordinates the popup
- **SlashCommandPopup** — WPF `Popup` near the cursor; keyboard (`↑↓ Enter Tab Escape`) and mouse navigable; filtered list updates live as user types
- **CommandHandlers** — one handler per command; replaces `/command` text with the appropriate Markdown snippet and positions cursor
- **MathEvaluator** — uses `NCalc` to evaluate expressions on `Enter`; appends `→ result` inline; silently ignores invalid expressions

---

## Slash Commands

| Command | Inserts | Cursor |
|---|---|---|
| `/math` | `= ` | After `= ` |
| `/list` | `- ` | After `- ` |
| `/checklist` | `- [ ] ` | After `- [ ] ` |
| `/todo` | `- [ ] ` | After `- [ ] ` |
| `/date` | `April 20, 2026` | After date |
| `/time` | `14:32` | After time |
| `/divider` | `---` | Next line |
| `/code` | ` ```\n\n``` ` | Inside block |
| `/heading` | `## ` | After `## ` |

---

## Popup Behavior

- Appears 100ms after `/` is typed
- Filters live as user continues typing (e.g. `/ma` → shows `math`)
- `↑` / `↓` to navigate, `Enter` or `Tab` to confirm, `Escape` to dismiss
- Disappears silently if no commands match
- Click to select

---

## Math Evaluation

- Lines starting with `= ` are math lines
- On `Enter`, expression after `= ` is evaluated via NCalc
- Result appended: `= 2 + 2 → 4`
- Invalid expressions: no action, no error shown
- `= ` prefix rendered in dimmer color via AvalonEdit `DocumentColorizingTransformer`

---

## AvalonEdit Integration

- Replaces `RichTextBox` in `NoteWindow.xaml`
- Config: `FontFamily="Chivo Mono"`, `FontSize="14"`, `Background="Transparent"`, `BorderThickness="0"`, `ShowLineNumbers="False"`, `WordWrap="True"`
- Load/save uses plain `TextEditor.Text` — simpler than `FlowDocument`
- Auto-save debounce hooks into `TextEditor.TextChanged`
- Placeholder overlay shown when `TextEditor.Text` is empty

---

## Font

**Chivo Mono** — used everywhere: editor, date header, placeholder text.

---

## Storage

No changes from Phase 1. All slash command output saves as standard Markdown.

---

## Out of Scope (Phase 2)

- Search across notes
- Cloud sync
- Custom themes
