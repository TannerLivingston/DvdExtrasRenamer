# Changelog

All notable changes to this project will be documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.0.0/),
and this project adheres to [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

## [Unreleased]

### Added
- Support for video duration matching with ±1 second tolerance
- Automatic title formatting for articles (The, A, An)
- Intelligent title cleanup (removes descriptors, quotes, invalid filesystem characters)
- Real-time progress feedback during video matching and renaming
- Cross-platform support (Windows, macOS, Linux)
- Light theme with proper contrast and accessibility

### Fixed
- TextBox focus styling on macOS (white background with readable text)
- Button hover states (maintains visibility instead of becoming transparent)
- TextView selection behavior preventing text input

### Technical
- Implemented MVVM pattern with CommunityToolkit.MVVM
- Added async/await for non-blocking file operations
- Integrated TagLibSharp for reliable video metadata extraction
- Used HtmlAgilityPack for robust HTML parsing

## [0.1.0] - 2026-02-10

### Added
- Initial release
- DVD lookup from dvdcompare.net
- Video file matching by duration
- File renaming with cleaned titles
- Directory selection and browsing
- Support for MP4, MKV, AVI, MOV, FLV, WMV, WebM formats

[Unreleased]: https://github.com/TannerLivingston/DvdExtrasRenamer/compare/v0.1.0...HEAD
[0.1.0]: https://github.com/TannerLivingston/DvdExtrasRenamer/releases/tag/v0.1.0
