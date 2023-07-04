using System;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using LibHac.Common;
using LibHac.Fs;
using LibHac.Fs.Fsa;
using LibHac.FsSystem;
using LibHac.Tools.Ncm;
using LibHac.Tools.FsSystem;
using LibHac.Tools.FsSystem.NcaUtils;
using EmmcHaccGen.cnmt;
using LibHac.Common.Keys;
using NcaHeader = LibHac.Tools.FsSystem.NcaUtils.NcaHeader;

namespace EmmcHaccGen.nca
{
    public class NcaFile
    {
        public string FileName { get; private set; }
        public string Path { get; private set; }
        public byte[] Hash { get; private set; }
        public string TitleId { get; private set; }
        public Nca Data { get; private set; }
        public NcaHeader Header { 
            get
            {
                return Data.Header;
            }
        }
        public Cnmt Cnmt { get; private set; }
        public CnmtRawParser CnmtRaw { get; private set; }
        private IStorage _stream;
        private KeySet _keySet;

        public NcaFile(KeySet keySet, string path, bool fixHash = false)
        {
            FileName =  path.Split('/', '\\').Last();
            Path = path;
            _keySet = keySet;

            VerifyHash(fixHash);
            AddNcaInfo();
        }

        private void AddNcaInfo()
        {
            _stream = new LocalStorage(Path, FileAccess.Read);

            try
            {
                Data = new Nca(_keySet, _stream);
            }
            catch (Exception e)
            {
                throw new Exception($"Unable to create NCA class. Is your keyset file valid?\r\n{e.Message}");
            }

            if (Header.ContentType == NcaContentType.Meta)
            {
                using IFileSystem fs = Data.OpenFileSystem(NcaSectionType.Data, IntegrityCheckLevel.ErrorOnInvalid);
                string cnmtPath = fs.EnumerateEntries("/", "*.cnmt").Single().FullPath;
                UniqueRef<IFile> cnmtFile = new();

                fs.OpenFile(ref cnmtFile, new U8Span(cnmtPath), OpenMode.Read).ThrowIfFailure();
                Cnmt = new Cnmt(cnmtFile.Get.AsStream());

                cnmtFile.Get.GetSize(out long size);
                byte[] cnmtRaw = new byte[size];
                cnmtFile.Get.Read(out long none, 0, cnmtRaw);

                CnmtRaw = new CnmtRawParser(cnmtRaw);
            }

            TitleId = Header.TitleId.ToString("x16").ToUpper();
        }
        private void VerifyHash(bool fixHash = false)
        {
            string fileNameHash = FileName.Substring(0, 32);
            string fileHash;

            using (SHA256 hasher = SHA256.Create())
            {
                FileStream fileStream = File.OpenRead(Path);
                fileStream.Position = 0;

                Hash = hasher.ComputeHash(fileStream).Take(16).ToArray();
                fileStream.Close();
            }

            fileHash = BitConverter.ToString(Hash).Replace("-", "").ToLower();

            if (fileHash != fileNameHash)
            {
                Console.Write($"Incorrect hash for file {FileName}.");

                if (fixHash)
                {
                    Console.WriteLine($"Renaming to {fileHash}.nca");
                    int indexOfLastSlash = Path.LastIndexOfAny(new char[] { '\\', '/' });
                    FileName = $"{fileHash}.nca";
                    string newPath = $"{Path.Substring(0, indexOfLastSlash + 1)}{FileName}";
                    File.Move(Path, newPath);
                    Path = newPath;
                }
                else
                {
                    throw new Exception("This firmware dump is fishy. Stopping execution. Use the commandline argument \'--fix-hashes\' to fix the filenames");
                }
            }
        }
    }
}
