using System;

namespace MusicPlayer.Models;

public class Song
{
    public string FilePath { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public string Artist { get; set; } = string.Empty;
    public string Album { get; set; } = string.Empty;
    public TimeSpan Duration { get; set; }
    public string? CoverPath { get; set; }
    
    public Song() { }
    
    public Song(string filePath)
    {
        FilePath = filePath;
        Title = System.IO.Path.GetFileNameWithoutExtension(filePath);
    }
}