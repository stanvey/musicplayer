using MusicPlayer.Models;
using System;
using System.Collections.ObjectModel;
using System.IO;
using System.Text.Json;
using System.Threading.Tasks;

namespace MusicPlayer.Services;

public class PlaylistService
{
    private readonly string _playlistsPath;
    
    public PlaylistService()
    {
        _playlistsPath = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
            "MusicPlayer",
            "Playlists"
        );
        
        Directory.CreateDirectory(_playlistsPath);
    }
    
    public async Task SavePlaylistAsync(Playlist playlist)
    {
        var filePath = Path.Combine(_playlistsPath, $"{playlist.Name}.json");
        var options = new JsonSerializerOptions
        {
            WriteIndented = true
        };
        
        var json = JsonSerializer.Serialize(playlist, options);
        await File.WriteAllTextAsync(filePath, json);
    }
    
    public async Task<Playlist?> LoadPlaylistAsync(string name)
    {
        var filePath = Path.Combine(_playlistsPath, $"{name}.json");
        
        if (!File.Exists(filePath))
            return null;
        
        var json = await File.ReadAllTextAsync(filePath);
        return JsonSerializer.Deserialize<Playlist>(json);
    }
    
    public async Task<ObservableCollection<Playlist>> LoadAllPlaylistsAsync()
    {
        var playlists = new ObservableCollection<Playlist>();
        
        if (!Directory.Exists(_playlistsPath))
            return playlists;
        
        var files = Directory.GetFiles(_playlistsPath, "*.json");
        
        foreach (var file in files)
        {
            try
            {
                var json = await File.ReadAllTextAsync(file);
                var playlist = JsonSerializer.Deserialize<Playlist>(json);
                if (playlist != null)
                    playlists.Add(playlist);
            }
            catch (JsonException)
            {
                // 跳过无效的播放列表文件
            }
        }
        
        return playlists;
    }
    
    public void DeletePlaylist(string name)
    {
        var filePath = Path.Combine(_playlistsPath, $"{name}.json");
        
        if (File.Exists(filePath))
            File.Delete(filePath);
    }
}
