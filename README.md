# DVD Extras Renamer

A cross-platform desktop application built with Avalonia that automatically matches and renames video files in your DVD extras directories based on metadata from [dvdcompare.net](https://www.dvdcompare.net).

## Features

- **DVD Lookup**: Search dvdcompare.net for DVD titles, years, and directors
- **Automatic Video Matching**: Match video files by duration (±1 second tolerance) to DVD extras
- **Smart Title Formatting**: Automatically formats titles like "The Northman" to "Northman (The)" for accurate searches
- **Intelligent Title Cleanup**: Removes descriptors (documentary, featurette), quotes, and invalid filesystem characters
- **Batch Renaming**: Rename matched videos with a single click per file
- **Real-time Progress**: Watch matching and renaming operations with live feedback
- **Cross-Platform**: Runs on Windows, macOS, and Linux

## Supported Video Formats

- MP4 (.mp4)
- Matroska (.mkv)
- AVI (.avi)
- MOV (.mov)
- Flash Video (.flv)
- Windows Media (.wmv)
- WebM (.webm)

## Prerequisites

- **.NET 10 SDK** or later (download from [dotnet.microsoft.com](https://dotnet.microsoft.com))
- **macOS 10.13+**, **Windows 10+**, or **Linux** with appropriate runtime support

## Installation

### From Source

1. Clone the repository:
   ```bash
   git clone https://github.com/TannerLivingston/DvdExtrasRenamer.git
   cd DvdExtrasRenamer
   ```

2. Build the project:
   ```bash
   dotnet build
   ```

3. Run the application:
   ```bash
   dotnet run --project DvdExtrasRenamer/DvdExtrasRenamer.csproj
   ```

### Pre-built Binaries

Download the latest release for your platform from the [Releases](https://github.com/TannerLivingston/DvdExtrasRenamer/releases) page.

## Usage

1. **Select a Directory**: Click "Browse Directory" and navigate to your DVD extras folder
2. **Search for DVD**: 
   - Enter the DVD title (e.g., "The Northman")
   - Optionally add year and/or director name
   - Click "Lookup DVD"
3. **Select DVD**: Click the desired DVD from the results
4. **Match Videos**: Click "Match Videos in Directory" to scan for matches
5. **Review Matches**: The matched videos appear in the results section
6. **Rename Videos**: Click each "Rename" button to rename files with their extra titles

## How It Works

### Matching Algorithm

The application matches video files to DVD extras using:
- **Duration matching**: Compares video file duration to extra duration with ±1 second tolerance
- **Deduplication**: Eliminates duplicate matches to ensure one-to-one mappings
- **Parallel processing**: Non-blocking UI with Task.Run for smooth user experience

### Title Cleanup

Extra titles are intelligently cleaned to create valid filenames:
- Removes descriptors: "documentary", "featurette", "behind the scenes", etc.
- Strips quote marks (including Unicode variants)
- Removes invalid filesystem characters: `< > : " / \ ? * |`
- Normalizes whitespace

### Article Handling

Titles starting with articles are automatically reformatted for dvdcompare.net searches:
- "The Northman" → "Northman (The)"
- "A Beautiful Mind" → "Beautiful Mind (A)"
- "An American Tale" → "American Tale (An)"

## Architecture

### Project Structure

```
DvdExtrasRenamer/
├── Models/              # Data models and business logic
│   ├── DvdCompareResult.cs
│   ├── DvdDotComClient.cs      # HTTP client for dvdcompare.net
│   ├── DvdDotComParser.cs      # HTML parsing and title cleanup
│   └── FileHandler.cs          # Video file operations
├── ViewModels/          # MVVM view models
│   ├── MainWindowViewModel.cs
│   └── ViewModelBase.cs
├── Views/              # XAML UI definitions
│   ├── MainWindow.axaml
│   └── MainWindow.axaml.cs
└── Assets/             # Application resources
```

### MVVM Pattern

The application uses the MVVM (Model-View-ViewModel) pattern with:
- **CommunityToolkit.MVVM**: For ObservableProperty and RelayCommand
- **Avalonia**: Cross-platform UI framework with XAML support
- **Separation of Concerns**: UI logic in ViewModels, file operations in Models

### Dependencies

- **Avalonia 11.3.11**: UI Framework
- **CommunityToolkit.MVVM 8.2.1**: MVVM Toolkit
- **HtmlAgilityPack 1.12.4**: HTML parsing for dvdcompare.net
- **TagLibSharp 2.2.0**: Metadata extraction from video files

## Configuration

The application reads from dvdcompare.net dynamically with no additional configuration required. All settings are managed through the UI.

## Troubleshooting

### Lookup Returns No Results

- Check title spelling against [dvdcompare.net](https://www.dvdcompare.net)
- Try removing year/director filters
- Some regional editions may have different titles

### No Videos Matched

- Verify video files are in the selected directory
- Check file extensions are supported (.mp4, .mkv, .avi, .mov, .flv, .wmv, .webm)
- Video duration must match DVD extra duration within ±1 second
- Some videos may have incorrect metadata; check file properties

### File Rename Fails

- Ensure you have write permissions to the directory
- Close any applications that may be using the video files
- Check disk space is available

## Contributing

Contributions are welcome! Please follow these guidelines:

1. **Fork the repository** on GitHub
2. **Create a feature branch**: `git checkout -b feature/your-feature-name`
3. **Make your changes** and commit with clear messages:
   - Reference issues: `Fix #123`
   - Describe what changed and why
4. **Push to your fork**: `git push origin feature/your-feature-name`
5. **Open a Pull Request** with a description of your changes

### Development Setup

1. Clone your fork
2. Open in Visual Studio, VS Code, or Rider
3. Build and run: `dotnet run --project DvdExtrasRenamer`
4. Run tests (if applicable): `dotnet test`

### Code Style

- Follow C# naming conventions (PascalCase for public members, camelCase for private)
- Use meaningful variable names
- Add XML documentation comments to public methods
- Format code consistently (use .editorconfig)

## License

This project is licensed under the MIT License - see the [LICENSE](LICENSE) file for details.

## Acknowledgments

- [dvdcompare.net](https://www.dvdcompare.net) - DVD comparison and extras database
- [Avalonia UI](https://avaloniaui.net/) - Cross-platform UI framework
- [TagLibSharp](https://github.com/mono/taglib-sharp) - Media metadata library
- [HtmlAgilityPack](https://html-agility-pack.net/) - HTML parsing library

## Support

For issues, questions, or feature requests, please open an [issue](https://github.com/TannerLivingston/DvdExtrasRenamer/issues) on GitHub.

---

**Note**: This application is not affiliated with dvdcompare.net and uses their public website for information lookup only.
