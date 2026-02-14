using System;
using System.IO;
using System.Net.Http;
using System.Threading.Tasks;

namespace EmmcHaccGen.GUI.Utils
{
    public class SdCardPreparation
    {
        private const string TEGRAEXPLORER_URL = "https://github.com/suchmememanyskill/TegraExplorer/releases/download/4.2.0/TegraExplorer.bin";
        private const string SYSTEMRESTORE_URL = "https://suchmememanyskill.github.io/TegraScript/scripts/SystemRestoreV3.te";
        
        private readonly string _generatedFolder;
        private readonly Action<string> _logCallback;
        private readonly Action<int, string>? _progressCallback; // percentage, current operation
        
        public SdCardPreparation(string generatedFolder, Action<string> logCallback, Action<int, string>? progressCallback = null)
        {
            _generatedFolder = generatedFolder;
            _logCallback = logCallback;
            _progressCallback = progressCallback;
        }
        
        public async Task PrepareSDCardAsync(string sdCardPath)
        {
            try
            {
                _logCallback?.Invoke("Starting SD card preparation...");
                _progressCallback?.Invoke(0, "Creating folders...");
                
                // create MMCRebuild folder
                string MMCRebuildFolder = Path.Combine(sdCardPath, "MMCRebuild");
                Directory.CreateDirectory(MMCRebuildFolder);
                _logCallback?.Invoke($"✓ Created folder: MMCRebuild/");
                _progressCallback?.Invoke(10, "Copying boot.bis...");
                
                // copy boot.bis
                string bootBisSource = Path.Combine(_generatedFolder, "boot.bis");
                string bootBisDest = Path.Combine(MMCRebuildFolder, "boot.bis");
                
                if (File.Exists(bootBisSource))
                {
                    await CopyFileWithProgressAsync(bootBisSource, bootBisDest);
                    _logCallback?.Invoke("✓ Copied boot.bis");
                }
                else
                {
                    _logCallback?.Invoke("⚠ Warning: boot.bis not found");
                }
                
                _progressCallback?.Invoke(30, "Copying SYSTEM folder...");
                
                // copy SYSTEM folder
                string systemSource = Path.Combine(_generatedFolder, "SYSTEM");
                string systemDest = Path.Combine(MMCRebuildFolder, "SYSTEM");
                
                if (Directory.Exists(systemSource))
                {
                    await CopyDirectoryWithProgressAsync(systemSource, systemDest, true, 30, 60);
                    int fileCount = Directory.GetFiles(systemDest, "*", SearchOption.AllDirectories).Length;
                    _logCallback?.Invoke($"✓ Copied SYSTEM folder ({fileCount} files)");
                }
                else
                {
                    _logCallback?.Invoke("⚠ Warning: SYSTEM folder not found");
                }
                
                _progressCallback?.Invoke(60, "Downloading SystemRestoreV3.te...");
                
                // download SystemRestoreV3.te
                _logCallback?.Invoke("Downloading SystemRestoreV3.te...");
                string scriptPath = Path.Combine(MMCRebuildFolder, "SystemRestoreV3.te");
                await DownloadFileAsync(SYSTEMRESTORE_URL, scriptPath);
                _logCallback?.Invoke("✓ Downloaded SystemRestoreV3.te");
                
                _progressCallback?.Invoke(75, "Downloading TegraExplorer.bin...");
                
                // download TegraExplorer.bin
                _logCallback?.Invoke("Downloading TegraExplorer.bin...");
                string payloadsFolder = Path.Combine(sdCardPath, "bootloader", "payloads");
                Directory.CreateDirectory(payloadsFolder);
                
                string tegraExplorerPath = Path.Combine(payloadsFolder, "TegraExplorer.bin");
                await DownloadFileAsync(TEGRAEXPLORER_URL, tegraExplorerPath);
                _logCallback?.Invoke("✓ Downloaded TegraExplorer.bin to bootloader/payloads/");
                
                _progressCallback?.Invoke(100, "Complete!");
                
                _logCallback?.Invoke("");
                _logCallback?.Invoke("========================================");
                _logCallback?.Invoke("✓ SD Card preparation complete!");
            }
            catch (Exception ex)
            {
                _logCallback?.Invoke($"✗ Error: {ex.Message}");
                _progressCallback?.Invoke(0, "Error!");
                throw;
            }
        }
        
        // copy progress indicator
        private async Task CopyFileWithProgressAsync(string source, string destination)
        {
            const int bufferSize = 81920;
            
            using (var sourceStream = new FileStream(source, FileMode.Open, FileAccess.Read, FileShare.Read, bufferSize, true))
            using (var destStream = new FileStream(destination, FileMode.Create, FileAccess.Write, FileShare.None, bufferSize, true))
            {
                await sourceStream.CopyToAsync(destStream, bufferSize);
            }
        }
        
        // copy progress indicator
        private async Task CopyDirectoryWithProgressAsync(string sourceDir, string destDir, bool recursive, int startProgress, int endProgress)
        {
            var dir = new DirectoryInfo(sourceDir);
            
            if (!dir.Exists)
                throw new DirectoryNotFoundException($"Source directory not found: {dir.FullName}");
            
            // get all files to copy
            var allFiles = dir.GetFiles("*", recursive ? SearchOption.AllDirectories : SearchOption.TopDirectoryOnly);
            int totalFiles = allFiles.Length;
            int copiedFiles = 0;
            
            Directory.CreateDirectory(destDir);
            
            foreach (FileInfo file in allFiles)
            {
                string relativePath = Path.GetRelativePath(sourceDir, file.FullName);
                string targetFilePath = Path.Combine(destDir, relativePath);
                
                // create subdirectories if needed
                string? targetDir = Path.GetDirectoryName(targetFilePath);
                if (targetDir != null)
                {
                    Directory.CreateDirectory(targetDir);
                }
                
                await CopyFileWithProgressAsync(file.FullName, targetFilePath);
                
                copiedFiles++;
                int progress = startProgress + (int)((double)copiedFiles / totalFiles * (endProgress - startProgress));
                _progressCallback?.Invoke(progress, $"Copying SYSTEM folder... ({copiedFiles}/{totalFiles})");
            }
        }
        
        private async Task DownloadFileAsync(string url, string destination)
        {
            using (var client = new HttpClient())
            {
                client.Timeout = TimeSpan.FromMinutes(5);
                var response = await client.GetAsync(url, HttpCompletionOption.ResponseHeadersRead);
                response.EnsureSuccessStatusCode();
                
                await using (var fileStream = new FileStream(destination, FileMode.Create, FileAccess.Write, FileShare.None))
                {
                    await response.Content.CopyToAsync(fileStream);
                }
            }
        }
    }
}
