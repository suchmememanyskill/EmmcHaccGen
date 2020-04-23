using System;
using System.Collections.Generic;
using System.Text;
using LibHac;
using LibHac.Common;
using LibHac.FsSystem.NcaUtils;
using LibHac.Fs;
using LibHac.FsSystem;
using System.IO;
using System.Linq;
using LibHac.FsSystem.Save;
using EmmcHaccGen.cnmt;

namespace EmmcHaccGen.nca
{
    class NcaFile
    {
        public string filename;
        public byte[] hash;
        public string titleId;
        public Nca data;
        public NcaHeader header { 
            get
            {
                return data.Header;
            }
        }
        public Cnmt cnmt;
        public CnmtRawParser cnmt_raw;
        private IStorage infile;

        public void AddNcaInfo(string path)
        {
            if (Config.verbose)
                Console.WriteLine($"Parsing File:     {path}");

            infile = new LocalStorage(path, FileAccess.Read);

            try
            {
                data = new Nca(Config.keyset, infile);
            }
            catch (Exception e)
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine("Unable to create NCA class. Is your keyset file valid?");
                Console.WriteLine($"Error: {e.Message}");
                Console.ResetColor();
                Environment.Exit(0);
            }

            if (header.ContentType == NcaContentType.Meta)
            {
                using IFileSystem fs = data.OpenFileSystem(NcaSectionType.Data, IntegrityCheckLevel.ErrorOnInvalid);
                string cnmtPath = fs.EnumerateEntries("/", "*.cnmt").Single().FullPath;

                fs.OpenFile(out IFile cnmtFile, new U8Span(cnmtPath), OpenMode.Read).ThrowIfFailure();
                cnmt = new Cnmt(cnmtFile.AsStream());

                cnmtFile.GetSize(out long size);
                hash = new byte[size];
                cnmtFile.Read(out long BytesRead, 0, hash);

                cnmt_raw = new CnmtRawParser(hash);
            }

            titleId = header.TitleId.ToString("x16").ToUpper();
        }
        public void GetHash()
        {
            string toSplit = filename.Substring(0, filename.Length - 4);

            if (toSplit.Contains(".cnmt"))
                toSplit = toSplit.Substring(0, toSplit.Length - 5);

            if (Config.verbose)
                Console.WriteLine($"Parsing FileName: {toSplit}");

            List<byte> bytehash = new List<byte>();

            for (int i = 0; i < toSplit.Length; i += 2)
                bytehash.Add(Convert.ToByte(toSplit.Substring(i, 2), 16));

            hash = bytehash.ToArray();
        }
    }
}
