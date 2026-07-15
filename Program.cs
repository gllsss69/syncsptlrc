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

        // Ініціалізуємо наші сервіси
        var player = new PlayerService();
        var ascii = new AsciiService();
        var lyrics = new LyricsService();

        string lastTrack = "";
        
        List<SyncedLine> syncedLyrics = new List<SyncedLine>();
        bool isSynced = false;
        string scrollModeInfo = "";



        string currentArtist = "";
        string currentTitle = "";

        // --- Smooth sync: local interpolation ---
        Stopwatch localTimer = new Stopwatch();
        double lastPolledPosition = 0;
        DateTime lastPollTime = DateTime.MinValue;
        bool isPlaying = false;
        string currentTrack = "";

        while (true)
        {
            int winWidth = 80;
            int winHeight = 24;

            // --- Poll playerctl once per second ---
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
                int searchBlockHeight = logoLines.Length + 12; // logo + search strings + padding
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


                // ============================================
                //  FALLBACK-ЛАНЦЮЖОК СИНХРОНІЗОВАНОГО ТЕКСТУ
                // ============================================

                string? syncedText = null;

                // Крок 1: Musixmatch через Spotify Track ID (найточніший матч)
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

                // Крок 2: Musixmatch через пошук artist+title
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

                // Крок 3: LRCLIB через пошук artist+title
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
                
                // Очищаємо екран після пошуку, щоб не було накладання тексту
                Console.Clear();
            }

            // --- МАЛЮЄМО ЕКРАН ---
            Console.SetCursorPosition(0, 0);

            try { winWidth = Console.WindowWidth; winHeight = Console.WindowHeight; } catch { }

            int footerHeight = 3;
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

                Console.ForegroundColor = ConsoleColor.Yellow;
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

            // --- ВИВЕДЕННЯ ФУТЕРА ---
            Action<string, ConsoleColor> PrintCenteredFooter = (text, color) => 
            {
                if (string.IsNullOrEmpty(text)) text = " ";
                int pad = Math.Max(0, (winWidth - text.Length) / 2);
                Console.ForegroundColor = color;
                Console.WriteLine((new string(' ', pad) + text).PadRight(winWidth));
                Console.ResetColor();
            };

            Console.WriteLine(new string(' ', winWidth)); // Пустий рядок перед футером
            PrintCenteredFooter($"{currentArtist} - {currentTitle}", ConsoleColor.Cyan);
            
            int fPad2 = Math.Max(0, (winWidth - scrollModeInfo.Length - 16) / 2);
            Console.ForegroundColor = ConsoleColor.DarkGray;
            Console.Write((new string(' ', fPad2) + $"Display mode: {scrollModeInfo}").PadRight(winWidth));
            Console.ResetColor();

            Thread.Sleep(50);
        }
    }
}