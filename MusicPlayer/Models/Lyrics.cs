using System;
using System.Collections.Generic;

namespace MusicPlayer.Models;

public class LyricLine
{
    public TimeSpan Time { get; set; }
    public string Text { get; set; } = string.Empty;
    
    public LyricLine() { }
    
    public LyricLine(TimeSpan time, string text)
    {
        Time = time;
        Text = text;
    }
}

public class Lyrics
{
    public List<LyricLine> Lines { get; set; } = new();
    
    public Lyrics() { }
    
    public LyricLine? GetCurrentLine(TimeSpan position)
    {
        for (int i = Lines.Count - 1; i >= 0; i--)
        {
            if (Lines[i].Time <= position)
                return Lines[i];
        }
        
        return Lines.Count > 0 ? Lines[0] : null;
    }
    
    public int GetCurrentLineIndex(TimeSpan position)
    {
        for (int i = Lines.Count - 1; i >= 0; i--)
        {
            if (Lines[i].Time <= position)
                return i;
        }
        
        return 0;
    }
}
