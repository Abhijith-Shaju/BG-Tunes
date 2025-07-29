using System;
using System.Drawing;
using System.IO;
using System.Runtime.InteropServices;
using System.Windows.Forms;
using NAudio.Wave;
using NAudio.CoreAudioApi;
using NAudio.CoreAudioApi.Interfaces;
using System.Linq;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Diagnostics;
using System.Reflection;

namespace BackgroundTunes
{
    public partial class TrayMusicPlayer : Form
    {
        private NotifyIcon? trayIcon;
        private ContextMenuStrip? trayMenu;
        private WaveOutEvent? waveOut;
        private AudioFileReader? audioFile;
        private bool isPlaying = false;
        private bool isPausedByDevice = false;
        private string currentDeviceName = "";
        private string currentSongName = "";
        private MMDeviceEnumerator? deviceEnumerator;
        private DeviceChangeHandler? deviceChangeHandler;
        private List<(string DisplayName, string FilePath, bool IsBuiltIn)> songs = new();
        private int currentSongIndex = 0;
        private bool isDisposed = false;
        private string? tempAudioFile = null;

        // Constants
        private const float DEFAULT_VOLUME = 0.25f;
        private const string APP_NAME = "BG-Tunes";
        private const string RESOURCES_FOLDER = "Resources";
        private const int BALLOON_TIP_DURATION = 3000;
        private const int MAX_SONG_NAME_LENGTH = 50;

        public TrayMusicPlayer()
        {
            try
            {
                InitializeComponent();
                EnsureResourcesFolderExists();
                InitializeTrayIcon();
                InitializeAudioDeviceMonitoring();
                LoadDefaultSong();
            }
            catch (Exception ex)
            {
                LogError("Failed to initialize application", ex);
                MessageBox.Show($"Failed to initialize application: {ex.Message}", APP_NAME, MessageBoxButtons.OK, MessageBoxIcon.Error);
                Application.Exit();
            }
        }

        private void EnsureResourcesFolderExists()
        {
            try
            {
                if (!Directory.Exists(RESOURCES_FOLDER))
                {
                    Directory.CreateDirectory(RESOURCES_FOLDER);
                }
            }
            catch (Exception ex)
            {
                LogError("Failed to create Resources folder", ex);
                throw;
            }
        }

        private void InitializeComponent()
        {
            this.SuspendLayout();
            this.AutoScaleDimensions = new System.Drawing.SizeF(7F, 15F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(0, 0);
            this.Name = "TrayMusicPlayer";
            this.ShowInTaskbar = false;
            this.WindowState = FormWindowState.Minimized;
            this.FormBorderStyle = FormBorderStyle.FixedToolWindow;
            this.ResumeLayout(false);
        }

        private void InitializeTrayIcon()
        {
            try
            {
                // Use the application's own icon for the tray
                trayIcon = new NotifyIcon
                {
                    Icon = Icon.ExtractAssociatedIcon(Application.ExecutablePath),
                    Text = APP_NAME,
                    Visible = true
                };

                trayMenu = new ContextMenuStrip();
                trayMenu.BackColor = Color.FromArgb(45, 45, 48);
                trayMenu.ForeColor = Color.White;
                trayMenu.Font = new Font("Segoe UI", 9F, FontStyle.Regular);
                trayMenu.Renderer = new CustomMenuRenderer();
                trayMenu.Padding = Padding.Empty;
                trayMenu.Margin = Padding.Empty;
                
                // Title section
                var titleLabel = new ToolStripLabel("🎵 BG-Tunes");
                titleLabel.Font = new Font("Segoe UI", 10F, FontStyle.Bold);
                titleLabel.ForeColor = Color.FromArgb(0, 122, 204);
                trayMenu.Items.Add(titleLabel);
                trayMenu.Items.Add(new ToolStripSeparator());

                // Status section with icon
                var statusLabel = new ToolStripLabel("⏸️ Status: Stopped");
                statusLabel.Name = "statusLabel";
                statusLabel.Font = new Font("Segoe UI", 9F, FontStyle.Regular);
                trayMenu.Items.Add(statusLabel);
                
                // Song selection submenu
                var songMenu = new ToolStripMenuItem("🎶 Song: " + currentSongName) { Name = "songMenu" };
                UpdateSongMenu(songMenu);
                trayMenu.Items.Add(songMenu);
                trayMenu.Items.Add(new ToolStripSeparator());

                // Play/Pause button with icon
                var playPauseButton = new ToolStripMenuItem("▶️ Play");
                playPauseButton.Name = "playPauseButton";
                playPauseButton.Click += PlayPause_Click;
                playPauseButton.Font = new Font("Segoe UI", 9F, FontStyle.Regular);
                trayMenu.Items.Add(playPauseButton);

                // Stop button with icon
                var stopButton = new ToolStripMenuItem("⏹️ Stop");
                stopButton.Click += Stop_Click;
                stopButton.Font = new Font("Segoe UI", 9F, FontStyle.Regular);
                trayMenu.Items.Add(stopButton);

                trayMenu.Items.Add(new ToolStripSeparator());

                            // Ultra-compact volume control
            var volumeLabel = new ToolStripLabel("🔊 25%");
            volumeLabel.Name = "volumeLabel";
            volumeLabel.Font = new Font("Segoe UI", 9F, FontStyle.Regular);
            volumeLabel.Margin = new Padding(5, 2, 0, 2);
            trayMenu.Items.Add(volumeLabel);

            // Compact volume slider
            var trackBar = new CustomTrackBar();
            trackBar.Minimum = 0;
            trackBar.Maximum = 100;
            trackBar.Value = 25;
            trackBar.Width = 150;
            trackBar.Height = 20;
            trackBar.ValueChanged += VolumeSlider_ValueChanged;
            trackBar.BackColor = Color.FromArgb(45, 45, 48);
            trackBar.ForeColor = Color.FromArgb(0, 122, 204);
            trackBar.TickStyle = TickStyle.None;
            var volumeSlider = new ToolStripControlHost(trackBar);
            volumeSlider.Margin = new Padding(5, 0, 5, 0);
            trayMenu.Items.Add(volumeSlider);

                trayMenu.Items.Add(new ToolStripSeparator());

                // Exit button with icon
                var exitButton = new ToolStripMenuItem("❌ Exit");
                exitButton.Click += Exit_Click;
                exitButton.Font = new Font("Segoe UI", 9F, FontStyle.Regular);
                trayMenu.Items.Add(exitButton);

                trayIcon.ContextMenuStrip = trayMenu;
                trayIcon.DoubleClick += TrayIcon_DoubleClick;

                UpdateStatus("Stopped");
            }
            catch (Exception ex)
            {
                LogError("Failed to initialize tray icon", ex);
                throw;
            }
        }

        private void InitializeAudioDeviceMonitoring()
        {
            try
            {
                deviceEnumerator = new MMDeviceEnumerator();
                deviceChangeHandler = new DeviceChangeHandler(this);
                deviceEnumerator.RegisterEndpointNotificationCallback(deviceChangeHandler);

                // Check initial device status
                CheckAudioDeviceStatus();
            }
            catch (Exception ex)
            {
                LogError("Failed to initialize audio device monitoring", ex);
                // Don't throw - app can still work without device monitoring
            }
        }

        private void CheckAudioDeviceStatus()
        {
            try
            {
                var defaultDevice = deviceEnumerator?.GetDefaultAudioEndpoint(DataFlow.Render, Role.Multimedia);
                if (defaultDevice == null) throw new Exception("No audio device found");
                currentDeviceName = defaultDevice.FriendlyName;
                UpdateStatus(isPlaying ? "Playing" : "Stopped", currentDeviceName);
                
                // Enable play button if device is available
                var playButton = trayMenu?.Items.Find("playPauseButton", false).FirstOrDefault() as ToolStripMenuItem;
                if (playButton != null)
                    playButton.Enabled = true;
            }
            catch (Exception ex)
            {
                LogError("Failed to check audio device status", ex);
                currentDeviceName = "No Device";
                UpdateStatus("Stopped", currentDeviceName);
                
                // Disable play button if no device
                var playButton = trayMenu?.Items.Find("playPauseButton", false).FirstOrDefault() as ToolStripMenuItem;
                if (playButton != null)
                    playButton.Enabled = false;

                trayIcon?.ShowBalloonTip(BALLOON_TIP_DURATION, APP_NAME, "No audio device detected – muted", ToolTipIcon.Warning);
            }
        }

        private void LoadDefaultSong()
        {
            try
            {
                songs.Clear();
                
                // Load embedded MP3 resources
                var assembly = Assembly.GetExecutingAssembly();
                var resourceNames = assembly.GetManifestResourceNames()
                    .Where(name => name.EndsWith(".mp3"))
                    .ToList();

                foreach (var resourceName in resourceNames)
                {
                    string displayName = Path.GetFileNameWithoutExtension(resourceName);
                    // Remove namespace prefix if present
                    if (displayName.Contains("."))
                    {
                        displayName = displayName.Substring(displayName.LastIndexOf('.') + 1);
                    }
                    
                    // Truncate long names
                    if (displayName.Length > MAX_SONG_NAME_LENGTH)
                    {
                        displayName = displayName.Substring(0, MAX_SONG_NAME_LENGTH - 3) + "...";
                    }
                    
                    songs.Add((displayName, resourceName, true)); // true = embedded resource
                }
                
                // Handle case when no songs are found
                if (songs.Count == 0)
                {
                    songs.Add(("No songs found", "", true));
                    trayIcon?.ShowBalloonTip(BALLOON_TIP_DURATION, APP_NAME, "No songs found - please add songs to Resources folder", ToolTipIcon.Warning);
                }
                else
                {
                    trayIcon?.ShowBalloonTip(BALLOON_TIP_DURATION, APP_NAME, $"Loaded {songs.Count} embedded song(s)", ToolTipIcon.Info);
                }
                
                currentSongIndex = 0;
                LoadSong(currentSongIndex);
            }
            catch (Exception ex)
            {
                LogError("Failed to load default songs", ex);
                songs.Add(("Error loading songs", "", true));
                currentSongIndex = 0;
                LoadSong(currentSongIndex);
            }
        }

        private void LoadSong(int index)
        {
            try
            {
                if (audioFile != null)
                {
                    audioFile.Dispose();
                    audioFile = null;
                }
                
                if (index < 0 || index >= songs.Count)
                    return;
                    
                var song = songs[index];
                currentSongName = song.DisplayName;
                
                if (song.IsBuiltIn && !string.IsNullOrEmpty(song.FilePath))
                {
                    // Load from embedded resource
                    var assembly = Assembly.GetExecutingAssembly();
                    using var resourceStream = assembly.GetManifestResourceStream(song.FilePath);
                    if (resourceStream != null)
                    {
                        // Create temporary file for NAudio
                        string tempFile = Path.GetTempFileName() + ".mp3";
                        using (var fileStream = File.Create(tempFile))
                        {
                            resourceStream.CopyTo(fileStream);
                        }
                        audioFile = new AudioFileReader(tempFile);
                        audioFile.Volume = DEFAULT_VOLUME;
                    }
                }
                else if (!string.IsNullOrEmpty(song.FilePath) && File.Exists(song.FilePath))
                {
                    // Load from file system (for user-added songs)
                    audioFile = new AudioFileReader(song.FilePath);
                    audioFile.Volume = DEFAULT_VOLUME;
                }
                else if (song.DisplayName.Contains("Generated"))
                {
                    CreateFallbackAudio();
                }
                // For "No songs found" or "Error loading songs", audioFile remains null
                
                UpdateSongDisplay();
            }
            catch (Exception ex)
            {
                LogError($"Failed to load song at index {index}", ex);
                currentSongName = "Error loading song";
                UpdateSongDisplay();
            }
        }

        private void UpdateSongMenu(ToolStripMenuItem songMenu)
        {
            try
            {
                songMenu.DropDownItems.Clear();
                for (int i = 0; i < songs.Count; i++)
                {
                    var idx = i;
                    var item = new ToolStripMenuItem(songs[i].DisplayName)
                    {
                        Checked = (i == currentSongIndex),
                        CheckOnClick = false,
                        ForeColor = Color.White,
                        BackColor = Color.FromArgb(45, 45, 48),
                        Enabled = !songs[i].DisplayName.Contains("No songs found") && !songs[i].DisplayName.Contains("Error")
                    };
                    item.Click += (s, e) =>
                    {
                        if (songs[idx].DisplayName.Contains("No songs found") || songs[idx].DisplayName.Contains("Error"))
                            return;
                        currentSongIndex = idx;
                        LoadSong(currentSongIndex);
                        UpdateSongMenu(songMenu);
                    };
                    songMenu.DropDownItems.Add(item);
                }
                songMenu.DropDownItems.Add(new ToolStripSeparator());
                var addSongItem = new ToolStripMenuItem("➕ Add Song...")
                {
                    ForeColor = Color.White,
                    BackColor = Color.FromArgb(45, 45, 48)
                };
                addSongItem.Click += (s, e) => AddSongFromDialog(songMenu);
                songMenu.DropDownItems.Add(addSongItem);
            }
            catch (Exception ex)
            {
                LogError("Failed to update song menu", ex);
            }
        }

        private void AddSongFromDialog(ToolStripMenuItem songMenu)
        {
            try
            {
                using (var ofd = new OpenFileDialog())
                {
                    ofd.Filter = "MP3 Files (*.mp3)|*.mp3";
                    ofd.Title = "Add a Song";
                    ofd.InitialDirectory = Environment.GetFolderPath(Environment.SpecialFolder.MyMusic);
                    
                    if (ofd.ShowDialog() == DialogResult.OK)
                    {
                        string fileName = Path.GetFileName(ofd.FileName);
                        string dest = Path.Combine(RESOURCES_FOLDER, fileName);
                        
                        // Check if file already exists
                        if (File.Exists(dest))
                        {
                            var result = MessageBox.Show($"A file named '{fileName}' already exists. Do you want to replace it?", 
                                APP_NAME, MessageBoxButtons.YesNo, MessageBoxIcon.Question);
                            if (result != DialogResult.Yes)
                                return;
                        }
                        
                        File.Copy(ofd.FileName, dest, true);
                        string displayName = Path.GetFileNameWithoutExtension(dest);
                        if (displayName.Length > MAX_SONG_NAME_LENGTH)
                        {
                            displayName = displayName.Substring(0, MAX_SONG_NAME_LENGTH - 3) + "...";
                        }
                        
                        songs.Add((displayName, dest, false));
                        UpdateSongMenu(songMenu);
                        
                        trayIcon?.ShowBalloonTip(BALLOON_TIP_DURATION, APP_NAME, $"Added: {displayName}", ToolTipIcon.Info);
                    }
                }
            }
            catch (Exception ex)
            {
                LogError("Failed to add song from dialog", ex);
                MessageBox.Show($"Failed to add song: {ex.Message}", APP_NAME, MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void UpdateSongDisplay()
        {
            try
            {
                var songMenu = trayMenu?.Items.Find("songMenu", false).FirstOrDefault() as ToolStripMenuItem;
                if (songMenu != null)
                {
                    songMenu.Text = $"🎶 Song: {currentSongName}";
                    UpdateSongMenu(songMenu);
                }
            }
            catch (Exception ex)
            {
                LogError("Failed to update song display", ex);
            }
        }

        private void PlayPause_Click(object? sender, EventArgs e)
        {
            if (isPlaying)
            {
                Pause();
            }
            else
            {
                Play();
            }
        }

        private void Play()
        {
            if (audioFile == null)
            {
                trayIcon?.ShowBalloonTip(BALLOON_TIP_DURATION, APP_NAME, "No song loaded to play", ToolTipIcon.Warning);
                return;
            }

            try
            {
                if (waveOut == null)
                {
                    waveOut = new WaveOutEvent();
                    waveOut.PlaybackStopped += WaveOut_PlaybackStopped;
                }

                if (isPausedByDevice)
                {
                    isPausedByDevice = false;
                }

                audioFile.Position = 0; // Start from beginning
                waveOut.Init(audioFile);
                waveOut.Play();
                isPlaying = true;

                UpdateStatus("Playing", currentDeviceName);
                UpdatePlayPauseButton("Pause");
            }
            catch (Exception ex)
            {
                LogError("Failed to play audio", ex);
                MessageBox.Show($"Error playing audio: {ex.Message}", APP_NAME, MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void Pause()
        {
            try
            {
                if (waveOut != null)
                {
                    waveOut.Pause();
                    isPlaying = false;
                    UpdateStatus("Paused", currentDeviceName);
                    UpdatePlayPauseButton("Play");
                }
            }
            catch (Exception ex)
            {
                LogError("Failed to pause audio", ex);
            }
        }

        private void Stop_Click(object? sender, EventArgs e)
        {
            Stop();
        }

        private void Stop()
        {
            try
            {
                if (waveOut != null)
                {
                    waveOut.Stop();
                    isPlaying = false;
                    isPausedByDevice = false;
                    UpdateStatus("Stopped", currentDeviceName);
                    UpdatePlayPauseButton("Play");
                }
            }
            catch (Exception ex)
            {
                LogError("Failed to stop audio", ex);
            }
        }

        private void WaveOut_PlaybackStopped(object? sender, StoppedEventArgs e)
        {
            try
            {
                if (e.Exception != null)
                {
                    // Device disconnected or error occurred
                    LogError("Playback stopped due to error", e.Exception);
                    isPausedByDevice = true;
                    isPlaying = false;
                    UpdateStatus("Paused: No Device", "Device Disconnected");
                    UpdatePlayPauseButton("Play");
                    trayIcon?.ShowBalloonTip(BALLOON_TIP_DURATION, APP_NAME, "Audio disconnected – paused", ToolTipIcon.Info);
                }
                else if (isPlaying && !isPausedByDevice)
                {
                    // Song ended, restart
                    Play();
                }
            }
            catch (Exception ex)
            {
                LogError("Error in playback stopped handler", ex);
            }
        }

        private void VolumeSlider_ValueChanged(object? sender, EventArgs e)
        {
            try
            {
                if (sender is TrackBar slider && audioFile != null)
                {
                    var volume = slider.Value / 100f;
                    audioFile.Volume = volume;
                    
                    var volumeLabel = trayMenu?.Items.Find("volumeLabel", false).FirstOrDefault() as ToolStripLabel;
                    if (volumeLabel != null)
                    {
                        string volumeIcon = slider.Value switch
                        {
                            0 => "🔇",
                            <= 25 => "🔈",
                            <= 50 => "🔉",
                            <= 75 => "🔊",
                            _ => "🔊"
                        };
                        volumeLabel.Text = $"{volumeIcon} {slider.Value}%";
                    }
                }
            }
            catch (Exception ex)
            {
                LogError("Failed to change volume", ex);
            }
        }



        private void UpdateStatus(string status, string device = "")
        {
            try
            {
                var statusLabel = trayMenu?.Items.Find("statusLabel", false).FirstOrDefault() as ToolStripLabel;
                if (statusLabel != null)
                {
                    string icon = status switch
                    {
                        "Playing" => "▶️",
                        "Paused" => "⏸️",
                        "Stopped" => "⏹️",
                        _ => "⏸️"
                    };
                    
                    if (!string.IsNullOrEmpty(device))
                        statusLabel.Text = $"{icon} Status: {status} ({device})";
                    else
                        statusLabel.Text = $"{icon} Status: {status}";
                }
            }
            catch (Exception ex)
            {
                LogError("Failed to update status", ex);
            }
        }

        private void UpdatePlayPauseButton(string text)
        {
            try
            {
                var playButton = trayMenu?.Items.Find("playPauseButton", false).FirstOrDefault() as ToolStripMenuItem;
                if (playButton != null)
                {
                    string icon = text switch
                    {
                        "Play" => "▶️",
                        "Pause" => "⏸️",
                        _ => "▶️"
                    };
                    playButton.Text = $"{icon} {text}";
                }
            }
            catch (Exception ex)
            {
                LogError("Failed to update play/pause button", ex);
            }
        }

        private void CreateFallbackAudio()
        {
            try
            {
                // Create a simple ambient sine wave as fallback
                var sampleRate = 44100;
                var duration = 10; // 10 seconds
                var frequency = 220; // A3 note - calm frequency
                var samples = new float[sampleRate * duration];
                
                for (int i = 0; i < samples.Length; i++)
                {
                    samples[i] = (float)(Math.Sin(2 * Math.PI * frequency * i / sampleRate) * 0.05); // Very low volume
                }

                var sampleProvider = new SampleProvider(samples);
                
                // Create a temporary WAV file
                tempAudioFile = Path.GetTempFileName() + ".wav";
                using (var waveWriter = new WaveFileWriter(tempAudioFile, sampleProvider.WaveFormat))
                {
                    var buffer = new float[1024];
                    int bytesRead;
                    while ((bytesRead = sampleProvider.Read(buffer, 0, buffer.Length)) > 0)
                    {
                        waveWriter.WriteSamples(buffer, 0, bytesRead);
                    }
                }
                
                audioFile = new AudioFileReader(tempAudioFile);
                audioFile.Volume = DEFAULT_VOLUME;
            }
            catch (Exception ex)
            {
                LogError("Failed to create fallback audio", ex);
                throw;
            }
        }

        private Icon CreateMusicIcon()
        {
            try
            {
                // Use the application's own icon for the tray
                return Icon.ExtractAssociatedIcon(Application.ExecutablePath);
            }
            catch (Exception ex)
            {
                LogError("Failed to create music icon", ex);
                return SystemIcons.Application;
            }
        }

        private void TrayIcon_DoubleClick(object? sender, EventArgs e)
        {
            try
            {
                // Show/hide the form (minimal functionality)
                this.WindowState = this.WindowState == FormWindowState.Minimized ? FormWindowState.Normal : FormWindowState.Minimized;
            }
            catch (Exception ex)
            {
                LogError("Failed to handle tray icon double click", ex);
            }
        }

        private void Exit_Click(object? sender, EventArgs e)
        {
            try
            {
                Application.Exit();
            }
            catch (Exception ex)
            {
                LogError("Failed to exit application", ex);
                Environment.Exit(1);
            }
        }

        protected override void OnFormClosing(FormClosingEventArgs e)
        {
            try
            {
                if (e.CloseReason == CloseReason.UserClosing)
                {
                    e.Cancel = true;
                    this.WindowState = FormWindowState.Minimized;
                    this.ShowInTaskbar = false;
                }
                else
                {
                    Cleanup();
                }
                base.OnFormClosing(e);
            }
            catch (Exception ex)
            {
                LogError("Error in form closing", ex);
            }
        }

        private void Cleanup()
        {
            try
            {
                isDisposed = true;
                
                // Stop playback
                if (waveOut != null)
                {
                    waveOut.Stop();
                    waveOut.Dispose();
                    waveOut = null;
                }
                
                // Dispose audio file
                if (audioFile != null)
                {
                    audioFile.Dispose();
                    audioFile = null;
                }
                
                // Clean up temporary file
                if (!string.IsNullOrEmpty(tempAudioFile) && File.Exists(tempAudioFile))
                {
                    try
                    {
                        File.Delete(tempAudioFile);
                    }
                    catch (Exception ex)
                    {
                        LogError("Failed to delete temporary audio file", ex);
                    }
                }
                
                // Unregister device monitoring
                if (deviceEnumerator != null && deviceChangeHandler != null)
                {
                    try
                    {
                        deviceEnumerator.UnregisterEndpointNotificationCallback(deviceChangeHandler);
                    }
                    catch (Exception ex)
                    {
                        LogError("Failed to unregister device notification callback", ex);
                    }
                }
                
                // Dispose tray icon
                if (trayIcon != null)
                {
                    trayIcon.Dispose();
                    trayIcon = null;
                }
            }
            catch (Exception ex)
            {
                LogError("Error during cleanup", ex);
            }
        }

        private void LogError(string message, Exception? ex = null)
        {
            try
            {
                string logMessage = $"[{DateTime.Now:yyyy-MM-dd HH:mm:ss}] {message}";
                if (ex != null)
                {
                    logMessage += $"\nException: {ex.Message}\nStackTrace: {ex.StackTrace}";
                }
                
                // Write to debug output
                Debug.WriteLine(logMessage);
                
                // Write to log file
                string logFile = Path.Combine(Application.StartupPath, "background_tunes.log");
                File.AppendAllText(logFile, logMessage + Environment.NewLine);
            }
            catch
            {
                // If logging fails, don't throw - just continue
            }
        }

        // Device change handler
        public void OnDeviceStateChanged(string deviceId, DeviceState newState)
        {
            if (isDisposed) return;
            
            if (InvokeRequired)
            {
                try
                {
                    Invoke(new Action(() => OnDeviceStateChanged(deviceId, newState)));
                }
                catch (ObjectDisposedException)
                {
                    // Form is disposed, ignore
                }
                return;
            }

            try
            {
                CheckAudioDeviceStatus();
                
                if (newState == DeviceState.Active && isPausedByDevice)
                {
                    // Device reconnected but don't auto-resume
                    isPausedByDevice = false;
                    UpdateStatus("Paused", currentDeviceName);
                }
            }
            catch (Exception ex)
            {
                LogError("Error in device state changed handler", ex);
            }
        }

        public void OnDeviceAdded(string pwstrDeviceId)
        {
            if (isDisposed) return;
            
            if (InvokeRequired)
            {
                try
                {
                    Invoke(new Action(() => OnDeviceAdded(pwstrDeviceId)));
                }
                catch (ObjectDisposedException)
                {
                    // Form is disposed, ignore
                }
                return;
            }

            try
            {
                CheckAudioDeviceStatus();
            }
            catch (Exception ex)
            {
                LogError("Error in device added handler", ex);
            }
        }

        public void OnDeviceRemoved(string pwstrDeviceId)
        {
            if (isDisposed) return;
            
            if (InvokeRequired)
            {
                try
                {
                    Invoke(new Action(() => OnDeviceRemoved(pwstrDeviceId)));
                }
                catch (ObjectDisposedException)
                {
                    // Form is disposed, ignore
                }
                return;
            }

            try
            {
                if (isPlaying)
                {
                    Pause();
                    isPausedByDevice = true;
                    trayIcon?.ShowBalloonTip(BALLOON_TIP_DURATION, APP_NAME, "Audio device disconnected – paused", ToolTipIcon.Info);
                }

                CheckAudioDeviceStatus();
            }
            catch (Exception ex)
            {
                LogError("Error in device removed handler", ex);
            }
        }

        public void OnDefaultDeviceChanged(DataFlow flow, Role role, string defaultDeviceId)
        {
            if (isDisposed) return;
            
            if (InvokeRequired)
            {
                try
                {
                    Invoke(new Action(() => OnDefaultDeviceChanged(flow, role, defaultDeviceId)));
                }
                catch (ObjectDisposedException)
                {
                    // Form is disposed, ignore
                }
                return;
            }

            try
            {
                if (flow == DataFlow.Render && role == Role.Multimedia)
                {
                    if (isPlaying)
                    {
                        Pause();
                        isPausedByDevice = true;
                        trayIcon?.ShowBalloonTip(BALLOON_TIP_DURATION, APP_NAME, "Audio output changed – paused", ToolTipIcon.Info);
                    }

                    CheckAudioDeviceStatus();
                }
            }
            catch (Exception ex)
            {
                LogError("Error in default device changed handler", ex);
            }
        }
    }

    // Device change handler class
    public class DeviceChangeHandler : IMMNotificationClient
    {
        private readonly TrayMusicPlayer player;

        public DeviceChangeHandler(TrayMusicPlayer player)
        {
            this.player = player;
        }

        public void OnDeviceStateChanged(string deviceId, DeviceState newState)
        {
            player.OnDeviceStateChanged(deviceId, newState);
        }

        public void OnDeviceAdded(string pwstrDeviceId)
        {
            player.OnDeviceAdded(pwstrDeviceId);
        }

        public void OnDeviceRemoved(string pwstrDeviceId)
        {
            player.OnDeviceRemoved(pwstrDeviceId);
        }

        public void OnDefaultDeviceChanged(DataFlow flow, Role role, string defaultDeviceId)
        {
            player.OnDefaultDeviceChanged(flow, role, defaultDeviceId);
        }

        public void OnPropertyValueChanged(string pwstrDeviceId, PropertyKey key)
        {
            // Not used in this implementation
        }
    }

    // Simple sample provider for fallback audio
    public class SampleProvider : ISampleProvider
    {
        private readonly float[] samples;
        private int position = 0;

        public SampleProvider(float[] samples)
        {
            this.samples = samples;
        }

        public WaveFormat WaveFormat => new WaveFormat(44100, 16, 1);

        public int Read(float[] buffer, int offset, int count)
        {
            var samplesToCopy = Math.Min(count, samples.Length - position);
            if (samplesToCopy <= 0) return 0;

            Array.Copy(samples, position, buffer, offset, samplesToCopy);
            position += samplesToCopy;
            return samplesToCopy;
        }
    }

    // Custom menu renderer to remove white strip and change hover color
    public class CustomMenuRenderer : ToolStripProfessionalRenderer
    {
        public CustomMenuRenderer() : base(new CustomColorTable())
        {
        }

        protected override void OnRenderItemBackground(ToolStripItemRenderEventArgs e)
        {
            // Always fill with dark background first
            using (var brush = new SolidBrush(Color.FromArgb(45, 45, 48)))
            {
                e.Graphics.FillRectangle(brush, e.Item.Bounds);
            }

            // Then add hover effect if selected
            if (e.Item.Selected)
            {
                // Gray hover color instead of blue
                using (var brush = new SolidBrush(Color.FromArgb(70, 70, 70)))
                {
                    e.Graphics.FillRectangle(brush, e.Item.Bounds);
                }
            }
        }

        protected override void OnRenderToolStripBackground(ToolStripRenderEventArgs e)
        {
            // Fill the entire menu background, including the left edge
            using (var brush = new SolidBrush(Color.FromArgb(45, 45, 48)))
            {
                e.Graphics.FillRectangle(brush, e.AffectedBounds);
            }
        }

        protected override void OnRenderSeparator(ToolStripSeparatorRenderEventArgs e)
        {
            // Remove the white strip by making separators transparent
            using (var pen = new Pen(Color.FromArgb(60, 60, 60)))
            {
                e.Graphics.DrawLine(pen, e.Item.Bounds.Left + 10, e.Item.Bounds.Top + e.Item.Bounds.Height / 2,
                                   e.Item.Bounds.Right - 10, e.Item.Bounds.Top + e.Item.Bounds.Height / 2);
            }
        }

        protected override void OnRenderToolStripBorder(ToolStripRenderEventArgs e)
        {
            // Remove the white border by not drawing it
            // base.OnRenderToolStripBorder(e); // Commented out to remove border
        }

        protected override void OnRenderImageMargin(ToolStripRenderEventArgs e)
        {
            // Fill the image margin (the left strip) with the same background color
            using (var brush = new SolidBrush(Color.FromArgb(45, 45, 48)))
            {
                e.Graphics.FillRectangle(brush, e.AffectedBounds);
            }
        }
    }

    public class CustomColorTable : ProfessionalColorTable
    {
        public override Color MenuItemSelected => Color.FromArgb(70, 70, 70);
        public override Color MenuItemSelectedGradientBegin => Color.FromArgb(70, 70, 70);
        public override Color MenuItemSelectedGradientEnd => Color.FromArgb(70, 70, 70);
        public override Color MenuItemPressedGradientBegin => Color.FromArgb(70, 70, 70);
        public override Color MenuItemPressedGradientEnd => Color.FromArgb(70, 70, 70);
        public override Color SeparatorDark => Color.FromArgb(60, 60, 60);
        public override Color SeparatorLight => Color.FromArgb(60, 60, 60);
        public override Color MenuBorder => Color.FromArgb(60, 60, 60);
        public override Color MenuItemBorder => Color.FromArgb(60, 60, 60);
    }

    // Custom TrackBar that doesn't steal focus from the menu
    public class CustomTrackBar : TrackBar
    {
        public CustomTrackBar()
        {
            this.TabStop = false;
            this.SetStyle(ControlStyles.Selectable, false);
        }

        protected override bool IsInputKey(Keys keyData)
        {
            // Prevent keyboard navigation from stealing focus
            return false;
        }

        protected override void OnGotFocus(EventArgs e)
        {
            // Immediately lose focus to prevent focus stealing
            this.Parent?.Focus();
        }

        protected override void OnMouseDown(MouseEventArgs e)
        {
            // Handle mouse down without gaining focus
            base.OnMouseDown(e);
            this.Parent?.Focus();
        }

        protected override void OnMouseUp(MouseEventArgs e)
        {
            // Handle mouse up without gaining focus
            base.OnMouseUp(e);
            this.Parent?.Focus();
        }
    }
} 