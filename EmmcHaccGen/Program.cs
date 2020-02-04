using System;
using System.Collections.Generic;
using LibHac;
using LibHac.FsSystem.NcaUtils;
using LibHac.Fs;
using LibHac.FsSystem;
using System.IO;
using System.Linq;

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
        /*
        Byte[] ReadFile(ref IFileSystem filesystem, string path)
        {
            Byte[] tempByte;
            IFile tempFile;
            long readlength, writelength;

            filesystem.OpenFile(out tempFile, path, OpenMode.Read);
            tempFile.GetSize(out readlength);
            tempByte = new Byte[readlength];
            tempFile.Read(out writelength, 0, tempByte, ReadOption.None);
            tempFile.Dispose();

            Console.WriteLine($"File: {path}, ReadSize: {readlength}, OutSize: {writelength}");

            return tempByte;
        }
        void Pad(ref List<Byte> bytelist, long padamount)
        {
            for (int i = 0; i < padamount; i++)
            {
                bytelist.Add(0);
            }
        }
        void AddBctBytes(ref List<Byte> bytelist, ref IFileSystem filesystem)
        {
            Byte[] tempByte;

            tempByte = ReadFile(ref filesystem, "/nx/bct");
            tempByte[0x210] = 0x77;

            bytelist.AddRange(tempByte.ToList());
            Pad(ref bytelist, (0x4000 - tempByte.Length));
        }
        void AddPkg1Bytes(ref List<Byte> bytelist, ref IFileSystem filesystem)
        {
            Byte[] tempByte;

            tempByte = ReadFile(ref filesystem, "/nx/package1");

            bytelist.AddRange(tempByte.ToList());
            Pad(ref bytelist, (0x40000 - tempByte.Length));
        }
        void AddPkg2Bytes(ref List<Byte> bytelist, ref IFileSystem filesystem, long extrapaddingsize)
        {
            Byte[] tempByte;

            tempByte = ReadFile(ref filesystem, "/nx/package2");

            bytelist.AddRange(tempByte.ToList());
            Pad(ref bytelist, (0x800000 - tempByte.Length - extrapaddingsize));
        }
        */
        Dictionary<string, List<ncaList>> SortNca(List<ncaList> list)
        {
            Dictionary<string, List<ncaList>> NcaDict = new Dictionary<string, List<ncaList>>();

            foreach (ncaList nca in list)
            {
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

            return NcaDict;
        }
        static void Main(string[] args)
        {
            Console.WriteLine("Hello World!");
            Program yeet = new Program();
            yeet.Start();
        }
        void Start()
        {
            if (BitConverter.IsLittleEndian == false)
                throw new ArgumentException("Bitconverter is not converting to little endian!");

            List<ncaList> ncalist = new List<ncaList>();
            keyset = ExternalKeyReader.ReadKeyFile("prod.keys");
            /*
            List<Byte> boot0 = new List<byte>();
            List<Byte> boot1 = new List<byte>();
            List<Byte> bcpkg2_1 = new List<byte>();
            List<Byte> bcpkg2_3 = new List<byte>();
            */
            bis boot0 = new bis(0x180000);
            bis boot1 = new bis(0x80000);
            bis bcpkg2_1 = new bis(0x800000);
            bis bcpkg2_3 = new bis(0x800000);

            foreach (var file in Directory.EnumerateFiles("9.1.0", "*.nca"))
            {
                //Console.WriteLine(file.Substring("9.1.0".Length + 1));
                ncalist.Add(parseNca(file.Substring("9.1.0".Length + 1), file.ToString()));
            }

            ncaList Normal = ncalist.Find(x => x.titleid == "0100000000000819" && x.type == NcaContentType.Data);
            ncaList Safe = ncalist.Find(x => x.titleid == "010000000000081A" && x.type == NcaContentType.Data);

            ncaBisExtractor NormalExtractor = new ncaBisExtractor($"9.1.0\\{Normal.filename}", keyset);
            ncaBisExtractor SafeExtractor = new ncaBisExtractor($"9.1.0\\{Safe.filename}", keyset);

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
            boot0.DumpToFile("boot0.testnew");

            boot1.Write(SafeExtractor.pkg1);
            boot1.Pad(0x40000 - SafeExtractor.pkg1.Length);
            boot1.Write(SafeExtractor.pkg1);
            boot1.Pad(0x40000 - SafeExtractor.pkg1.Length);
            boot1.DumpToFile("boot1.testnew");

            bcpkg2_1.Pad(0x4000);
            bcpkg2_1.Write(NormalExtractor.pkg2);
            bcpkg2_1.DumpToFile("bcpkg2_1.testnew");

            bcpkg2_3.Pad(0x40000);
            bcpkg2_3.Write(SafeExtractor.pkg2);
            bcpkg2_3.DumpToFile("bcpkg2_3.testnew");

            Dictionary<string, List<ncaList>> test = SortNca(ncalist);
            List<imen> imenlist = new List<imen>();

            foreach (KeyValuePair<string, List<ncaList>> hi in test)
            {
                /*
                Console.WriteLine($"Key: {hi.Key}");

                foreach (ncaList hi2 in hi.Value)
                {
                    Console.WriteLine($"^- {hi2.filename} {hi2.type}");
                }
                */
                imen newimen = new imen(keyset, hi.Value);
                newimen.Gen();
                imenlist.Add(newimen);
            }
            /*
            imen testimen = new imen(keyset, test["0100000000000816"]);
            testimen.Gen();
            //imen test2 = new imen(keyset, test.ElementAt(0).Value);
            //test2.GetCnmt($"9.1.0\\{test.ElementAt(0).Value[0].filename}");
            //test2.Gen();



            
            foreach (var entry in ncalist)
            {
                Console.WriteLine($"File: {entry.filename}, TitleID: {entry.titleid}, Type: {entry.type}");
            }

            ncaList Normal = ncalist.Find(x => x.titleid == "0100000000000819" && x.type == NcaContentType.Data);
            ncaList Safe = ncalist.Find(x => x.titleid == "010000000000081A" && x.type == NcaContentType.Data);

            IStorage inNormal = new LocalStorage($"9.1.0\\{Normal.filename}", FileAccess.Read);
            var ncaNormal = new Nca(keyset, inNormal);
            IFileSystem filesystemNormal = ncaNormal.OpenFileSystem(NcaSectionType.Data, IntegrityCheckLevel.None);

            IStorage inSafe = new LocalStorage($"9.1.0\\{Safe.filename}", FileAccess.Read);
            var ncaSafe = new Nca(keyset, inSafe);
            IFileSystem filesystemSafe = ncaSafe.OpenFileSystem(NcaSectionType.Data, IntegrityCheckLevel.None);

            foreach (var temp in filesystemSafe.EnumerateEntries())
            {
                Console.WriteLine(temp.FullPath);
            }

            AddBctBytes(ref boot0, ref filesystemNormal);
            AddBctBytes(ref boot0, ref filesystemSafe);
            AddBctBytes(ref boot0, ref filesystemNormal);
            AddBctBytes(ref boot0, ref filesystemSafe);
            Pad(ref boot0, 0xF0000);
            AddPkg1Bytes(ref boot0, ref filesystemNormal);
            AddPkg1Bytes(ref boot0, ref filesystemNormal);

            AddPkg1Bytes(ref boot1, ref filesystemSafe);
            AddPkg1Bytes(ref boot1, ref filesystemSafe);

            Pad(ref bcpkg2_1, 0x4000);
            AddPkg2Bytes(ref bcpkg2_1, ref filesystemNormal, 0x4000);

            Pad(ref bcpkg2_3, 0x40000);
            AddPkg2Bytes(ref bcpkg2_3, ref filesystemSafe, 0x40000);

            //IFileSystem filesystemCode = ncaNormal.OpenFileSystem(NcaSectionType.Code, IntegrityCheckLevel.None);
            //filesystemCode.
            //holy fuck https://github.com/Thealexbarney/LibHac/blob/d08e6b060c913a8a08df4b546a3cb290dc07851f/src/LibHac/SwitchFs.cs#L142

            inNormal.Dispose();
            inSafe.Dispose();

            if (File.Exists("boot0.test"))
                File.Delete("boot0.test");

            if (File.Exists("boot1.test"))
                File.Delete("boot1.test");

            if (File.Exists("bcpkg2_1.test"))
                File.Delete("bcpkg2_1.test");

            if (File.Exists("bcpkg2_3.test"))
                File.Delete("bcpkg2_3.test");

            using (Stream file = File.OpenWrite("boot0.test"))
            {
                file.Write(boot0.ToArray(), 0, boot0.Count);
            }

            using (Stream file = File.OpenWrite("boot1.test"))
            {
                file.Write(boot1.ToArray(), 0, boot1.Count);
            }

            using (Stream file = File.OpenWrite("bcpkg2_1.test"))
            {
                file.Write(bcpkg2_1.ToArray(), 0, bcpkg2_1.Count);
            }

            using (Stream file = File.OpenWrite("bcpkg2_3.test"))
            {
                file.Write(bcpkg2_3.ToArray(), 0, bcpkg2_3.Count);
            }

            Dictionary<string, List<ncaList>> test = SortNca(ncalist);

            foreach(KeyValuePair<string, List<ncaList>> hi in test)
            {
                Console.WriteLine($"Key: {hi.Key}");

                foreach(ncaList hi2 in hi.Value)
                {
                    Console.WriteLine($"^- {hi2.filename} {hi2.type}");
                }

                using (IStorage tempncastorage = new LocalStorage($"9.1.0\\{hi.Value.Find(x => x.type == NcaContentType.Meta).filename}", FileAccess.Read))
                {
                    var tempNca = new Nca(keyset, tempncastorage);
                    using (IFileSystem tempncafilesystem = tempNca.OpenFileSystem(NcaSectionType.Data, IntegrityCheckLevel.ErrorOnInvalid))
                    {
                        foreach (var temp in tempncafilesystem.EnumerateEntries())
                        {
                            Console.WriteLine(temp.FullPath);
                        }
                    }
                }





                //tempncafilesystem.Dispose();
                //tempncastorage.Dispose();

            }

            */

            Console.ReadKey();
        }
    }
}
