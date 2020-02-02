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
            }

            return entry;
        }
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
        static void Main(string[] args)
        {
            Console.WriteLine("Hello World!");
            Program yeet = new Program();
            yeet.Start();
        }
        void Start()
        {
            List<ncaList> ncalist = new List<ncaList>();
            keyset = ExternalKeyReader.ReadKeyFile("prod.keys");
            List<Byte> boot0 = new List<byte>();
            List<Byte> boot1 = new List<byte>();
            List<Byte> bcpkg2_1 = new List<byte>();
            List<Byte> bcpkg2_3 = new List<byte>();

            foreach (var file in Directory.EnumerateFiles("9.1.0", "*.nca"))
            {
                //Console.WriteLine(file.Substring("9.1.0".Length + 1));
                ncalist.Add(parseNca(file.Substring("9.1.0".Length + 1), file.ToString()));
            }

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

            Console.ReadKey();
        }
    }
}
