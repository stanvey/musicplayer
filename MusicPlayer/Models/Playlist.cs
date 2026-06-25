using System.Collections.ObjectModel;

namespace MusicPlayer.Models;

public class Playlist
{
    public string Name { get; set; } = string.Empty;
    public ObservableCollection<Song> Songs { get; set; } = new();
    
    public Playlist() { }
    
    public Playlist(string name)
    {
        Name = name;
    }
}