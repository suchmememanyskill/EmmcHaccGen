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
using System.Security.Cryptography;
using LibHac.Fs.Fsa;

namespace EmmcHaccGen.nca
{
    class NcaFile
    {
        public string filename;
        public string path;
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

        public void AddNcaInfo()
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
                byte[] cnmtRaw = new byte[size];
                cnmtFile.Read(out long none, 0, cnmtRaw);

                cnmt_raw = new CnmtRawParser(cnmtRaw);
            }

            titleId = header.TitleId.ToString("x16").ToUpper();
        }
        public void GenHash()
        {
            string fileNameHash = filename.Substring(0, 32);
            string fileHash;

            if (Config.verbose)
                Console.Write($"Hashing File:     {filename}... ");

            using (SHA256 hasher = SHA256.Create())
            {
                FileStream fileStream = File.OpenRead(path);
                fileStream.Position = 0;

                hash = hasher.ComputeHash(fileStream).Take(16).ToArray();
                fileStream.Close();
            }

            fileHash = BitConverter.ToString(hash).Replace("-", "").ToLower();

            if (fileHash != fileNameHash)
            {
                if (Config.verbose)
                    Console.WriteLine("FAIL!");

                Console.Write($"Incorrect hash for file {filename}. ");

                if (Config.fixHashes)
                {
                    Console.WriteLine($"Renaming to {fileHash}.nca");
                    int indexOfLastSlash = path.LastIndexOfAny(new char[] { '\\', '/' });
                    filename = $"{fileHash}.nca";
                    string newPath = $"{path.Substring(0, indexOfLastSlash + 1)}{filename}";
                    File.Move(path, newPath);
                    path = newPath;
                }
                else
                {
                    Console.WriteLine("This firmware dump is fishy. Stopping execution. Use the commandline argument \'--fix-hashes\' to fix the filenames");
                    Environment.Exit(1);
                }
            }
            else
            {
                if (Config.verbose)
                    Console.WriteLine("OK!");
            }
        }
    }
}
