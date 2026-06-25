# 🎵 Music Player

Apple Music 风格的 Windows 本地音乐播放器，支持自动获取专辑封面和同步歌词。

## 功能

- 支持 MP3 / WAV / FLAC / AAC / OGG 格式
- 自动读取音频元数据（歌名、艺术家、专辑）
- 专辑封面自动下载（iTunes API）
- 歌词自动获取（网易云音乐 + lrclib）
- 播放列表管理（保存 / 加载 / 搜索过滤）
- 迷你悬浮播放窗口
- 系统托盘集成 + 快捷键
- 深色 Apple Music 风格界面

## 快捷键

| 按键 | 功能 |
|------|------|
| Space | 播放 / 暂停 |
| ← | 上一首 |
| → | 下一首 |
| ↑ | 音量增加 |
| ↓ | 音量减少 |
| M | 切换迷你模式 |

## 运行方式

### 方式一：直接运行 exe

下载 `MusicPlayer.exe`，双击即可运行（不需要安装任何依赖）。

### 方式二：从源码构建

需要安装 [.NET 8 SDK](https://dotnet.microsoft.com/download/dotnet/8.0) 或更高版本。

```bash
# 克隆项目
git clone https://github.com/stanvey/musicplayer.git
cd musicplayer

# 编译运行
dotnet run --project MusicPlayer

# 或发布为独立 exe
dotnet publish MusicPlayer -c Release -r win-x64 --self-contained true -p:PublishSingleFile=true
```

## 技术栈

- C# / .NET 8
- WPF
- NAudio（音频播放）
- TagLib#（元数据读取）
- CommunityToolkit.Mvvm（MVVM 框架）

## 系统要求

- Windows 10 / 11
- .NET 8 Runtime（从源码运行时需要，独立 exe 不需要）
