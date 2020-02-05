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
    class ncaList
    {
        public string filename;
        public string titleid;
        public NcaContentType type;
        public Cnmt cnmt;
        public byte[] raw_cnmt;
        public long size;

        public ncaList()
        {
            filename = "";
            titleid = "";
            type = NcaContentType.Control;
            cnmt = null;
        }
    }

    class ncaVersionExtractor
    {
        public byte[] rawVersion;
        public string versionString, platformString;
        private Keyset keyset;

        public ncaVersionExtractor() { }
        public ncaVersionExtractor(string path, Keyset keyset)
        {
            this.keyset = keyset;
            this.OpenNca(path);
        }
        public void Parse()
        {
            platformString = System.Text.Encoding.UTF8.GetString(ReadFromOffset(0x20, 0x8)).Trim('\0').ToLower();
            versionString = System.Text.Encoding.UTF8.GetString(ReadFromOffset(0x18, 0x68)).Trim('\0').ToLower();
        }
        private byte[] ReadFromOffset(uint amount, uint offset)
        {
            byte[] temp = new byte[amount];
            for (int i = 0; i < amount; i++)
                temp[i] = rawVersion[i + offset];
            return temp;
        }
        private void OpenNca(string path)
        {
            using (IStorage versionFile = new LocalStorage(path, FileAccess.Read))
            {
                Nca versionNca = new Nca(keyset, versionFile);
                using (IFileSystem fs = versionNca.OpenFileSystem(NcaSectionType.Data, IntegrityCheckLevel.ErrorOnInvalid))
                {
                    fs.OpenFile(out IFile version, "file", OpenMode.Read).ThrowIfFailure();
                    version.GetSize(out long ReadSize);
                    rawVersion = new byte[ReadSize];
                    version.Read(out long WriteBytes, 0, rawVersion);
                }
            }
        }
    }

    class ncaBisExtractor
    {
        public string path;
        private Keyset keyset;
        public Byte[] bct, pkg1, pkg2;
        private IFileSystem fs;

        public ncaBisExtractor()
        {
            path = "";
        }

        public ncaBisExtractor(string target, Keyset keyset)
        {
            path = target;
            this.keyset = keyset;
        }

        public void Extract()
        {
            using (IStorage inFile = new LocalStorage(path, FileAccess.Read))
            {
                Nca nca = new Nca(keyset, inFile);
                using (fs = nca.OpenFileSystem(NcaSectionType.Data, IntegrityCheckLevel.ErrorOnInvalid))
                {
                    bct = this.ReadFile("/nx/bct");
                    bct[0x210] = 0x77;
                    pkg1 = this.ReadFile("/nx/package1");
                    pkg2 = this.ReadFile("/nx/package2");
                }
            }
        }

        private Byte[] ReadFile(string path)
        {
            byte[] tempByte;
            long readlength, writelength;
            IFile file;

            fs.OpenFile(out file, path, OpenMode.Read);
            file.GetSize(out readlength);
            tempByte = new byte[readlength];
            file.Read(out writelength, 0, tempByte, ReadOption.None);
            file.Dispose();

            if (readlength != writelength)
                throw new ArgumentException("Read is not the same as Write!");

            return tempByte;
        }
    }
}
