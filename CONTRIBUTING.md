# Contributing to DVD Extras Renamer

Thank you for your interest in contributing to DVD Extras Renamer! This document provides guidelines and instructions for contributing to the project.

## Code of Conduct

This project adheres to the Contributor Covenant Code of Conduct. By participating, you are expected to uphold this code. Please report unacceptable behavior to the project maintainers.

## How to Contribute

### Reporting Bugs

Before submitting a bug report, please check the [issue list](https://github.com/TannerLivingston/DvdExtrasRenamer/issues) to avoid duplicates.

When filing a bug report, include:
- **OS and Version**: Windows 10, macOS 12, Ubuntu 22.04, etc.
- **Application Version**: Version number or "latest"
- **Steps to Reproduce**: Clear steps to trigger the bug
- **Expected vs. Actual Behavior**: What should happen vs. what actually happens
- **Screenshots**: If visually related
- **Console Output**: Any error messages or logs

### Suggesting Features

Open an issue with the label `enhancement`. Describe:
- **The Problem**: What pain point does this solve?
- **The Solution**: How should it work?
- **Use Case**: Why is this useful?
- **Alternatives**: Have you considered other approaches?

### Submitting Pull Requests

1. **Fork the repository** on GitHub
2. **Clone your fork**:
   ```bash
   git clone https://github.com/TannerLivingston/DvdExtrasRenamer.git
   cd DvdExtrasRenamer
   ```
3. **Create a feature branch**:
   ```bash
   git checkout -b feature/your-feature-name
   ```
4. **Make your changes**:
   - Follow the code style (see below)
   - Add/update XML documentation for public methods
   - Test your changes thoroughly
5. **Commit with clear messages**:
   ```bash
   git commit -m "Add feature X: Brief description
   
   More detailed explanation of what changed and why."
   ```
6. **Push to your fork**:
   ```bash
   git push origin feature/your-feature-name
   ```
7. **Open a Pull Request** on GitHub with:
   - Clear title and description
   - Reference to any related issues: "Fixes #123"
   - List of changes made
   - Testing performed

## Development Setup

### Prerequisites

- **.NET 10 SDK** ([download](https://dotnet.microsoft.com/download/dotnet/10.0))
- **Git**
- Text editor or IDE (Visual Studio, VS Code, or Rider recommended)

### Building

```bash
# Clone and navigate to repo
git clone https://github.com/TannerLivingston/DvdExtrasRenamer.git
cd DvdExtrasRenamer

# Restore dependencies
dotnet restore

# Build
dotnet build

# Run
dotnet run --project DvdExtrasRenamer/DvdExtrasRenamer.csproj
```

### Project Structure

```
DvdExtrasRenamer/
├── Models/              # Business logic & data models
│   ├── DvdDotComClient.cs      # HTML fetching
│   ├── DvdDotComParser.cs      # HTML parsing & title cleanup
│   ├── FileHandler.cs          # Video file operations
│   └── DvdCompareResult.cs     # Data records
├── ViewModels/          # MVVM view models
│   ├── MainWindowViewModel.cs  # Main business logic
│   └── ViewModelBase.cs        # Base class
├── Views/              # XAML UI definitions
│   ├── MainWindow.axaml
│   └── MainWindow.axaml.cs
├── Assets/             # Resources
└── App.axaml*          # Application configuration
```

## Code Style Guidelines

### C# Conventions

- **Naming**: Follow C# naming conventions
  - Classes, methods, properties: `PascalCase`
  - Private fields, local variables: `_camelCase` or `camelCase`
  - Constants: `UPPER_CASE` (or `PascalCase` if common in codebase)
- **Spacing**: Use 4 spaces for indentation (configured in .editorconfig)
- **Line Length**: Keep lines under 120 characters when reasonable
- **Comments**: Use `//` for inline comments, `///` for XML documentation

### XML Documentation

All public classes, methods, and properties should have XML documentation comments:

```csharp
/// <summary>
/// Brief one-line description.
/// </summary>
/// <param name="paramName">Description of parameter</param>
/// <returns>Description of return value</returns>
public async Task<string> ExampleMethodAsync(string paramName)
{
    // Implementation
}
```

### Async/Await

- Use `async`/`await` for I/O operations (HTTP, file access)
- Method names ending in operations should end with `Async`
- Avoid `.Result` or `.Wait()` - use `await` instead
- Use `Task.Run()` for long-running CPU-bound operations on the UI thread

### Error Handling

- Include try-catch blocks for operations that may fail
- Log errors to Console in debug scenarios
- Provide user-friendly error messages in the UI
- Don't suppress exceptions silently

## Testing Recommendations

Before submitting a PR, please test:

1. **Manual Testing**:
   - [ ] Lookup DVDs with various title formats
   - [ ] Test with article-prefixed titles (The, A, An)
   - [ ] Match videos in directories with different video formats
   - [ ] Rename files and verify filesystem integrity
   - [ ] Test on different OS (Windows/macOS/Linux if possible)

2. **Error Cases**:
   - [ ] Invalid directory paths
   - [ ] No videos in directory
   - [ ] Duplicate filenames
   - [ ] No matches found
   - [ ] Network errors during lookup

## Pull Request Process

1. Update documentation (README, CHANGELOG, etc.) if needed
2. Follow the PR template provided
3. Reference any related issues
4. One or more maintainers will review
5. Respond to feedback promptly
6. Once approved, squash commits if requested
7. Maintainer will merge

## Areas for Contribution

### High Priority

- [ ] Unit and integration tests
- [ ] Command-line interface version
- [ ] Configuration file support (save recent searches, preferences)
- [ ] Better error recovery and user guidance

### Medium Priority

- [ ] Subtitle file handling/renaming
- [ ] Additional metadata sources beyond DVD.com
- [ ] Batch directory processing
- [ ] File preview/confirmation before renaming
- [ ] Undo functionality

### Nice to Have

- [ ] Internationalization (i18n)
- [ ] Dark/light theme toggle
- [ ] Custom title formatting rules
- [ ] Regular expression-based matching
- [ ] Database caching of DVD info

## Getting Help

- **Questions**: Open a discussion or issue
- **Documentation**: Check the README and code comments
- **Examples**: Look at existing code in Models/ and ViewModels/

## License

By contributing, you agree that your contributions will be licensed under the MIT License.

## Acknowledgments

Thank you for contributing to make DVD Extras Renamer better for everyone!
