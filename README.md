# 🎮 Afterlife Mods Installer — Schedule 1

**Afterlife Mods Installer** is a lightweight, user-friendly mod management tool designed for the **Schedule 1 mod framework**. It allows users to **download mods**, **update configuration files**, and **customize the setup** to their own preferences — all through a clean, integrated interface.

---

## 🚀 Features

- 📦 **One-click Mod Installation**  
  Automatically downloads mods and places them in the correct directory.

- 🧠 **Smart Config Updating**  
  Appends new asset entries to `AfterlifeAssetBundles.txt` only if they're not already present.

- 🔍 **Asset Validation**  
  Verifies that referenced assets exist before updating config files.

- ⚙️ **Custom Installation Path**  
  Users can specify their own installation location for maximum flexibility.

- 🎨 **Modern UI with WebView2**  
  Sleek, embedded browser interface powered by HTML, CSS, and JavaScript.

---

## 📂 File Structure

```
AfterlifeModsInstaller/
│
├── Assets/                  # Local mod files (images, bundles, etc.)
├── Scripts/                 # JavaScript logic (UI actions, events)
├── AfterlifeAssetBundles.txt  # Auto-managed asset registry
├── Installer.exe            # The main executable
└── README.md                # This file
```

---

## 🛠️ How It Works

### 1. **Install an Asset**

In the input box, type:

```
install asset <assetName>
```

This will:
- Search for any file with `<assetName>` in the installation path.
- Move it to the proper `Mods/AssetBundles/` directory.
- Append its name to `AfterlifeAssetBundles.txt` if it isn't already listed.

> If the asset file cannot be found, the installer will prompt you to move it manually.

---

### 2. **Update Configuration**

Assets are added to the config file without duplication, and the tool ensures each entry ends on its own line.

---

## ⚡ Setup & Usage

1. Clone or download the repository.
2. Run `Installer.exe`.
3. Set your installation path when prompted.
4. Use the input box to install assets or update your config.

---

## 🧩 Requirements

- ✅ .NET 6.0 or later  
- ✅ Windows 10+  
- ✅ WebView2 Runtime (for UI)

---

## 🗨️ Example Use Case

```
install asset femalec
```

- Searches for `femalec.*`
- If found: moves it to `Mods/AssetBundles/`
- Then updates `AfterlifeAssetBundles.txt` with `femalec`

---

## 🧪 Coming Soon

- 🔄 Auto-update mod definitions from GitHub
- 🌐 Community mod sharing
- 🧰 Backup/restore configuration

---

## 📫 Contributing / Feedback

Pull requests and feedback are welcome! Please open an issue for bugs or suggestions.

---

## 📜 License

MIT License — do whatever you want, just don’t claim it as your own without credit.

---
**Made with ☠️ for Schedule 1**
