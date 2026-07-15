using System;
using System.Diagnostics;
using System.IO;
using Figgle;
using Figgle.Fonts;

public class AsciiService
{
    public readonly string Logo = """
                                 _   _          
                                | | | |         
  ___ _   _ _ __   ___ ___ _ __ | |_| |_ __ ___ 
 / __| | | | '_ \ / __/ __| '_ \| __| | '__/ __|
 \__ \ |_| | | | | (__\__ \ |_) | |_| | | | (__ 
 |___/\__, |_| |_|\___|___/ .__/ \__|_|_|  \___|
       __/ |              | |                   
      |___/               |_|                   
""";

    public string ConvertToAsciiArt(string text)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(text)) return "";
            return FiggleFonts.Standard.Render(text);
        }
        catch
        {
            return text + "\n";
        }
    }

    public List<string> RenderAsciiWrapped(string text, int maxCharsPerLine = 20)
    {
        var lines = new List<string>();
        if (string.IsNullOrWhiteSpace(text)) return lines;

        string[] words = text.Split(' ');
        string currentLine = "";

        foreach (var word in words)
        {
            if ((currentLine + word).Length > maxCharsPerLine && !string.IsNullOrEmpty(currentLine))
            {
                lines.Add(ConvertToAsciiArt(currentLine.Trim()));
                currentLine = "";
            }
            currentLine += word + " ";
        }
        if (!string.IsNullOrEmpty(currentLine))
        {
            lines.Add(ConvertToAsciiArt(currentLine.Trim()));
        }

        return lines;
    }
}