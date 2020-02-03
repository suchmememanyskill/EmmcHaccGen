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

        public ncaList()
        {
            filename = "";
            titleid = "";
            type = NcaContentType.Control;
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
