# DVD Extras Renamer

A cross-platform desktop application built with Avalonia that automatically matches and renames video files in your DVD extras directories based on metadata from [dvdcompare.net](https://www.dvdcompare.net).

I wanted to make it easier to identify and rename the bonus features that I rip off my disks with [MakeMKV](https://www.makemkv.com/). This is easier than manually checking the length of each extra that comes off the disk and comparing it against the list of titles found online. Maybe someone else will find it useful too.

## Features

- **DVD Lookup**: Search dvdcompare.net for DVD titles, years, and directors
- **Automatic Video Matching**: Match video files by duration (±1 second tolerance) to DVD extras
- **Intelligent Title Cleanup**: Removes descriptors (documentary, featurette), quotes, and invalid filesystem characters
- **Batch Renaming**: Rename matched videos with a single click per file
- **Cross-Platform**: Runs on Windows, macOS, and Linux

## Supported Video Formats

- MP4 (.mp4)
- Matroska (.mkv)
- AVI (.avi)
- MOV (.mov)
- Flash Video (.flv)
- Windows Media (.wmv)
- WebM (.webm)

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
