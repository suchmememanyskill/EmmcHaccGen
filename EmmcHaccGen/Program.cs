using System;
using EmmcHaccGen.nca;
using System.Collections.Generic;
using LibHac;
using System.IO;
using EmmcHaccGen.bis;
using EmmcHaccGen.imkv;
using LibHac.Fs;
using LibHac.Common;
using LibHac.FsSystem.NcaUtils;
using LibHac.FsSystem;
using LibHac.FsSystem.Save;
using System.Linq;
using LibHac.Fs.Fsa;

namespace EmmcHaccGen
{
    class Program
    {
        static string[] FOLDERSTRUCTURE = new string[]
        {
            "/SAFE",
            "/SYSTEM/Contents/placehld",
            "/SYSTEM/Contents/registered",
            "/SYSTEM/save",
            "/USER/Album",
            "/USER/Contents/placehld",
            "/USER/Contents/registered",
            "/USER/save",
            "/USER/saveMeta",
            "/USER/temp"
        };
        void SetArchiveRecursively(string path)
        {
            foreach (var file in Directory.EnumerateFiles(path))
            {
                File.SetAttributes(file, FileAttributes.Archive);
            }
            foreach (var folder in Directory.EnumerateDirectories(path))
            {
                File.SetAttributes(folder, 0);
                SetArchiveRecursively(folder);
            }
        }
        void ShowNcaIndex(ref NcaIndexer ncaIndex, string version)
        {
            Console.WriteLine($"\nVersion: {version}\nNcaCount: {ncaIndex.files.Count}\n");
            foreach(KeyValuePair<string, List<NcaFile>> ncaPair in ncaIndex.sortedNcaDict)
            {
                Console.WriteLine($"Titleid: {ncaPair.Key}");
                foreach(NcaFile file in ncaPair.Value)
                {
                    Console.WriteLine($"    - Nca: {file.filename} >> ID: {file.header.TitleId:x16}, Type: {file.header.ContentType}");
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

            Program program = new Program();
            program.Start(keys, fw, noExfat, verbose, showNcaIndex, fixHashes, noAutorcm, mariko);
        }
        void Start(string keys, string fwPath, bool noExfat, bool verbose, bool showNcaIndex, bool fixHashes, bool noAutoRcm, bool mariko)
        {
            Config.keyset = ExternalKeyReader.ReadKeyFile(keys);
            Config.fwPath = fwPath;
            Config.noExfat = noExfat;
            Config.normalBisId = (noExfat) ? "0100000000000819" : "010000000000081B";
            Config.safeBisId = (noExfat) ? "010000000000081A" : "010000000000081C";
            Config.verbose = verbose;
            Config.fixHashes = fixHashes;
            Config.noAutoRcm = mariko || noAutoRcm;
            Config.marikoBoot = mariko;

            int convertCount = 0;
            foreach (var foldername in Directory.GetDirectories(fwPath, "*.nca"))
            {
                convertCount++;
                File.Move($"{foldername}/00", $"{fwPath}/temp");
                Directory.Delete(foldername);
                File.Move($"{fwPath}/temp", foldername);
            }

            if (convertCount > 0)
                Console.WriteLine($"Converted folder ncas to files (count: {convertCount})");

            Console.WriteLine("Indexing nca files...");

            NcaIndexer ncaIndex = new NcaIndexer();

            VersionExtractor versionExtractor = new VersionExtractor(ncaIndex.FindNca("0100000000000809", NcaContentType.Meta));

            Config.v5 = versionExtractor.UseV5Save();

            string prefix = (Config.marikoBoot) ? "a" : "NX";
            string destFolder = $"{prefix}-{versionExtractor.Version}";
            if (!noExfat)
                destFolder += "_exFAT";

            if (showNcaIndex)
            {
                ShowNcaIndex(ref ncaIndex, destFolder);
                return;
            }

            string saveVersion = (Config.v5) ? "v5" : "v4";

            Console.WriteLine("\nEmmcHaccGen will now generate firmware files using the following settings:\n" +
                $"fw: {versionExtractor.Version}\n" + 
                $"Exfat Support: {!noExfat}\n" +
                $"Key path: {keys}\n" +
                $"Destination folder: {destFolder}\n" +
                $"Mariko boot generation: {mariko}\n" +
                $"Save version: {saveVersion}\n" +
                $"AutoRCM: {!Config.noAutoRcm}\n");

            if (verbose)
                Console.WriteLine($"BisIds:\nNormal: {Config.normalBisId}\nSafe:   {Config.safeBisId}\n");

            // Folder creation
            Console.WriteLine("\nCreating folders..");

            if (Directory.Exists(destFolder))
            {
                Console.Write("Destination folder already exists. Delete the old folder?\nY/N: ");
                string input = Console.ReadLine();

                if (input[0].ToString().ToLower() != "y")
                    return;

                Console.WriteLine($"Deleting {destFolder}");
                Directory.Delete(destFolder, true);
            }

            foreach(string folder in FOLDERSTRUCTURE)
                Directory.CreateDirectory($"{destFolder}{folder}");

            // Bis creation
            Console.WriteLine("\nGenerating bis..");
            BisAssembler bisAssembler = new BisAssembler(ref ncaIndex, destFolder);
            BisFileAssembler bisFileAssembler = new BisFileAssembler($"{versionExtractor.Version}{((!noExfat) ? "_exFAT" : "")}", ref bisAssembler, $"{destFolder}/boot.bis");

            // Copy fw files
            Console.WriteLine("\nCopying files...");
            foreach (var file in Directory.EnumerateFiles(fwPath))
            {
                File.Copy(file, $"{destFolder}/SYSTEM/Contents/registered/{file.Split(new char[] { '/', '\\' }).Last().Replace(".cnmt.nca", ".nca")}", true);
            }

            // Archive bit setting
            Console.WriteLine("\nSetting archive bits..");
            SetArchiveRecursively($"{destFolder}/SYSTEM");
            SetArchiveRecursively($"{destFolder}/USER");

            //Imkv generation
            Console.WriteLine("\nGenerating imkvdb..");
            Imkv imkvdb = new Imkv(ref ncaIndex);

            if (verbose)
                imkvdb.DumpToFile($"{destFolder}/data.arc");

            File.Copy($"save.stub.{saveVersion}", $"{destFolder}/SYSTEM/save/8000000000000120", true);

            using (IStorage outfile = new LocalStorage($"{destFolder}/SYSTEM/save/8000000000000120", FileAccess.ReadWrite))
            {
                var save = new SaveDataFileSystem(Config.keyset, outfile, IntegrityCheckLevel.ErrorOnInvalid, true);
                save.OpenFile(out IFile file, new U8Span("/meta/imkvdb.arc"), OpenMode.AllowAppend | OpenMode.ReadWrite);
                using (file)
                {
                    file.Write(0, imkvdb.bytes.ToArray(), WriteOption.Flush).ThrowIfFailure();
                }
                save.Commit(Config.keyset).ThrowIfFailure();
            }
            Console.WriteLine($"Wrote save with an imvkdb size of 0x{imkvdb.bytes.Count:X4}");
        }
    }
}
