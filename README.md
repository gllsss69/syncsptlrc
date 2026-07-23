# syncsptlrc

> ⚠️ **Note:** This project is currently in active development (Work In Progress). It is not the final version, and features, UI, or stability may change as development continues.

**syncsptlrc** is a stylish, terminal-based synchronized lyrics viewer for Spotify on Linux. It tracks the currently playing song in Spotify using `playerctl`, fetches synchronized lyrics from popular APIs (Musixmatch, LRCLIB), and renders them in beautiful ASCII art using `Figgle`.

## ✨ Features

- **Synced Lyrics in ASCII**: Experience your music like never before with dynamic, terminal-based ASCII art lyrics.
- **Cyrillic Support**: Automatically transliterates Ukrainian, Russian, and other Cyrillic text into Latin characters on the fly, ensuring all lyrics render flawlessly in ASCII fonts.
- **Word-by-word Mode**: Watch the lyrics appear one word at a time based on time interpolation for maximum synchronization.
- **Multiple Lyric Sources**: Automatically searches Musixmatch and LRCLIB to find the most accurate synced lyrics.
- **Real-time Customization**: Change ASCII fonts, text colors, HUD visibility, and Word-by-word mode on the fly using keyboard hotkeys.
- **Smart Playback Tracking**: Seamlessly handles pausing, resuming, ad breaks, and track changes via `playerctl`.

## 📋 Prerequisites

To run this application, you must have the following installed on your system:
- **.NET SDK 10.0** (or later)
- **`playerctl`**: A command-line utility for controlling media players. Required to communicate with Spotify.
- **Spotify Desktop Client**: Needs to be running and playing music.

## 🚀 Getting Started

1. Clone or download the repository to your local machine.
2. Ensure you have Spotify open and playing music.
3. Open a terminal in the project directory and run:

```bash
dotnet run
```

## ⌨️ Hotkeys / Controls

While the application is running, you can use the following keys to customize your experience:

- `F` - **Change Font**: Cycle through various available ASCII fonts.
- `C` - **Change Color**: Cycle through the available terminal text colors.
- `H` - **Toggle HUD**: Change the visibility of the bottom HUD menu (Full, Minimal, or Hidden).
- `W` - **Toggle Word-by-word Mode**: Switches between displaying whole lines or interpolating words one at a time.

## 🛠️ Built With

- **C# / .NET 10.0**
- [**Figgle**](https://github.com/drewnoakes/figgle) - For generating ASCII art from text.
- **playerctl** - For retrieving metadata and playback status from Spotify.
- **Musixmatch & LRCLIB APIs** - For fetching synced `.lrc` lyrics.