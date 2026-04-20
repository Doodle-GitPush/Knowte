# Antinote — Design Document

**Date:** 2026-04-20  
**Stack:** .NET 8, WPF, C#  
**Platform:** Windows only

---

## Overview

Antinote is a lightweight Windows tray application for frictionless daily note-taking. A global hotkey summons a floating note panel; notes are saved automatically as Markdown files, one per day.

---

## Architecture

A single .NET 8 WPF application with four main components:

- **App Core** — registers the global hotkey, manages the system tray icon, owns the app lifecycle, and starts on Windows login
- **Note Window** — a borderless, always-on-top WPF window with rounded corners and a RichTextBox editor
- **Storage Layer** — reads and writes daily `.md` files; auto-saves with a 500ms debounce on every keystroke
- **Tray Menu** — right-click system tray icon with: Open Today's Note, Open Notes Folder, Exit

---

## Hotkey

`Ctrl+Alt+N` — global hotkey registered via Win32 `RegisterHotKey` API.  
Toggles the note window: shows it if hidden, hides it if visible.

---

## UI Design

- **Window**: Borderless, always-on-top, centered on screen, not shown in taskbar
- **Size**: 600×400px, resizable by dragging edges
- **Appearance**: Pure white background, 16px corner radius, subtle drop shadow
- **Header**: Thin top strip showing today's date (e.g. `Sunday, April 20`) in light gray
- **Editor**: `RichTextBox` with 24px padding, placeholder text `Start writing...`
- **Font**: JetBrains Mono, 14px
- **Dismiss**: `Escape` key or click outside hides the window (app keeps running)
- **Animation**: 150ms fade-in/fade-out on show/hide

---

## Storage

- **Location**: `%AppData%\Antinote\notes\`
- **Format**: One `.md` file per day, named `YYYY-MM-DD.md`
- **Auto-save**: Debounced 500ms after last keystroke — no manual save
- **On open**: Load today's file if it exists; create blank if not
- **No database, no sync, no encryption** — plain files on disk

---

## Out of Scope (Phase 1)

- Slash commands
- Math / list keyword shortcuts
- Search across notes
- Cloud sync
