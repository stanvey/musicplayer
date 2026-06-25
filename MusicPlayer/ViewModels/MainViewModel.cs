using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Win32;
using MusicPlayer.Models;
using MusicPlayer.Services;
using MusicPlayer.Windows;
using System;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Windows;

namespace MusicPlayer.ViewModels;

public partial class MainViewModel : ObservableObject
{
    private readonly AudioEngine _audioEngine = new();
    private readonly PlaylistService _playlistService = new();
    private readonly LyricsService _lyricsService = new();
    private readonly MetadataService _metadataService = new();
    private readonly OnlineLyricsService _onlineLyricsService = new();
    
    [ObservableProperty]
    private Song? _currentSong;
    
    [ObservableProperty]
    private PlaybackState _playbackState = PlaybackState.Stopped;
    
    [ObservableProperty]
    private TimeSpan _currentPosition;
    
    [ObservableProperty]
    private TimeSpan _duration;
    
    [ObservableProperty]
    private float _volume = 0.8f;
    
    [ObservableProperty]
    private double _progress;
    
    private bool _isSeeking;
    public bool IsSeeking { get => _isSeeking; set => _isSeeking = value; }
    
    [ObservableProperty]
    private bool _isPlaying;
    
    [ObservableProperty]
    private string _playButtonContent = "▶";
    
    [ObservableProperty]
    private Playlist? _currentPlaylist;
    
    [ObservableProperty]
    private string _newPlaylistName = string.Empty;
    
    [ObservableProperty]
    private Lyrics? _currentLyrics;
    
    [ObservableProperty]
    private string _currentLyricText = string.Empty;
    
    [ObservableProperty]
    private int _currentLyricIndex;
    
    [ObservableProperty]
    private string? _currentCoverPath;
    
    public ObservableCollection<Song> Songs { get; } = new();
    public ObservableCollection<Playlist> Playlists { get; } = new();
    public ObservableCollection<string> LyricLines { get; } = new();
    
    public MainViewModel()
    {
        _audioEngine.PlaybackStateChanged += OnPlaybackStateChanged;
        _audioEngine.PositionChanged += OnPositionChanged;
        _audioEngine.DurationChanged += OnDurationChanged;
        _audioEngine.PlaybackEnded += OnPlaybackEnded;
        _audioEngine.Volume = Volume;
        
        LoadPlaylists();
    }
    
    private async void LoadPlaylists()
    {
        var playlists = await _playlistService.LoadAllPlaylistsAsync();
        foreach (var playlist in playlists)
        {
            Playlists.Add(playlist);
        }
    }
    
    private void OnPlaybackStateChanged(object? sender, PlaybackState state)
    {
        System.Windows.Application.Current.Dispatcher.Invoke(() =>
        {
            PlaybackState = state;
            IsPlaying = state == PlaybackState.Playing;
            PlayButtonContent = state == PlaybackState.Playing ? "⏸" : "▶";
        });
    }
    
    private void OnPositionChanged(object? sender, TimeSpan position)
    {
        if (_isSeeking) return;
        
        System.Windows.Application.Current.Dispatcher.Invoke(() =>
        {
            CurrentPosition = position;
            if (Duration.TotalSeconds > 0)
                Progress = position.TotalSeconds / Duration.TotalSeconds * 100;
            
            UpdateCurrentLyric(position);
        });
    }
    
    private void OnDurationChanged(object? sender, TimeSpan duration)
    {
        System.Windows.Application.Current.Dispatcher.Invoke(() =>
        {
            Duration = duration;
        });
    }
    
    private void OnPlaybackEnded(object? sender, EventArgs e)
    {
        System.Windows.Application.Current.Dispatcher.Invoke(() =>
        {
            PlayNext();
        });
    }
    
    private void UpdateCurrentLyric(TimeSpan position)
    {
        if (CurrentLyrics == null) return;
        
        var lineIndex = CurrentLyrics.GetCurrentLineIndex(position);
        if (lineIndex != CurrentLyricIndex)
        {
            CurrentLyricIndex = lineIndex;
            CurrentLyricText = CurrentLyrics.Lines[lineIndex].Text;
        }
    }
    
    [RelayCommand]
    private void PlayPause()
    {
        if (CurrentSong == null)
        {
            if (Songs.Count > 0)
                PlaySong(Songs[0]);
            return;
        }
        
        if (PlaybackState == PlaybackState.Playing)
            _audioEngine.Pause();
        else
            _audioEngine.Play();
    }
    
    [RelayCommand]
    private void VolumeUp()
    {
        Volume = Math.Min(1f, Volume + 0.1f);
    }

    [RelayCommand]
    private void VolumeDown()
    {
        Volume = Math.Max(0f, Volume - 0.1f);
    }

    [RelayCommand]
    private void Stop()
    {
        _audioEngine.Stop();
    }
    
    [RelayCommand]
    private void PlayNext()
    {
        if (Songs.Count == 0) return;
        
        var currentIndex = CurrentSong != null ? Songs.IndexOf(CurrentSong) : -1;
        var nextIndex = (currentIndex + 1) % Songs.Count;
        PlaySong(Songs[nextIndex]);
    }
    
    [RelayCommand]
    private void PlayPrevious()
    {
        if (Songs.Count == 0) return;
        
        var currentIndex = CurrentSong != null ? Songs.IndexOf(CurrentSong) : 0;
        var prevIndex = currentIndex == 0 ? Songs.Count - 1 : currentIndex - 1;
        PlaySong(Songs[prevIndex]);
    }
    
    [RelayCommand]
    private void AddFiles()
    {
        var dialog = new Microsoft.Win32.OpenFileDialog
        {
            Multiselect = true,
            Filter = "音频文件|*.mp3;*.wav;*.flac;*.aac;*.ogg|所有文件|*.*"
        };
        
        if (dialog.ShowDialog() == true)
        {
            foreach (var file in dialog.FileNames)
            {
                var song = _metadataService.LoadMetadata(file);
                Songs.Add(song);
            }
        }
    }
    
    [RelayCommand]
    private void AddFolder()
    {
        var dialog = new OpenFolderDialog
        {
            Title = "选择音乐文件夹"
        };
        
        if (dialog.ShowDialog() == true)
        {
            var folder = dialog.FolderName;
            var audioExtensions = new[] { ".mp3", ".wav", ".flac", ".aac", ".ogg" };
            
            var files = Directory.GetFiles(folder, "*.*", SearchOption.AllDirectories)
                .Where(f => audioExtensions.Contains(Path.GetExtension(f).ToLowerInvariant()));
            
            foreach (var file in files)
            {
                var song = _metadataService.LoadMetadata(file);
                Songs.Add(song);
            }
        }
    }
    
    [RelayCommand]
    private async void PlaySong(Song song)
    {
        try
        {
            Progress = 0;
            CurrentPosition = TimeSpan.Zero;
            CurrentLyricText = "暂无歌词";
            LyricLines.Clear();
            
            _audioEngine.LoadFile(song.FilePath);
            CurrentSong = song;
            CurrentCoverPath = song.CoverPath;
            _audioEngine.Play();
            
            if (CurrentCoverPath == null && !string.IsNullOrWhiteSpace(song.Title))
            {
                CurrentCoverPath = await _metadataService.FetchCoverOnlineAsync(song.Title, song.Artist, song.FilePath);
                song.CoverPath = CurrentCoverPath;
            }
            
            CurrentLyrics = await _lyricsService.LoadLyricsAsync(song.FilePath);
            
            if (CurrentLyrics == null && !string.IsNullOrWhiteSpace(song.Title))
            {
                CurrentLyrics = await _onlineLyricsService.GetLyricsAsync(song.Title, song.Artist, song.Duration);
            }
            
            LyricLines.Clear();
            if (CurrentLyrics != null)
            {
                foreach (var line in CurrentLyrics.Lines)
                {
                    LyricLines.Add(line.Text);
                }
            }
            CurrentLyricIndex = 0;
            CurrentLyricText = CurrentLyrics?.Lines.FirstOrDefault()?.Text ?? "暂无歌词";
        }
        catch (Exception ex)
        {
            System.Windows.MessageBox.Show($"播放失败：{ex.Message}", "错误", System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Error);
        }
    }
    
    [RelayCommand]
    public void SeekToPosition()
    {
        if (Progress >= 0 && Progress <= 100)
        {
            _audioEngine.SetPosition(Progress / 100);
        }
    }
    
    public void SeekToPercentage(double percentage)
    {
        if (percentage >= 0 && percentage <= 100)
        {
            _audioEngine.SetPosition(percentage / 100);
        }
    }
    
    [RelayCommand]
    private void RemoveSong(Song song)
    {
        if (song == CurrentSong)
        {
            _audioEngine.Stop();
            CurrentSong = null;
        }
        
        Songs.Remove(song);
    }
    
    [RelayCommand]
    private void MoveSongUp(Song song)
    {
        var index = Songs.IndexOf(song);
        if (index > 0)
        {
            Songs.Move(index, index - 1);
        }
    }
    
    [RelayCommand]
    private void MoveSongDown(Song song)
    {
        var index = Songs.IndexOf(song);
        if (index < Songs.Count - 1)
        {
            Songs.Move(index, index + 1);
        }
    }
    
    [RelayCommand]
    private async void SavePlaylist()
    {
        if (string.IsNullOrWhiteSpace(NewPlaylistName))
        {
            System.Windows.MessageBox.Show("请输入播放列表名称", "提示", System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Warning);
            return;
        }
        
        var playlist = new Playlist(NewPlaylistName)
        {
            Songs = new ObservableCollection<Song>(Songs)
        };
        
        await _playlistService.SavePlaylistAsync(playlist);
        Playlists.Add(playlist);
        CurrentPlaylist = playlist;
        NewPlaylistName = string.Empty;
        
        System.Windows.MessageBox.Show("播放列表已保存", "提示", System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Information);
    }
    
    [RelayCommand]
    private void LoadPlaylist(Playlist playlist)
    {
        Songs.Clear();
        foreach (var song in playlist.Songs)
        {
            Songs.Add(song);
        }
        CurrentPlaylist = playlist;
    }
    
    [RelayCommand]
    private void DeletePlaylist(Playlist playlist)
    {
        if (CurrentPlaylist == playlist)
        {
            CurrentPlaylist = null;
        }
        
        _playlistService.DeletePlaylist(playlist.Name);
        Playlists.Remove(playlist);
    }
    
    [RelayCommand]
    public void SwitchToMiniMode()
    {
        var mainWindow = System.Windows.Application.Current.MainWindow;
        mainWindow.Hide();
        
        var miniWindow = new MiniPlayerWindow(this);
        miniWindow.Show();
    }
    
    partial void OnVolumeChanged(float value)
    {
        _audioEngine.Volume = value;
    }
    
    public void Cleanup()
    {
        _audioEngine.Dispose();
    }
}
