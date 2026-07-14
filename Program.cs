using System;
using System.Diagnostics;
using System.Text;
using System.Text.Json;
using System.Text.Unicode;
using System.Threading;
using HtmlAgilityPack;

using System.Text.RegularExpressions;

class Program
{

   static string syncsptlrcArt = """
                                 _   _          
                                | | | |         
  ___ _   _ _ __   ___ ___ _ __ | |_| |_ __ ___ 
 / __| | | | '_ \ / __/ __| '_ \| __| | '__/ __|
 \__ \ |_| | | | | (__\__ \ |_) | |_| | | | (__ 
 |___/\__, |_| |_|\___|___/ .__/ \__|_|_|  \___|
       __/ |              | |                   
      |___/               |_|                   
""";

    static void Main(string[] args)
    {

        Console.OutputEncoding = System.Text.Encoding.UTF8;
        Console.Clear();
        Console.WriteLine(syncsptlrcArt);
        Thread.Sleep(3000);

        string lastTrack = "";
        string currentLyrics = "";

        while (true)
        {
            string currentTrack = GetCurrentTrack();
            
            if (currentTrack != lastTrack && currentTrack != "The music isn't playing." && currentTrack != "Failed to retrieve the song")
            {
                Console.Clear();
                Console.ForegroundColor = ConsoleColor.Cyan;
                Console.WriteLine(syncsptlrcArt);
                Console.ResetColor();

                Console.WriteLine($"\n Looking lyrics for: {currentTrack}...");

                string[] trackParts = currentTrack.Split(new string[] { " - " }, StringSplitOptions.None);
                string artist = "";
                string title = "";

                if (trackParts.Length >= 2)
                {
                    artist = trackParts[0];
                    title = trackParts[1];
                }
                else
                {
                    title = currentTrack;
                }

                currentLyrics = GetLyricsFromGenius(artist, title);
                lastTrack = currentTrack;
            }

            Console.Clear();
            Console.WriteLine($"Play now: {currentTrack}\n");

            Console.WriteLine(currentLyrics);

            Thread.Sleep(3000);

        }
    }

    static string GetCurrentTrack()
    {
        try
        {
          ProcessStartInfo psi = new ProcessStartInfo
            {
                FileName = "playerctl",
                Arguments = "metadata --format \"{{ artist }} - {{ title }}\"",
                RedirectStandardOutput = true,
                UseShellExecute = false,
                CreateNoWindow = true
            };

            using (Process process = Process.Start(psi))
            {
                using (System.IO.StreamReader reader = process.StandardOutput)
                {
                    string result = reader.ReadToEnd().Trim();
                    return string.IsNullOrEmpty(result) ? "The music isn't playing." : result;
                }
            }
        }
        catch
        {
            return "Failed to retrieve the song";
        }
    }

    static string GetLyricsFromGenius(string artist, string title)
    {
        
        string accessToken = Environment.GetEnvironmentVariable("GENIUS_ACCESS_TOKEN") ?? "";

        try
        {
            using (HttpClient client = new HttpClient())
            {
                client.DefaultRequestHeaders.Add("Authorization", $"Bearer {accessToken}");
                client.DefaultRequestHeaders.Add("User-Agent", "syncsptlrc-app");

                string searchQuery = $"{artist} {title}";
                string searchUrl = $"https://api.genius.com/search?q={Uri.EscapeDataString(searchQuery)}";

                string searchResponse = client.GetStringAsync(searchUrl).GetAwaiter().GetResult();

                string songUrl = "";
                using (JsonDocument doc = JsonDocument.Parse(searchResponse))
                {
                    JsonElement hits = doc.RootElement.GetProperty("response").GetProperty("hits");
                    if (hits.GetArrayLength() > 0)
                    {
                        songUrl = hits[0].GetProperty("result").GetProperty("url").GetString();
                    }
                }

                if (string.IsNullOrEmpty(songUrl))
                    return "Song not found on Genius";

                var web = new HtmlWeb();
                web.UserAgent = "Mozilla/5.0 (X11; Linux x86_64) AppleWebKit/537.36";
                var htmlDoc = web.Load(songUrl);

                var lyricsNodes = htmlDoc.DocumentNode.SelectNodes("//div[contains(@class, 'Lyrics__Container')]");

                if (lyricsNodes != null)
                {
                    System.Text.StringBuilder sb = new System.Text.StringBuilder();
                    foreach  (var node in lyricsNodes)
                    {
                        
                        string text = node.InnerHtml.Replace("<br>", "\n");
 
                        var subDoc = new HtmlDocument();
                        subDoc.LoadHtml(text);
                    
                        sb.AppendLine(subDoc.DocumentNode.InnerText.Trim());
                }
                
                string rawLyrics = System.Web.HttpUtility.HtmlDecode(sb.ToString().Trim());

                string cleanLyrics = Regex.Replace(rawLyrics, @"\[.*?\]", "");

                cleanLyrics = Regex.Replace(cleanLyrics, @"^\d+\s+Contributors?.*?\s+Lyrics", "", RegexOptions.IgnoreCase);

                cleanLyrics = Regex.Replace(cleanLyrics, @"^.*?Contributors?.*?Lyrics", "", RegexOptions.IgnoreCase);

                cleanLyrics = Regex.Replace(cleanLyrics, @"\n{3,}", "\n\n");

                return cleanLyrics.Trim();
            }
        }
    }
    catch (Exception ex)
    {
        return $"Error loading lyrics: {ex.Message}";
    }

    return "Failed to parse the text from the Genius page.";
  }
}
