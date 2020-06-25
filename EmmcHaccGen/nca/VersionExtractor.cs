using LibHac.Fs;
using System;
using System.Collections.Generic;
using System.Text;
using LibHac.FsSystem.NcaUtils;
using LibHac.FsSystem;
using LibHac.Common;

namespace EmmcHaccGen.nca
{
    class VersionExtractor
    {
        private byte[] rawVersion;
        public string platform, version;

        public VersionExtractor() { }
        public VersionExtractor(NcaFile nca)
        {
            try
            {
                this.OpenVersionNca(nca);
                this.Parse();
            }
            catch
            {
                platform = "NX";
                version = "Unkn";
                Console.WriteLine("[Warning] Version extraction failed. Version is unknown");
            }
        }
        private void OpenVersionNca(NcaFile nca)
        {
            using IFileSystem fs = nca.data.OpenFileSystem(NcaSectionType.Data, IntegrityCheckLevel.ErrorOnInvalid);

            fs.OpenFile(out IFile version, new U8Span("/file"), OpenMode.Read).ThrowIfFailure();
            version.GetSize(out long readSize);
            rawVersion = new byte[readSize];
            version.Read(out long temp, 0, rawVersion);
        }
        private void Parse()
        {
            platform = Encoding.UTF8.GetString(ReadFromOffset(0x20, 0x8)).Trim('\0').ToLower();
            version = Encoding.UTF8.GetString(ReadFromOffset(0x18, 0x68)).Trim('\0').ToLower();
        }
        private byte[] ReadFromOffset(uint amount, uint offset)
        {
            byte[] temp = new byte[amount];
            for (int i = 0; i < amount; i++)
                temp[i] = rawVersion[i + offset];
            return temp;
        }
    }
}
