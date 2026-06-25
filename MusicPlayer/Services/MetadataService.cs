using MusicPlayer.Models;
using System;
using System.IO;
using System.Net.Http;
using System.Text.Json;
using System.Threading.Tasks;

namespace MusicPlayer.Services;

public class MetadataService
{
    private static readonly HttpClient _http = new();
    private readonly string _cachePath;

    public MetadataService()
    {
        _cachePath = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
            "MusicPlayer", "Covers");
        Directory.CreateDirectory(_cachePath);
    }

    public Song LoadMetadata(string filePath)
    {
        var song = new Song(filePath);

        try
        {
            using var tagFile = TagLib.File.Create(filePath);

            if (!string.IsNullOrWhiteSpace(tagFile.Tag.Title))
                song.Title = tagFile.Tag.Title;
            if (!string.IsNullOrWhiteSpace(tagFile.Tag.FirstPerformer))
                song.Artist = tagFile.Tag.FirstPerformer;
            if (!string.IsNullOrWhiteSpace(tagFile.Tag.Album))
                song.Album = tagFile.Tag.Album;
            if (tagFile.Properties.Duration.TotalSeconds > 0)
                song.Duration = tagFile.Properties.Duration;

            song.CoverPath = ExtractCover(tagFile, filePath);
        }
        catch
        {
        }

        if (string.IsNullOrWhiteSpace(song.Artist))
            ParseFromFileName(song, filePath);

        return song;
    }

    private static void ParseFromFileName(Song song, string filePath)
    {
        var name = Path.GetFileNameWithoutExtension(filePath);
        if (string.IsNullOrWhiteSpace(name)) return;

        var separatorIndex = name.IndexOf(" - ");
        if (separatorIndex <= 0 || separatorIndex >= name.Length - 3)
            separatorIndex = name.IndexOf("- ");
        if (separatorIndex <= 0 || separatorIndex >= name.Length - 2)
            separatorIndex = name.IndexOf(" -");

        if (separatorIndex > 0 && separatorIndex < name.Length - 2)
        {
            var possibleArtist = name[..separatorIndex].Trim();
            var possibleTitle = name[(separatorIndex + 1)..].Trim().TrimStart('-').Trim();

            if (!string.IsNullOrWhiteSpace(possibleArtist) && !string.IsNullOrWhiteSpace(possibleTitle))
            {
                song.Artist = possibleArtist;
                song.Title = possibleTitle;
                return;
            }
        }

        song.Title = name;
    }

    private string? ExtractCover(TagLib.File tagFile, string filePath)
    {
        var pictures = tagFile.Tag.Pictures;
        if (pictures != null && pictures.Length > 0)
        {
            try
            {
                var picture = pictures[0];
                var data = picture.Data.Data;
                if (data != null && data.Length > 0)
                {
                    var hash = Math.Abs(filePath.GetHashCode()).ToString("X8");
                    var ext = picture.MimeType switch
                    {
                        "image/png" => ".png",
                        "image/jpeg" => ".jpg",
                        _ => ".jpg"
                    };
                    var coverPath = Path.Combine(_cachePath, $"{hash}{ext}");
                    if (File.Exists(coverPath))
                        return coverPath;
                    File.WriteAllBytes(coverPath, data);
                    return coverPath;
                }
            }
            catch
            {
            }
        }

        return GetCachedCoverPath(filePath);
    }

    public string? GetCachedCoverPath(string filePath)
    {
        var hash = Math.Abs(filePath.GetHashCode()).ToString("X8");
        var jpgPath = Path.Combine(_cachePath, $"{hash}.jpg");
        if (File.Exists(jpgPath)) return jpgPath;
        var pngPath = Path.Combine(_cachePath, $"{hash}.png");
        if (File.Exists(pngPath)) return pngPath;
        return null;
    }

    public async Task<string?> FetchCoverOnlineAsync(string title, string artist, string filePath)
    {
        var cached = GetCachedCoverPath(filePath);
        if (cached != null) return cached;

        try
        {
            var query = Uri.EscapeDataString($"{artist} {title}");
            var url = $"https://itunes.apple.com/search?term={query}&media=music&limit=1";
            var response = await _http.GetAsync(url);
            if (!response.IsSuccessStatusCode) return null;

            var json = await response.Content.ReadAsStringAsync();
            var doc = JsonSerializer.Deserialize<JsonElement>(json);
            if (!doc.TryGetProperty("results", out var results) || results.GetArrayLength() == 0)
                return null;

            var artworkUrl = results[0].GetProperty("artworkUrl100").GetString();
            if (string.IsNullOrWhiteSpace(artworkUrl)) return null;

            artworkUrl = artworkUrl.Replace("100x100", "600x600");

            var imageBytes = await _http.GetByteArrayAsync(artworkUrl);
            if (imageBytes == null || imageBytes.Length == 0) return null;

            var hash = Math.Abs(filePath.GetHashCode()).ToString("X8");
            var coverPath = Path.Combine(_cachePath, $"{hash}.jpg");
            File.WriteAllBytes(coverPath, imageBytes);
            return coverPath;
        }
        catch
        {
            return null;
        }
    }
}
