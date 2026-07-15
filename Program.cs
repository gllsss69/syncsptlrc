using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading;

class Program
{
    static void Main(string[] args)
    {
        Console.OutputEncoding = System.Text.Encoding.UTF8;
        Console.CursorVisible = false;


        var player = new PlayerService();
        var ascii = new AsciiService();
        var lyrics = new LyricsService();

        string lastTrack = "";
        
        List<SyncedLine> syncedLyrics = new List<SyncedLine>();
        bool isSynced = false;
        string scrollModeInfo = "";


        string currentArtist = "";
        string currentTitle = "";


        // --- Кольори тексту ---
        ConsoleColor[] _colors = { ConsoleColor.Yellow, ConsoleColor.Cyan, ConsoleColor.Green, ConsoleColor.Magenta, ConsoleColor.White, ConsoleColor.Red };
        int _colorIndex = 0;

        // --- HUD: 0 = все видно, 1 = меню сховано, 2 = тільки текст ---
        int hudMode = 0;

        Stopwatch localTimer = new Stopwatch();
        double lastPolledPosition = 0;
        DateTime lastPollTime = DateTime.MinValue;
        bool isPlaying = false;
        string currentTrack = "";

        while (true)
        {
            int winWidth = 80;
            int winHeight = 24;

            // --- Обробка клавіш ---
            while (Console.KeyAvailable)
            {
                var key = Console.ReadKey(true);
                switch (key.Key)
                {
                    case ConsoleKey.F:
                        ascii.NextFont();
                        Console.Clear();
                        break;
                    case ConsoleKey.C:
                        _colorIndex = (_colorIndex + 1) % _colors.Length;
                        break;
                    case ConsoleKey.H:
                        hudMode = (hudMode + 1) % 3;
                        Console.Clear();
                        break;
                }
            }

            bool shouldPoll = (DateTime.UtcNow - lastPollTime).TotalMilliseconds >= 1000;
            if (shouldPoll)
            {
                lastPollTime = DateTime.UtcNow;
                currentTrack = player.GetCurrentTrack();
                string status = player.GetStatus();
                isPlaying = status.Trim().Equals("Playing", StringComparison.OrdinalIgnoreCase);

                if (isPlaying)
                {
                    lastPolledPosition = player.GetCurrentPosition();
                    localTimer.Restart();
                }
                else
                {
                    localTimer.Stop();
                }
            }

            if (string.IsNullOrEmpty(currentTrack)) { Thread.Sleep(50); continue; }

            if (currentTrack != lastTrack && currentTrack != "The music isn't playing." && currentTrack != "Failed to retrieve the song")
            {
                Console.Clear();
                try { winWidth = Console.WindowWidth; winHeight = Console.WindowHeight; } catch { }

                string[] logoLines = ascii.Logo.Split(new[] { "\r\n", "\r", "\n" }, StringSplitOptions.None);
                int searchBlockHeight = logoLines.Length + 12;
                int topPadding = Math.Max(0, (winHeight - searchBlockHeight) / 2);

                for (int i = 0; i < topPadding; i++) Console.WriteLine();

                Console.ForegroundColor = ConsoleColor.Cyan;
                foreach (string line in logoLines)
                {
                    int leftPad = Math.Max(0, (winWidth - line.Length) / 2);
                    Console.WriteLine(new string(' ', leftPad) + line);
                }
                Console.ResetColor();

                Action<string, ConsoleColor> PrintSearchStep = (text, color) =>
                {
                    int pad = Math.Max(0, (winWidth - text.Length) / 2);
                    Console.ForegroundColor = color;
                    Console.WriteLine(new string(' ', pad) + text);
                    Console.ResetColor();
                };

                string searchHeader = $"Searching lyrics for: {currentTrack}...";
                Console.WriteLine();
                PrintSearchStep(searchHeader, ConsoleColor.White);
                Console.WriteLine();

                string[] trackParts = currentTrack.Split(new string[] { " - " }, StringSplitOptions.None);
                currentArtist = trackParts.Length >= 2 ? trackParts[0] : "";
                currentTitle = trackParts.Length >= 2 ? trackParts[1] : currentTrack;
                
                string artist = currentArtist;
                string title = currentTitle;

                isSynced = false;
                syncedLyrics.Clear();


                string? syncedText = null;

                string? spotifyId = player.GetSpotifyTrackId();
                if (!string.IsNullOrEmpty(spotifyId))
                {
                    PrintSearchStep($"Spotify ID: {spotifyId}", ConsoleColor.DarkGray);
                    PrintSearchStep("Musixmatch (Spotify ID)...", ConsoleColor.DarkGray);
                    syncedText = lyrics.GetLyricsFromMusixmatchBySpotifyId(spotifyId);

                    if (!string.IsNullOrEmpty(syncedText) && syncedText.Contains("["))
                    {
                        syncedLyrics = lyrics.ParseLrc(syncedText);
                        if (syncedLyrics.Count > 0)
                        {
                            isSynced = true;
                            scrollModeInfo = "SYNCED (Musixmatch/Spotify ID)";
                        }
                    }
                }

                if (!isSynced)
                {
                    PrintSearchStep("Musixmatch (search)...", ConsoleColor.DarkGray);
                    syncedText = lyrics.GetLyricsFromMusixmatch(artist, title);

                    if (!string.IsNullOrEmpty(syncedText) && syncedText.Contains("["))
                    {
                        syncedLyrics = lyrics.ParseLrc(syncedText);
                        if (syncedLyrics.Count > 0)
                        {
                            isSynced = true;
                            scrollModeInfo = "SYNCED (Musixmatch)";
                        }
                    }
                }

                if (!isSynced)
                {
                    PrintSearchStep("LRCLIB (search)...", ConsoleColor.DarkGray);
                    syncedText = lyrics.GetLyricsFromLrcLib(artist, title);

                    if (!string.IsNullOrEmpty(syncedText) && syncedText.Contains("["))
                    {
                        syncedLyrics = lyrics.ParseLrc(syncedText);
                        if (syncedLyrics.Count > 0)
                        {
                            isSynced = true;
                            scrollModeInfo = "SYNCED (LRCLIB)";
                        }
                    }
                }

                lastTrack = currentTrack;
                Console.Clear();
            }

            Console.SetCursorPosition(0, 0);

            try { winWidth = Console.WindowWidth; winHeight = Console.WindowHeight; } catch { }

            // hudMode 0,1 — показуємо всі рядки; hudMode 2 — тільки текст
            int footerHeight = hudMode == 2 ? 0 : 3;
            int availableHeight = Math.Max(1, winHeight - footerHeight);
            int printedLines = 0;

            if (isSynced && syncedLyrics.Count > 0)
            {
                double currentPosition = lastPolledPosition + localTimer.Elapsed.TotalSeconds;
                int activeIndex = 0;

                for (int i = 0; i < syncedLyrics.Count; i++)
                {
                    if (syncedLyrics[i].TimeInSeconds <= currentPosition)
                    {
                        activeIndex = i;
                    }
                    else
                    {
                        break;
                    }
                }

                string activeText = syncedLyrics[activeIndex].Text;
                if (string.IsNullOrWhiteSpace(activeText) || activeText == "♪" || activeText == "♫")
                {
                    activeText = "~ ~ ~";
                }

                int maxChars = Math.Max(10, winWidth / 7);
                List<string> asciiLines = ascii.RenderAsciiWrapped(activeText, maxChars);

                int totalAsciiHeight = 0;
                foreach(var artPart in asciiLines)
                {
                    totalAsciiHeight += artPart.Split(new[] { "\n" }, StringSplitOptions.None).Length;
                }

                int topPadding = Math.Max(0, (availableHeight - totalAsciiHeight) / 2);

                for (int i = 0; i < topPadding; i++)
                {
                    Console.WriteLine(new string(' ', winWidth));
                }
                printedLines += topPadding;

                Console.ForegroundColor = _colors[_colorIndex];
                foreach(var artPart in asciiLines)
                {
                    string[] splitArt = artPart.Split(new[] { "\r\n", "\r", "\n" }, StringSplitOptions.None);
                    foreach(var line in splitArt)
                    {
                        if (string.IsNullOrWhiteSpace(line)) continue;
                        int leftPad = Math.Max(0, (winWidth - line.Length) / 2);
                        string paddedLine = new string(' ', leftPad) + line;
                        Console.WriteLine(paddedLine.PadRight(winWidth));
                        printedLines++;
                    }
                }
                Console.ResetColor();
            }
            else
            {
                string msg = "[Failed to find lyrics for this song]";
                int topPad = Math.Max(0, availableHeight / 2);
                for (int i = 0; i < topPad; i++) 
                {
                    Console.WriteLine(new string(' ', winWidth));
                }
                printedLines += topPad;
                
                int leftPad = Math.Max(0, (winWidth - msg.Length) / 2);
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine((new string(' ', leftPad) + msg).PadRight(winWidth));
                Console.ResetColor();
                printedLines++;
            }

            for (int i = printedLines; i < availableHeight; i++)
            {
                Console.WriteLine(new string(' ', winWidth));
            }

            Action<string, ConsoleColor> PrintCenteredFooter = (text, color) => 
            {
                if (string.IsNullOrEmpty(text)) text = " ";
                int pad = Math.Max(0, (winWidth - text.Length) / 2);
                Console.ForegroundColor = color;
                Console.WriteLine((new string(' ', pad) + text).PadRight(winWidth));
                Console.ResetColor();
            };

            // --- ФУТЕР ---
            if (hudMode == 0 || hudMode == 1)
            {
                Console.WriteLine(new string(' ', winWidth));
                PrintCenteredFooter($"{currentArtist} - {currentTitle}", ConsoleColor.Cyan);

                if (hudMode == 0)
                {
                    string hudText = $"Display mode: {scrollModeInfo}  |  [F]Font: {ascii.CurrentFontName}  [C]Color  [H]HUD";
                    int fPad2 = Math.Max(0, (winWidth - hudText.Length) / 2);
                    Console.ForegroundColor = ConsoleColor.DarkGray;
                    Console.Write((new string(' ', fPad2) + hudText).PadRight(winWidth));
                    Console.ResetColor();
                }
                else
                {
                    Console.Write(new string(' ', winWidth));
                }
            }

            Thread.Sleep(50);
        }
    }
}