using MusicPlayer.Models;
using System;
using System.IO;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace MusicPlayer.Services;

public class OnlineLyricsService
{
    private static readonly HttpClient _http = new() { Timeout = TimeSpan.FromSeconds(8) };
    private readonly string _cachePath;
    private readonly LyricsService _lrcParser = new();

    public OnlineLyricsService()
    {
        _cachePath = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
            "MusicPlayer", "Lyrics");
        Directory.CreateDirectory(_cachePath);
    }

    public async Task<Lyrics?> GetLyricsAsync(string title, string artist, TimeSpan duration)
    {
        var cached = TryLoadCached(title, artist);
        if (cached != null) return cached;

        string? lrc = null;

        lrc = await SearchNeteaseAsync(title, artist);
        lrc ??= await SearchLrclibAsync(title, artist);
        lrc ??= await SearchLrclibAsync(title, null);

        if (lrc == null) return null;

        SaveCache(title, artist, lrc);
        return _lrcParser.ParseLrcString(lrc);
    }

    private Lyrics? TryLoadCached(string title, string artist)
    {
        var path = GetCachePath(title, artist);
        if (!File.Exists(path)) return null;

        try
        {
            var content = File.ReadAllText(path);
            if (string.IsNullOrWhiteSpace(content)) return null;
            return _lrcParser.ParseLrcString(content);
        }
        catch
        {
            return null;
        }
    }

    private async Task<string?> SearchLrclibAsync(string title, string? artist)
    {
        try
        {
            var encodedTitle = Uri.EscapeDataString(title);
            var url = string.IsNullOrWhiteSpace(artist)
                ? $"https://lrclib.net/api/search?track_name={encodedTitle}"
                : $"https://lrclib.net/api/search?track_name={encodedTitle}&artist_name={Uri.EscapeDataString(artist)}";

            var response = await _http.GetAsync(url);
            if (!response.IsSuccessStatusCode) return null;

            var json = await response.Content.ReadAsStringAsync();
            var results = JsonSerializer.Deserialize<JsonElement[]>(json);
            if (results == null || results.Length == 0) return null;

            var first = results[0];
            if (first.TryGetProperty("syncedLyrics", out var synced) && synced.ValueKind == JsonValueKind.String && !string.IsNullOrWhiteSpace(synced.GetString()))
                return synced.GetString();
            if (first.TryGetProperty("plainLyrics", out var plain) && plain.ValueKind == JsonValueKind.String && !string.IsNullOrWhiteSpace(plain.GetString()))
                return ToLrcFormat(plain.GetString());
            return null;
        }
        catch
        {
            return null;
        }
    }

    private async Task<string?> SearchNeteaseAsync(string title, string? artist)
    {
        try
        {
            var keyword = string.IsNullOrWhiteSpace(artist) ? title : $"{title} {artist}";
            var encodedKeyword = Uri.EscapeDataString(keyword);

            var searchUrl = $"https://music.163.com/api/search/get/web?s={encodedKeyword}&type=1&limit=5";
            var request = new HttpRequestMessage(HttpMethod.Get, searchUrl);
            request.Headers.Add("Referer", "https://music.163.com/");
            request.Headers.Add("User-Agent", "Mozilla/5.0");

            var response = await _http.SendAsync(request);
            if (!response.IsSuccessStatusCode) return null;

            var json = await response.Content.ReadAsStringAsync();
            var doc = JsonSerializer.Deserialize<JsonElement>(json);

            if (!doc.TryGetProperty("result", out var result))
                return null;
            if (!result.TryGetProperty("songs", out var songs) || songs.GetArrayLength() == 0)
                return null;

            var songId = songs[0].GetProperty("id").GetInt64();
            return await FetchNeteaseLyricsAsync(songId);
        }
        catch
        {
            return null;
        }
    }

    private async Task<string?> FetchNeteaseLyricsAsync(long songId)
    {
        try
        {
            var url = $"https://music.163.com/api/song/lyric?id={songId}&lv=1&tv=1";
            var request = new HttpRequestMessage(HttpMethod.Get, url);
            request.Headers.Add("Referer", "https://music.163.com/");
            request.Headers.Add("User-Agent", "Mozilla/5.0");

            var response = await _http.SendAsync(request);
            if (!response.IsSuccessStatusCode) return null;

            var json = await response.Content.ReadAsStringAsync();
            var doc = JsonSerializer.Deserialize<JsonElement>(json);

            if (doc.TryGetProperty("lrc", out var lrc) && lrc.TryGetProperty("lyric", out var lyric))
            {
                var text = lyric.GetString();
                if (!string.IsNullOrWhiteSpace(text))
                    return text;
            }

            return null;
        }
        catch
        {
            return null;
        }
    }

    private static string ToLrcFormat(string? plainText)
    {
        if (string.IsNullOrWhiteSpace(plainText)) return string.Empty;

        var lines = plainText.Split('\n');
        var result = new StringBuilder();
        var time = 0;
        foreach (var line in lines)
        {
            var trimmed = line.Trim();
            if (string.IsNullOrEmpty(trimmed)) continue;
            var minutes = time / 60;
            var seconds = time % 60;
            result.AppendLine($"[{minutes:D2}:{seconds:D2}.00]{trimmed}");
            time += 4;
        }
        return result.ToString();
    }

    private void SaveCache(string title, string artist, string lrc)
    {
        try
        {
            var path = GetCachePath(title, artist);
            File.WriteAllText(path, lrc);
        }
        catch
        {
        }
    }

    private string GetCachePath(string title, string artist)
    {
        var safeName = $"{Sanitize(artist)} - {Sanitize(title)}.lrc";
        return Path.Combine(_cachePath, safeName);
    }

    private static string Sanitize(string name)
    {
        foreach (var c in Path.GetInvalidFileNameChars())
            name = name.Replace(c, '_');
        return name.Trim();
    }
}
