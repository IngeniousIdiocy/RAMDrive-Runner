# RAMDrive-Runner

RAMDrive-Runner is a small WPF utility that copies a selected game folder to a temporary RAM disk and creates a junction so the game loads directly from memory.  It was written as a quick tool for personal use.

## How it works

1. The application enumerates the subfolders of a chosen source directory (by default Steam's `common` folder) and displays their sizes.
2. When a folder is selected the RAM allocation slider is automatically set to a recommended size based on the folder size.
3. Clicking **mount** performs the following tasks:
   - Creates a RAM disk on drive **Z:** using [ImDisk](https://sourceforge.net/projects/imdisk-toolkit/).
   - Formats the disk as NTFS.
   - Copies the selected folder to the RAM disk.
   - Renames the original folder to `<name>_RAMDisk` and creates a junction pointing to the RAM disk.
4. The **unmount** button reverses the process by removing the junction, restoring the original folder and unmounting the RAM disk.

Any PowerShell errors during these operations are written to `RAMRunnerErrorLog.txt` in the application directory.

## Requirements

- Windows with the [.NET 6.0](https://dotnet.microsoft.com/download) runtime
- [ImDisk](https://sourceforge.net/projects/imdisk-toolkit/) installed and available on the `PATH`

## Building

Open `RAMDrive Runner.sln` in Visual Studio 2022 or run:

```bash
dotnet build "RAMDrive Runner/RAMDrive Runner.csproj"
```

## Usage

1. Launch **RAMDrive-Runner**.
2. Click the source directory text box to browse to your game library.
3. Select the folder you want to load into RAM.
4. Adjust the RAM allocation slider if necessary (leave at least 8&nbsp;GB free for Windows).
5. Press the **mount** button. A progress overlay will appear while files are copied.
6. When finished, start your game normally. The data will be served from the RAM disk.
7. After playing, press **unmount** to restore the original folder and free the RAM.

## Screenshots

Below are the three main states of the application.

| Unloaded | Loading | Loaded |
| --- | --- | --- |
| ![Unloaded](ramdrive%20runner%20unloaded%20state.png) | ![Loading](ramdrive%20runner%20loading%20state.png) | ![Loaded](ramdrive%20runner%20loaded%20state.png) |

I have never written WPF before and just used GPT-4 to tell me what to code.

