using System;
using EmmcHaccGen.nca;
using System.Collections.Generic;
using LibHac;
using System.IO;
using EmmcHaccGen.bis;
using EmmcHaccGen.imkv;
using LibHac.Fs;
using LibHac.FsSystem.NcaUtils;
using LibHac.FsSystem;
using LibHac.FsSystem.Save;

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
        /// <param name="noExfat">noExfat switch. Add this if you don't want exfat support. Disabled by default</param>
        /// <param name="verbose">Enable verbose output. Disabled by default</param>
        /// <param name="showNcaIndex">Show info about nca's, like it's titleid and type. Will not generate a firmware folder with this option enabled</param>
        static void Main(string keys=null, string fw=null, bool noExfat=false, bool verbose=false, bool showNcaIndex=false)
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
            program.Start(keys, fw, noExfat, verbose,showNcaIndex);
        }
        void Start(string keys, string fwPath, bool noExfat, bool verbose, bool showNcaIndex)
        {
            Config.keyset = ExternalKeyReader.ReadKeyFile(keys);
            Config.fwPath = fwPath;
            Config.noExfat = noExfat;
            Config.normalBisId = (noExfat) ? "0100000000000819" : "010000000000081B";
            Config.safeBisId = (noExfat) ? "010000000000081A" : "010000000000081C";
            Config.verbose = verbose;

            int convertCount = 0;
            foreach (var foldername in Directory.GetDirectories(fwPath))
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

            NcaFile versionNca = ncaIndex.FindNca("0100000000000809", NcaContentType.Data);
            VersionExtractor versionExtractor = new VersionExtractor(versionNca);

            string destFolder = $"{versionExtractor.platform.ToUpper()}-{versionExtractor.version}";
            if (!noExfat)
                destFolder += "_exFAT";

            if (showNcaIndex)
            {
                ShowNcaIndex(ref ncaIndex, destFolder);
                return;
            }

            Console.WriteLine("\nEmmcHaccGen will now generate firmware files using the following settings:\n" +
                $"fw: {versionExtractor.platform}-{versionExtractor.version}\n" + 
                $"Exfat Support: {!noExfat}\n" +
                $"Key path: {keys}\n" +
                $"Destination folder: {destFolder}\n");

            if (verbose)
                Console.WriteLine($"BisIds:\nNormal: {Config.normalBisId}\nSafe:   {Config.safeBisId}\n");

            // Folder creation
            Console.WriteLine("\nCreating folders..");

            foreach(string folder in FOLDERSTRUCTURE)
                Directory.CreateDirectory($"{destFolder}{folder}");

            // Bis creation
            Console.WriteLine("\nGenerating bis..");
            BisAssembler bisAssembler = new BisAssembler(ref ncaIndex, destFolder);
            BisFileAssembler bisFileAssembler = new BisFileAssembler($"{versionExtractor.platform.ToUpper()}-{versionExtractor.version}{((!noExfat) ? "_exFAT" : "")}", ref bisAssembler, $"{destFolder}/boot.bis");

            // Copy fw files
            Console.WriteLine("\nCopying files...");
            foreach (var file in Directory.EnumerateFiles(fwPath))
            {
                File.Copy(file, $"{destFolder}/SYSTEM/Contents/registered/{file.Substring(fwPath.Length + 1)}", true);
            }

            // Archive bit setting
            Console.WriteLine("\nSetting archive bits..");
            SetArchiveRecursively($"{destFolder}/SYSTEM");
            SetArchiveRecursively($"{destFolder}/USER");

            //Imkv generation
            Console.WriteLine("\nGenerating imkv..");
            Imkv imkvdb = new Imkv(ref ncaIndex);

            if (verbose)
                imkvdb.DumpToFile($"{destFolder}/data.arc");

            File.Copy("save.stub", $"{destFolder}/SYSTEM/save/8000000000000120", true);

            using (IStorage outfile = new LocalStorage($"{destFolder}/SYSTEM/save/8000000000000120", FileAccess.ReadWrite))
            {
                var save = new SaveDataFileSystem(Config.keyset, outfile, IntegrityCheckLevel.ErrorOnInvalid, true);
                save.OpenFile(out IFile file, "/meta/imkvdb.arc", OpenMode.AllowAppend | OpenMode.ReadWrite);
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
