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
        
        public SdCardPreparation(string generatedFolder, Action<string> logCallback)
        {
            _generatedFolder = generatedFolder;
            _logCallback = logCallback;
        }
        
        public async Task PrepareSDCardAsync(string sdCardPath)
        {
            try
            {
                _logCallback?.Invoke("Starting SD card preparation...");
                
                // create MMCRebuild folder
                string MMCRebuildFolder = Path.Combine(sdCardPath, "MMCRebuild");
                Directory.CreateDirectory(MMCRebuildFolder);
                _logCallback?.Invoke($"✓ Created folder: MMCRebuild/");
                
                // copy boot.bis
                string bootBisSource = Path.Combine(_generatedFolder, "boot.bis");
                string bootBisDest = Path.Combine(MMCRebuildFolder, "boot.bis");
                
                if (File.Exists(bootBisSource))
                {
                    File.Copy(bootBisSource, bootBisDest, true);
                    _logCallback?.Invoke("✓ Copied boot.bis");
                }
                else
                {
                    _logCallback?.Invoke("⚠ Warning: boot.bis not found");
                }
                
                // copy SYSTEM folder
                string systemSource = Path.Combine(_generatedFolder, "SYSTEM");
                string systemDest = Path.Combine(MMCRebuildFolder, "SYSTEM");
                
                if (Directory.Exists(systemSource))
                {
                    CopyDirectory(systemSource, systemDest, true);
                    int fileCount = Directory.GetFiles(systemDest, "*", SearchOption.AllDirectories).Length;
                    _logCallback?.Invoke($"✓ Copied SYSTEM folder ({fileCount} files)");
                }
                else
                {
                    _logCallback?.Invoke("⚠ Warning: SYSTEM folder not found");
                }
                
                // download SystemRestoreV3.te
                _logCallback?.Invoke("Downloading SystemRestoreV3.te...");
                string scriptPath = Path.Combine(MMCRebuildFolder, "SystemRestoreV3.te");
                await DownloadFileAsync(SYSTEMRESTORE_URL, scriptPath);
                _logCallback?.Invoke("✓ Downloaded SystemRestoreV3.te");
                
                // download TegraExplorer.bin
                _logCallback?.Invoke("Downloading TegraExplorer.bin...");
                string payloadsFolder = Path.Combine(sdCardPath, "bootloader", "payloads");
                Directory.CreateDirectory(payloadsFolder);
                
                string tegraExplorerPath = Path.Combine(payloadsFolder, "TegraExplorer.bin");
                await DownloadFileAsync(TEGRAEXPLORER_URL, tegraExplorerPath);
                _logCallback?.Invoke("✓ Downloaded TegraExplorer.bin to bootloader/payloads/");
                
                _logCallback?.Invoke("");
                _logCallback?.Invoke("========================================");
                _logCallback?.Invoke("✓ SD Card preparation complete!");
            }
            catch (Exception ex)
            {
                _logCallback?.Invoke($"✗ Error: {ex.Message}");
                throw;
            }
        }
        
        private async Task DownloadFileAsync(string url, string destination)
        {
            using (var client = new HttpClient())
            {
                client.Timeout = TimeSpan.FromMinutes(5);
                var response = await client.GetAsync(url);
                response.EnsureSuccessStatusCode();
                
                await using (var fileStream = new FileStream(destination, FileMode.Create, FileAccess.Write, FileShare.None))
                {
                    await response.Content.CopyToAsync(fileStream);
                }
            }
        }
        
        private void CopyDirectory(string sourceDir, string destDir, bool recursive)
        {
            var dir = new DirectoryInfo(sourceDir);
            
            if (!dir.Exists)
                throw new DirectoryNotFoundException($"Source directory not found: {dir.FullName}");
            
            DirectoryInfo[] dirs = dir.GetDirectories();
            Directory.CreateDirectory(destDir);
            
            foreach (FileInfo file in dir.GetFiles())
            {
                string targetFilePath = Path.Combine(destDir, file.Name);
                file.CopyTo(targetFilePath, true);
            }
            
            if (recursive)
            {
                foreach (DirectoryInfo subDir in dirs)
                {
                    string newDestinationDir = Path.Combine(destDir, subDir.Name);
                    CopyDirectory(subDir.FullName, newDestinationDir, true);
                }
            }
        }
    }
}
