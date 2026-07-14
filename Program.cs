using System;
using System.Diagnostics;
using System.Text.Unicode;
using System.Threading;

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
        while (true)
        {
            string currentTrack = GetCurrentTrack();
            
            Console.Clear();
            Console.WriteLine($"Play now: {currentTrack}");

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
}