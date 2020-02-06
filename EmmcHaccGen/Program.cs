using System;
using System.Collections.Generic;
using LibHac;
using LibHac.FsSystem.NcaUtils;
using LibHac.Fs;
using LibHac.FsSystem;
using System.IO;
using System.Linq;
using LibHac.FsSystem.Save;

namespace EmmcHaccGen
{
    class Program
    {
        Keyset keyset;
        ncaList parseNca(string filename, string path)
        {
            ncaList entry = new ncaList();
            entry.filename = filename;

            using (IStorage infile = new LocalStorage(path, FileAccess.Read))
            {
                Nca nca = new Nca(keyset, infile);
                entry.titleid = $"{nca.Header.TitleId:X16}";
                entry.type = nca.Header.ContentType;
                entry.size = nca.Header.NcaSize;
                if (entry.type == NcaContentType.Meta)
                {
                    using (IFileSystem fs = nca.OpenFileSystem(NcaSectionType.Data, IntegrityCheckLevel.ErrorOnInvalid))
                    {
                        string cnmtPath = fs.EnumerateEntries("/", "*.cnmt").Single().FullPath;
                        IFile tempfile;
                        fs.OpenFile(out tempfile, cnmtPath, OpenMode.Read).ThrowIfFailure();
                        entry.cnmt = new Cnmt(tempfile.AsStream());
                        tempfile.GetSize(out long size);
                        entry.raw_cnmt = new byte[size];
                        tempfile.Read(out long temp, 0, entry.raw_cnmt);
                        tempfile.Dispose();
                    }
                }
            }

            return entry;
        }
        void SetArchiveRecursively(string path)
        {
            foreach(var file in Directory.EnumerateFiles(path))
            {
                File.SetAttributes(file, FileAttributes.Archive);
            }
            foreach(var folder in Directory.EnumerateDirectories(path))
            {
                File.SetAttributes(folder, 0);
                SetArchiveRecursively(folder);
            }
        }
        Dictionary<string, List<ncaList>> SortNca(List<ncaList> list, List<string> forbiddenlist)
        {
            Dictionary<string, List<ncaList>> NcaDict = new Dictionary<string, List<ncaList>>();
            Dictionary<string, List<ncaList>> SortedNcaDict = new Dictionary<string, List<ncaList>>();

            foreach (ncaList nca in list)
            {
                if (forbiddenlist.Contains(nca.titleid))
                    continue;

                if (NcaDict.ContainsKey(nca.titleid))
                    NcaDict[nca.titleid].Add(nca);
                else
                {
                    List<ncaList> templist = new List<ncaList>();
                    templist.Add(nca);
                    NcaDict.Add(nca.titleid, templist);
                }
            }

            for (int i = 0; i < NcaDict.Count; i++)
            {
                var ncalist = NcaDict.ElementAt(i);
                var ncalistsorted = ncalist.Value.OrderBy(i => i.type != NcaContentType.Meta).ToList();
                NcaDict[ncalist.Key] = ncalistsorted;
            }

            foreach (var item in NcaDict.OrderBy(x => Convert.ToInt64(x.Key, 16)))
                SortedNcaDict.Add(item.Key, item.Value);

            return SortedNcaDict;
        }
        /// <summary>Generates required files to boot a switch. Generates BIS (boot01, bcpkg2) and the 120 system save</summary>
        /// <param name="keys">Path to your prod.keys file. Required argument</param>
        /// <param name="fw">Path to your firmware folder. Required argument</param>
        /// <param name="noexfat">non-Exfat generation option. Default is false</param>
        static void Main(string keys = null, string fw = null, bool noexfat = false)
        {
            if (keys == null || fw == null)
            {
                Console.WriteLine("Missing arguments!\nType 'EmmcHaccGen.exe -h' for help");
                return;
            }

            if (!File.Exists(keys))
            {
                Console.WriteLine("Keyset file not found.");
                return;
            }

            Console.WriteLine("EmmcHaccGen started");
            Program yeet = new Program();
            yeet.Start(keys, fw, noexfat);
        }
        void Start(string keys = null, string fw = null, bool noexfat = false)
        {
            if (BitConverter.IsLittleEndian == false)
                throw new ArgumentException("Bitconverter is not converting to little endian!");

            List<ncaList> ncalist = new List<ncaList>();
            keyset = ExternalKeyReader.ReadKeyFile(keys);
            bis boot0 = new bis(0x180000);
            bis boot1 = new bis(0x80000);
            bis bcpkg2_1 = new bis(0x800000);
            bis bcpkg2_3 = new bis(0x800000);
            string destFolder;

            string NormalLoc = (noexfat) ? "0100000000000819" : "010000000000081B";
            string SafeLoc = (noexfat) ? "010000000000081A" : "010000000000081C";
            List<string> forbiddenlist = new List<string>();

            if (noexfat)
            {
                forbiddenlist.Add("010000000000081B");
                forbiddenlist.Add("010000000000081C");
            }

            int convertCount = 0;
            foreach (var foldername in Directory.GetDirectories(fw))
            {
                convertCount++;
                File.Move($"{foldername}/00", $"{fw}/temp");
                Directory.Delete(foldername);
                File.Move($"{fw}/temp", foldername);
            }

            if (convertCount > 0)
                Console.WriteLine($"Converted folder ncas to files (count: {convertCount})");

            Console.WriteLine($"Key path: {keys}\nFw folder path: {fw}\nExfat support: {!noexfat}\nNormalLoc: {NormalLoc}\nSafeLoc: {SafeLoc}\n");

            Console.WriteLine($"Parsing Nca's.... (Count: {Directory.GetFiles(fw, "*.nca").Length})");
            foreach (var file in Directory.EnumerateFiles(fw, "*.nca"))
            {
                ncalist.Add(parseNca(file.Substring(fw.Length + 1), file.ToString()));
            }

            ncaList Normal = ncalist.Find(x => x.titleid == NormalLoc && x.type == NcaContentType.Data);
            ncaList Safe = ncalist.Find(x => x.titleid == SafeLoc && x.type == NcaContentType.Data);
            ncaList Version = ncalist.Find(x => x.titleid == "0100000000000809" && x.type == NcaContentType.Data);

            ncaVersionExtractor versionExtractor = new ncaVersionExtractor($"{fw}/{Version.filename}", keyset);
            versionExtractor.Parse();
            Console.WriteLine($"Detected: {versionExtractor.platformString}-{versionExtractor.versionString}");
            destFolder = $"{versionExtractor.platformString.ToUpper()}-{versionExtractor.versionString}";
            if (!noexfat)
                destFolder += "_exFAT";

            Console.WriteLine($"Making folder {destFolder} and subfolders...");
            Directory.CreateDirectory(destFolder);
            Directory.CreateDirectory($"{destFolder}/SAFE");
            Directory.CreateDirectory($"{destFolder}/SYSTEM/Contents/placehld");
            Directory.CreateDirectory($"{destFolder}/SYSTEM/Contents/registered");
            Directory.CreateDirectory($"{destFolder}/SYSTEM/save");
            Directory.CreateDirectory($"{destFolder}/USER/Album");
            Directory.CreateDirectory($"{destFolder}/USER/Contents/placehld");
            Directory.CreateDirectory($"{destFolder}/USER/Contents/registered");
            Directory.CreateDirectory($"{destFolder}/USER/save");
            Directory.CreateDirectory($"{destFolder}/USER/saveMeta");
            Directory.CreateDirectory($"{destFolder}/USER/temp");


            Console.WriteLine("Generating BIS...");
            ncaBisExtractor NormalExtractor = new ncaBisExtractor($"{fw}/{Normal.filename}", keyset);
            ncaBisExtractor SafeExtractor = new ncaBisExtractor($"{fw}/{Safe.filename}", keyset);

            NormalExtractor.Extract();
            SafeExtractor.Extract();

            boot0.Write(NormalExtractor.bct);
            boot0.Pad(0x4000 - NormalExtractor.bct.Length);
            boot0.Write(SafeExtractor.bct);
            boot0.Pad(0x4000 - SafeExtractor.bct.Length);
            boot0.Write(NormalExtractor.bct);
            boot0.Pad(0x4000 - NormalExtractor.bct.Length);
            boot0.Write(SafeExtractor.bct);
            boot0.Pad(0x4000 - SafeExtractor.bct.Length);
            boot0.Pad(0xF0000);
            boot0.Write(NormalExtractor.pkg1);
            boot0.Pad(0x40000 - NormalExtractor.pkg1.Length);
            boot0.Write(NormalExtractor.pkg1);
            boot0.Pad(0x40000 - NormalExtractor.pkg1.Length);
            boot0.DumpToFile($"{destFolder}/BOOT0.bin");

            boot1.Write(SafeExtractor.pkg1);
            boot1.Pad(0x40000 - SafeExtractor.pkg1.Length);
            boot1.Write(SafeExtractor.pkg1);
            boot1.Pad(0x40000 - SafeExtractor.pkg1.Length);
            boot1.DumpToFile($"{destFolder}/BOOT1.bin");

            bcpkg2_1.Pad(0x4000);
            bcpkg2_1.Write(NormalExtractor.pkg2);
            bcpkg2_1.DumpToFile($"{destFolder}/BCPKG2-1-Normal-Main.bin");
            bcpkg2_1.DumpToFile($"{destFolder}/BCPKG2-2-Normal-Sub.bin");

            bcpkg2_3.Pad(0x40000);
            bcpkg2_3.Write(SafeExtractor.pkg2);
            bcpkg2_3.DumpToFile($"{destFolder}/BCPKG2-3-SafeMode-Main.bin");
            bcpkg2_3.DumpToFile($"{destFolder}/BCPKG2-4-SafeMode-Sub.bin");

            Console.WriteLine("Copying files...");
            foreach (var file in Directory.EnumerateFiles(fw))
            {
                File.Copy(file, $"{destFolder}/SYSTEM/Contents/registered/{file.Substring(fw.Length + 1)}", true);
            }

            Console.WriteLine("Setting archive bits...");
            SetArchiveRecursively($"{destFolder}/SYSTEM");
            SetArchiveRecursively($"{destFolder}/USER");

            Console.WriteLine("Generating imkvdb...");

            Dictionary<string, List<ncaList>> SortedNcaPairDict = SortNca(ncalist, forbiddenlist);
            List<imen> imenlist = new List<imen>();

            foreach (var pair in SortedNcaPairDict)
            {
                imen newimen = new imen(pair.Value);
                newimen.Gen();
                imenlist.Add(newimen);
            }

            imkv final = new imkv(imenlist);
            final.Build();
            //final.DumpToFile("imkvdb.arc");

            File.Copy("save.stub", $"{destFolder}/SYSTEM/save/8000000000000120", true);

            using (IStorage outfile = new LocalStorage($"{destFolder}/SYSTEM/save/8000000000000120", FileAccess.ReadWrite))
            {
                var save = new SaveDataFileSystem(keyset, outfile, IntegrityCheckLevel.ErrorOnInvalid, true);
                save.OpenFile(out IFile file, "/meta/imkvdb.arc", OpenMode.AllowAppend | OpenMode.ReadWrite);
                using (file)
                {
                    file.Write(0, final.bytes.ToArray(), WriteOption.Flush).ThrowIfFailure();
                }
                save.Commit(keyset).ThrowIfFailure();
            }
            Console.WriteLine($"Wrote save with an imvkdb size of 0x{final.bytes.Count:X4}");
        }


    }
}
