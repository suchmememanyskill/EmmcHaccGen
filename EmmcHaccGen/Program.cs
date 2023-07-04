using System;
using EmmcHaccGen.nca;
using System.Collections.Generic;
using System.IO;
using EmmcHaccGen.bis;
using EmmcHaccGen.imkv;
using LibHac.Fs;
using LibHac.Common;
using LibHac.FsSystem;
using System.Linq;
using LibHac.Fs.Fsa;
using LibHac.Common.Keys;
using LibHac.Tools.FsSystem;
using LibHac.Tools.FsSystem.NcaUtils;
using LibHac.Tools.FsSystem.Save;
using System.Reflection;
using Path = System.IO.Path;

namespace EmmcHaccGen
{
    class Program
    {
        void ShowNcaIndex(NcaIndexer ncaIndex, string version)
        {
            Console.WriteLine($"\nVersion: {version}\nNcaCount: {ncaIndex.Files.Count}\n");
            foreach(KeyValuePair<string, List<NcaFile>> ncaPair in ncaIndex.SortedFiles)
            {
                Console.WriteLine($"Titleid: {ncaPair.Key}");
                foreach(NcaFile file in ncaPair.Value)
                {
                    Console.WriteLine($"    - Nca: {file.FileName} >> ID: {file.Header.TitleId:x16}, Type: {file.Header.ContentType}");
                }
                Console.WriteLine();
            }
        }
        /// <summary>
        /// Generates boot files for the Nintendo Switch. Generates Boot01, bcpkg2 and the 120 system save.
        /// </summary>
        /// <param name="keys">Path to your keyset file</param>
        /// <param name="fw">Path to your firmware folder</param>
        /// <param name="noExfat">Disables exfat support on generated firmware when enabled. Disabled by default</param>
        /// <param name="verbose">Enable verbose output. Disabled by default</param>
        /// <param name="showNcaIndex">Show info about nca's, like it's titleid and type. Will not generate a firmware folder with this option enabled</param>
        /// <param name="fixHashes">Fix incorrect hashes in the source firmware folder. Disabled by default</param>
        /// <param name="mariko">Enables mariko boot generation (and disables autorcm)</param>
        /// <param name="noAutorcm">Disables AutoRcm</param>
        static void Main(string keys=null, string fw=null, bool noExfat=false, bool verbose=false, bool showNcaIndex=false, bool fixHashes=false, bool noAutorcm=false, bool mariko=false)
        {
            Console.WriteLine("EmmcHaccGen started");
            

            if (keys == null || fw == null)
            {
                Console.WriteLine("Missing arguments. Type 'EmmcHaccGen.exe -h' for commandline usage");
                return;
            }

            if (!File.Exists(keys))
            {
                Console.WriteLine("Keyset file not found");
                return;
            }

            if (!Directory.Exists(fw))
            {
                Console.WriteLine("Firmware path not found");
                return;
            }

            if (mariko)
                noAutorcm = true;

            Program program = new Program();
            try
            {
                program.Start(keys, fw, noExfat, verbose, showNcaIndex, fixHashes, noAutorcm, mariko);
            }
            catch (Exception e)
            {
                Console.WriteLine($"[Exception] {e.Message}");
                Console.WriteLine(e.StackTrace);
                Environment.Exit(1);
            }
        }
        void Start(string keys, string fwPath, bool noExfat, bool verbose, bool showNcaIndex, bool fixHashes, bool noAutoRcm, bool mariko)
        {
            Console.WriteLine("Indexing NCA files...");
            LibEmmcHaccGen lib = new(keys, fwPath, fixHashes);
            
            Console.WriteLine("Extracted firmware:");
            Console.WriteLine($"Version: {lib.NcaIndexer.Version}");
            Console.WriteLine("Save Version: " + (lib.NcaIndexer.RequiresV5Save ? "v5" : "v4"));
            Console.WriteLine("Exfat Support: " + (lib.HasExfatCompat ? "Yes" : "No"));
            Console.WriteLine($"NCA Count: {lib.NcaIndexer.Files.Count}");
            Console.WriteLine("\n");

            if (showNcaIndex)
            {
                ShowNcaIndex(lib.NcaIndexer, lib.NcaIndexer.Version);
                return;
            }
            
            string prefix = (mariko) ? "a" : "NX";
            string destFolder = $"{prefix}-{lib.NcaIndexer.Version}" + (!noExfat ? "_exFAT" : "");
            string destPath = Path.Join(".", destFolder);

            if (Directory.Exists(destPath))
            {
                Console.Write("Output directory already exists. Overwrite? (Y/N): ");
                string line = Console.ReadLine()!;
                
                if (line.ToLower()[0] == 'y')
                    Directory.Delete(destFolder, true);
                else
                    throw new Exception("Output directory already exists");
            }

            Directory.CreateDirectory(destPath);

            Console.WriteLine("Generating BIS (boot0, boot1, BCPKG2 1-4)...");
            lib.WriteBis(destPath, !noAutoRcm, !noExfat, mariko);
            
            Console.WriteLine("Generating System...");
            lib.WriteSystem(destFolder, !noExfat, lib.NcaIndexer.RequiresV5Save || mariko, verbose);
        }
    }
}
