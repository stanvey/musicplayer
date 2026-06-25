using MusicPlayer.Models;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace MusicPlayer.Services;

public class LyricsService
{
    private static readonly Regex TimeRegex = new(@"\[(\d{2}):(\d{2})\.(\d{2,3})\]", RegexOptions.Compiled);
    private static readonly Regex MetadataRegex = new(@"\[(\w+):(.+)\]", RegexOptions.Compiled);
    
    public async Task<Lyrics?> LoadLyricsAsync(string songFilePath)
    {
        var directory = Path.GetDirectoryName(songFilePath);
        var fileNameWithoutExtension = Path.GetFileNameWithoutExtension(songFilePath);
        
        var lrcFiles = Directory.GetFiles(directory ?? string.Empty, "*.lrc")
            .Where(f => Path.GetFileNameWithoutExtension(f).Contains(fileNameWithoutExtension, StringComparison.OrdinalIgnoreCase))
            .ToList();
        
        if (lrcFiles.Count == 0)
        {
            var exactMatch = Path.Combine(directory ?? string.Empty, $"{fileNameWithoutExtension}.lrc");
            if (File.Exists(exactMatch))
                lrcFiles.Add(exactMatch);
        }
        
        if (lrcFiles.Count == 0)
            return null;
        
        return await ParseLrcFileAsync(lrcFiles[0]);
    }
    
    public async Task<Lyrics?> ParseLrcFileAsync(string filePath)
    {
        if (!File.Exists(filePath))
            return null;
        
        var lines = await File.ReadAllLinesAsync(filePath);
        var lyrics = new Lyrics();
        
        foreach (var line in lines)
        {
            if (string.IsNullOrWhiteSpace(line))
                continue;
            
            var metadataMatch = MetadataRegex.Match(line);
            if (metadataMatch.Success && !TimeRegex.IsMatch(line))
            {
                continue;
            }
            
            var timeMatches = TimeRegex.Matches(line);
            if (timeMatches.Count == 0)
                continue;
            
            var textStartIndex = line.LastIndexOf(']') + 1;
            var text = textStartIndex < line.Length ? line[textStartIndex..].Trim() : string.Empty;
            
            if (string.IsNullOrEmpty(text))
                continue;
            
            foreach (Match timeMatch in timeMatches)
            {
                var time = ParseTime(timeMatch);
                lyrics.Lines.Add(new LyricLine(time, text));
            }
        }
        
        lyrics.Lines = lyrics.Lines.OrderBy(l => l.Time).ToList();
        
        return lyrics.Lines.Count > 0 ? lyrics : null;
    }
    
    private TimeSpan ParseTime(Match match)
    {
        var minutes = int.Parse(match.Groups[1].Value);
        var seconds = int.Parse(match.Groups[2].Value);
        var milliseconds = int.Parse(match.Groups[3].Value);
        
        if (match.Groups[3].Value.Length == 2)
            milliseconds *= 10;
        
        return new TimeSpan(0, 0, minutes, seconds, milliseconds);
    }
    
    public Task<Lyrics?> ParseLrcStringAsync(string lrcContent)
    {
        return Task.FromResult(ParseLrcString(lrcContent));
    }
    
    public Lyrics? ParseLrcString(string lrcContent)
    {
        if (string.IsNullOrWhiteSpace(lrcContent))
            return null;
        
        var lines = lrcContent.Split('\n');
        var lyrics = new Lyrics();
        
        foreach (var line in lines)
        {
            if (string.IsNullOrWhiteSpace(line))
                continue;
            
            var trimmed = line.Trim();
            var metadataMatch = MetadataRegex.Match(trimmed);
            if (metadataMatch.Success && !TimeRegex.IsMatch(trimmed))
                continue;
            
            var timeMatches = TimeRegex.Matches(trimmed);
            if (timeMatches.Count == 0)
                continue;
            
            var textStartIndex = trimmed.LastIndexOf(']') + 1;
            var text = textStartIndex < trimmed.Length ? trimmed[textStartIndex..].Trim() : string.Empty;
            
            if (string.IsNullOrEmpty(text))
                continue;
            
            foreach (Match timeMatch in timeMatches)
            {
                var time = ParseTime(timeMatch);
                lyrics.Lines.Add(new LyricLine(time, text));
            }
        }
        
        lyrics.Lines = lyrics.Lines.OrderBy(l => l.Time).ToList();
        return lyrics.Lines.Count > 0 ? lyrics : null;
    }
}
