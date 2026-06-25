using NAudio.Wave;
using NAudio.Wave.SampleProviders;
using System;
using System.IO;
using System.Timers;

namespace MusicPlayer.Services;

public enum PlaybackState
{
    Stopped,
    Playing,
    Paused
}

public enum RepeatMode
{
    None,
    One,
    All
}

public class AudioEngine : IDisposable
{
    private WaveOutEvent? _outputDevice;
    private AudioFileReader? _audioFile;
    private System.Timers.Timer _progressTimer;
    private RepeatMode _repeatMode = RepeatMode.None;
    private bool _isShuffleEnabled = false;
    private float _volume = 1f;
    
    public event EventHandler<PlaybackState>? PlaybackStateChanged;
    public event EventHandler<TimeSpan>? PositionChanged;
    public event EventHandler<TimeSpan>? DurationChanged;
    public event EventHandler<float>? VolumeChanged;
    public event EventHandler? PlaybackEnded;
    
    public PlaybackState State { get; private set; } = PlaybackState.Stopped;
    public TimeSpan CurrentPosition => _audioFile?.CurrentTime ?? TimeSpan.Zero;
    public TimeSpan Duration => _audioFile?.TotalTime ?? TimeSpan.Zero;
    public float Volume
    {
        get => _volume;
        set
        {
            _volume = Math.Clamp(value, 0f, 1f);
            if (_outputDevice != null)
                _outputDevice.Volume = _volume;
            VolumeChanged?.Invoke(this, _volume);
        }
    }
    
    public RepeatMode RepeatMode
    {
        get => _repeatMode;
        set => _repeatMode = value;
    }
    
    public bool IsShuffleEnabled
    {
        get => _isShuffleEnabled;
        set => _isShuffleEnabled = value;
    }
    
    public AudioEngine()
    {
        _progressTimer = new System.Timers.Timer(100);
        _progressTimer.Elapsed += ProgressTimer_Elapsed;
    }
    
    private void CleanupDevice()
    {
        if (_outputDevice != null)
        {
            _outputDevice.PlaybackStopped -= OutputDevice_PlaybackStopped;
            try { _outputDevice.Stop(); } catch { }
            _outputDevice.Dispose();
            _outputDevice = null;
        }
        
        if (_audioFile != null)
        {
            _audioFile.Dispose();
            _audioFile = null;
        }
        
        _progressTimer.Stop();
    }
    
    public void LoadFile(string filePath)
    {
        CleanupDevice();
        
        if (!File.Exists(filePath))
            throw new FileNotFoundException("音频文件不存在", filePath);
        
        _audioFile = new AudioFileReader(filePath);
        _audioFile.Volume = _volume;
        _outputDevice = new WaveOutEvent();
        _outputDevice.Init(_audioFile);
        _outputDevice.PlaybackStopped += OutputDevice_PlaybackStopped;
        
        State = PlaybackState.Stopped;
        DurationChanged?.Invoke(this, Duration);
    }
    
    public void Play()
    {
        if (_outputDevice == null || _audioFile == null)
            return;
        
        _outputDevice.Play();
        State = PlaybackState.Playing;
        _progressTimer.Start();
        PlaybackStateChanged?.Invoke(this, State);
    }
    
    public void Pause()
    {
        if (_outputDevice == null || State != PlaybackState.Playing)
            return;
        
        _outputDevice.Pause();
        State = PlaybackState.Paused;
        _progressTimer.Stop();
        PlaybackStateChanged?.Invoke(this, State);
    }
    
    public void Stop()
    {
        if (_outputDevice == null)
            return;
        
        _outputDevice.Stop();
        if (_audioFile != null)
            _audioFile.CurrentTime = TimeSpan.Zero;
        State = PlaybackState.Stopped;
        _progressTimer.Stop();
        PositionChanged?.Invoke(this, CurrentPosition);
        PlaybackStateChanged?.Invoke(this, State);
    }
    
    public void Seek(TimeSpan position)
    {
        if (_audioFile == null)
            return;
        
        _audioFile.CurrentTime = position;
        PositionChanged?.Invoke(this, CurrentPosition);
    }
    
    public void SetPosition(double percentage)
    {
        if (_audioFile == null)
            return;
        
        var time = TimeSpan.FromSeconds(Duration.TotalSeconds * percentage);
        Seek(time);
    }
    
    private void ProgressTimer_Elapsed(object? sender, ElapsedEventArgs e)
    {
        PositionChanged?.Invoke(this, CurrentPosition);
    }
    
    private void OutputDevice_PlaybackStopped(object? sender, StoppedEventArgs e)
    {
        State = PlaybackState.Stopped;
        _progressTimer.Stop();
        PlaybackStateChanged?.Invoke(this, State);
        
        if (e.Exception == null)
        {
            PlaybackEnded?.Invoke(this, EventArgs.Empty);
        }
    }
    
    public void Dispose()
    {
        _progressTimer?.Dispose();
        CleanupDevice();
    }
}
