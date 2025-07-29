# BG-Tunes - System Tray Music Player

A lightweight C# system tray music player that automatically pauses playback when audio devices disconnect (e.g., headphones unplugged).

## Features

- **🎵 Multi-Song Support**: Switch between multiple songs and add your own via file dialog
- **🎧 Audio Device Monitoring**: Automatically pauses when headphones are unplugged or audio output changes
- **🔊 Fail-Safe Defaults**: Starts at 25% volume to prevent accidental loud playback
- **💻 System Tray Interface**: Modern dark-themed tray menu with status display and controls
- **⏸️ No Auto-Resume**: Playback only resumes when manually triggered by the user
- **🎨 Modern UI**: Clean, compact design with hover effects and emoji icons

## Setup

1. **Add your MP3 files**: Place your MP3 files in the `Resources` folder
   - The app will automatically detect all MP3 files in the Resources folder
   - You can add more songs via the "Add Song..." option in the menu
   - If no MP3 files are found, the app will prompt you to add songs

2. **Build and run**:
   ```bash
   dotnet build
   dotnet run
   ```

## Usage

- **Right-click the tray icon** to access the menu
- **Status display** shows current playback state and audio device
- **Song selection**: Choose from available songs or add new ones
- **Play/Pause**: Control playback
- **Stop**: Stop playback and reset to beginning
- **Volume slider**: Adjust volume (starts at 25%)
- **Exit**: Close the application

## Safety Features

- **Volume Control**: Always starts at 25% volume
- **Device Monitoring**: Instantly pauses when audio devices disconnect
- **No Auto-Resume**: Must manually resume after device reconnection
- **Error Handling**: Comprehensive error handling and logging
- **Resource Management**: Proper cleanup of audio resources

## Testing

To test the device monitoring:
1. Start playback
2. Unplug headphones or change audio output
3. Verify playback pauses automatically
4. Reconnect device and verify playback doesn't auto-resume
5. Manually click Play to resume

## Requirements

- .NET 6.0 or later
- Windows (uses Windows Forms and Core Audio APIs)
- NAudio library (automatically restored via NuGet)

## File Structure

```
BG-Tunes/
├── Program.cs              # Application entry point
├── TrayMusicPlayer.cs      # Main system tray player
├── BackgroundTunes.csproj   # Project file
├── Resources/              # Place your MP3 files here
├── run.bat                # Quick launcher script
└── README.md              # This file
``` 