# Archive Recovery Tool - WPF Application

A modern WPF application for recovering corrupted archive files by extracting valid content and re-archiving them with real-time monitoring, health validation, and support for 30+ archive formats.

## Features

- **🖥️ Modern WPF Interface**: Clean, intuitive user interface with real-time progress monitoring
- **📊 Real-time Progress**: Live updates showing file processing status, success/failure counts, and progress bars
- **🔧 Configuration UI**: Easy-to-use interface for configuring 7-Zip path, backup directory, and processing options
- **📋 File Management**: Visual list of all archive files with detailed status information including archive type and health status
- **📝 Log Viewer**: Built-in log viewer with filtering, export capabilities, and color-coded messages
- **🛡️ Safe Recovery**: Moves original corrupted files to backup directory before replacing
- **🔄 Batch Processing**: Process multiple archive files with automatic progress tracking
- **⚙️ Flexible Configuration**: Customizable settings for different recovery scenarios
- **🏥 Health Validation**: Skip healthy archives to focus only on corrupted files
- **📦 Multi-Format Support**: Support for 30+ archive formats including ZIP, 7Z, RAR, TAR, CAB, ISO, and more

## Screenshots

The application provides:
- **Configuration Panel**: Set input file, 7-Zip path, backup directory, and processing options
- **Progress Monitoring**: Real-time progress bar, file counts, and status updates
- **File List**: Detailed view of all ZIP files with status, file counts, and sizes
- **Log Viewer**: Comprehensive logging with timestamp, level, and message details

## Supported Archive Formats

The tool supports 30+ archive formats through 7-Zip integration:

### Common Formats
- **ZIP** (.zip) - Most common archive format
- **7-Zip** (.7z) - High compression ratio
- **RAR** (.rar) - WinRAR format
- **TAR** (.tar) - Unix tape archive
- **GZIP** (.gz) - Compressed TAR
- **BZIP2** (.bz2) - Better compression than GZIP
- **XZ** (.xz) - High compression ratio

### System Formats
- **CAB** (.cab) - Microsoft Cabinet files
- **ISO** (.iso) - CD/DVD images
- **MSI** (.msi) - Windows Installer packages
- **DMG** (.dmg) - macOS disk images
- **PKG** (.pkg) - macOS packages
- **DEB** (.deb) - Debian packages
- **RPM** (.rpm) - Red Hat packages

### Legacy Formats
- **ARJ** (.arj) - Legacy compression
- **LZH/LHA** (.lzh, .lha) - Japanese compression
- **ACE** (.ace) - WinACE format
- **Z** (.z) - Unix compress format
- **CPIO** (.cpio) - Unix archive

### Specialized Formats
- **CBZ** (.cbz) - Comic book ZIP
- **CBR** (.cbr) - Comic book RAR
- **CBT** (.cbt) - Comic book TAR
- **CB7** (.cb7) - Comic book 7Z
- **APK** (.apk) - Android packages
- **IPA** (.ipa) - iOS packages
- **JAR/WAR/EAR** (.jar, .war, .ear) - Java archives
- **SWM/WIM/ESD** (.swm, .wim, .esd) - Windows images
- **CHM** (.chm) - Compiled HTML help
- **HFS/HFSX** (.hfs, .hfsx) - macOS filesystem

## Health Validation

The tool includes intelligent health validation to skip healthy archives:

### Health Status Types
- **✅ Healthy**: Archive is intact and can be extracted normally
- **⚠️ Partially Corrupted**: Some files may be damaged but recovery is possible
- **❌ Corrupted**: Archive is severely damaged and may not be recoverable
- **🚫 Unsupported Format**: Archive format is not supported
- **❓ Unknown**: Health status could not be determined

### Validation Process
1. **7-Zip Test**: Uses 7-Zip's built-in test command for accurate health assessment
2. **Header Validation**: Fallback validation using file header analysis
3. **Timeout Protection**: Configurable timeout to prevent hanging on problematic files
4. **Smart Skipping**: Automatically skips healthy archives to focus on corrupted ones

## Prerequisites

- **.NET 9.0 SDK** with Windows Desktop Runtime
- **7-Zip** installed (recommended for full format support, falls back to .NET ZipFile for ZIP only)
- **Windows OS** (WPF application)
- Input file: `data/input.txt` with log entries containing archive file paths

## Installation & Usage

### Build and Run

```bash
# Navigate to project directory
cd tools/ImageViewer.Tools.ZipRecover

# Build the WPF application
dotnet build

# Run the application
dotnet run
```

### Using the Application

1. **Configure Settings**:
   - Set input file path (default: `data/input.txt`)
   - Configure 7-Zip executable path
   - Choose backup directory for original files
   - Enable/disable file validation and corruption handling
   - Configure health check timeout and skipping options

2. **Load Files**:
   - Click "Load Input File" to parse and load archive file paths
   - Review the list of files to be processed with archive types and health status

3. **Start Recovery**:
   - Click "Start Recovery" to begin batch processing
   - Monitor real-time progress and file status updates
   - View detailed logs in the Logs tab
   - Watch health validation results for each archive

4. **Review Results**:
   - Check success/failure statistics
   - Export logs for record keeping
   - Review recovered files in their original locations

## Configuration Options

The application provides UI controls for all configuration options:

- **Input File Path**: Path to the log file containing archive file paths
- **7-Zip Executable**: Path to 7-Zip executable (for robust extraction and multi-format support)
- **Backup Directory**: Where original corrupted files are moved
- **Validate Files**: Enable file header validation for extracted content
- **Skip Corrupted Files**: Skip files that cannot be extracted
- **Skip Healthy Archives**: Automatically skip archives that pass health validation
- **Health Check Timeout**: Maximum time to spend checking archive health (seconds)

## User Interface Features

### Main Window Layout
- **Header**: Application title and branding
- **Configuration Panel**: Settings and file path configuration
- **Progress Panel**: Real-time progress tracking and statistics
- **Tabbed Content**: Files list and log viewer
- **Status Bar**: Current status and summary information

### File Management
- **Visual Status**: Color-coded status indicators (Pending, Processing, Success, Failed)
- **Archive Type**: Shows the specific archive format (ZIP, 7Z, RAR, etc.)
- **Health Status**: Displays archive health validation results
- **Detailed Information**: File counts, sizes, and error messages
- **Batch Operations**: Process all files or individual file management

### Log Viewer
- **Real-time Updates**: Live log entries as processing occurs
- **Color Coding**: Different colors for Information, Success, Warning, Error
- **Export Functionality**: Save logs to text file for analysis
- **Clear Function**: Reset log view when needed

## File Validation

The tool validates extracted files using:

1. **Extension Check**: Only processes image files (.jpg, .jpeg, .png, .gif, .bmp, .webp)
2. **Size Validation**: Skips empty or zero-byte files
3. **Header Validation**: Checks file headers to detect corruption:
   - **JPEG**: Starts with 0xFF 0xD8
   - **PNG**: Starts with 0x89 0x50 0x4E 0x47
   - **GIF**: Starts with "GIF"
   - **BMP**: Starts with "BM"
   - **WebP**: Validates RIFF header structure

## Error Handling & Safety

- **🛡️ Safe Operations**: Original files are always backed up before replacement
- **🔄 Graceful Degradation**: Continues processing even if some files fail
- **📝 Comprehensive Logging**: All operations logged with context and timestamps
- **⚡ Real-time Feedback**: Immediate status updates and error reporting
- **🧹 Automatic Cleanup**: Temporary files cleaned up automatically

## Input Format

The application expects `data/input.txt` to contain log entries like:

```
Line   4:    37  2025-10-13 03:16:02.818 +07:00 [WRN] Error checking images in compressed file L:\EMedia\AI_Generated\Mr. Teardrop\[Mr. Teardrop] Himeko (Honkai Star Rail) (AI Generated).zip. File will be skipped.
Line   8:    45  2025-10-13 03:19:02.889 +07:00 [WRN] Error checking images in compressed file L:\Downloads\Torrents\_Complete\(MohumoAi) Mai Shiranui (Patreon) [Ai Generated].7z. File will be skipped.
Line  12:    53  2025-10-13 03:22:18.310 +07:00 [WRN] Error checking images in compressed file L:\Downloads\Torrents\_Complete\aipornarts collection [ai generated].rar. File will be skipped.
```

The regex pattern automatically extracts archive file paths from these log entries, supporting all configured archive formats.

## Output & Results

- **✅ Recovered Archives**: Original corrupted files replaced with recovered versions
- **📁 Backup Files**: Original corrupted files moved to backup directory
- **📊 Statistics**: Detailed success/failure counts and processing statistics
- **📝 Logs**: Comprehensive processing logs with timestamps and details
- **🧹 Cleanup**: Temporary extraction directories automatically removed
- **🏥 Health Reports**: Archive health validation results for each processed file

## Troubleshooting

### Common Issues

1. **7-Zip Not Found**: Application will fall back to .NET ZipFile automatically
2. **Permission Errors**: Ensure write access to ZIP file locations and backup directory
3. **Disk Space**: Ensure sufficient space for temp extraction and backup operations
4. **File Locked**: Close any applications using the ZIP files before processing

### UI Tips

- **Progress Monitoring**: Watch the progress bar and status updates for real-time feedback
- **Log Viewer**: Check the Logs tab for detailed information about processing
- **File Status**: Use the Files tab to see individual file processing results
- **Export Logs**: Save logs for troubleshooting or record keeping

## Technical Details

- **Framework**: .NET 9.0 WPF Application
- **Architecture**: MVVM pattern with CommunityToolkit.Mvvm
- **Dependency Injection**: Microsoft.Extensions.Hosting for service management
- **Logging**: Microsoft.Extensions.Logging with console and UI integration
- **File Processing**: 7-Zip command line integration with .NET ZipFile fallback

## Safety Features

- **🔄 Atomic Operations**: New ZIP created before replacing original
- **📁 Backup Before Replace**: Original files always backed up safely
- **🧹 Automatic Cleanup**: Temporary files removed automatically
- **✅ Validation**: Only valid files included in recovered ZIP
- **📊 Progress Tracking**: Real-time monitoring prevents data loss
