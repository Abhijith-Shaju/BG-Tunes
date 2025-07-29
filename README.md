# BG-Tunes 🎵

A lightweight, modern system tray music player for Windows with built-in device monitoring and two beautiful songs included.

---

## 🚀 For Users

**Just want to use BG-Tunes?**

1. **[Download the latest release here.](https://github.com/Abhijith-Shaju/BG-Tunes/releases/latest)**
2. Unzip and run `BG-Tunes.exe`
3. The app will appear in your system tray. Right-click the icon for controls.

**No installation or .NET required!**
- Works on Windows 10/11 (64-bit)
- Two songs are included in the app

### Optional: Add to Windows Search
- Run `install-to-start-menu.bat` (included) to add BG-Tunes to your Start Menu for easy search.

---

## ✨ Features

- 🎵 **Two embedded songs** (ready to play)
- 🎧 **Auto-pause** when headphones or audio device disconnects
- 🔊 **Safe 25% volume start**
- 💻 **Modern dark tray menu** with emoji icons
- ➕ **Add your own songs** via the menu
- 🛡️ **No auto-resume** (user must click play)
- 🖱️ **Compact, beautiful UI**

---

## 🛠️ For Developers

Want to build or contribute?

1. **Clone the repo:**
   ```sh
   git clone https://github.com/Abhijith-Shaju/BG-Tunes.git
   cd BG-Tunes
   ```

2. **Open in Visual Studio or VS Code**

3. **Build and run:**
   ```sh
   dotnet build
   dotnet run
   ```

- Songs in `Resources/*.mp3` are embedded at build time.
- See `INSTALLATION.md` for more details.

---

## 📦 File Structure

```
BG-Tunes/
├── Program.cs
├── TrayMusicPlayer.cs
├── Resources/           # Embedded MP3s
├── install-to-start-menu.bat
├── README.md
├── INSTALLATION.md
└── .gitignore
```

---

## 📄 License

This project is open source, licensed under the MIT License.  
See [LICENSE](LICENSE) for details.

---

**Enjoy your music!**  
If you like BG-Tunes, please star the repo or share it with friends! 