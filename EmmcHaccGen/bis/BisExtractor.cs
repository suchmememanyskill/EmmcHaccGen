using System;
using System.Collections.Generic;
using System.Text;
using EmmcHaccGen.nca;
using LibHac.Fs;
using LibHac.Common;
using LibHac.FsSystem;
using LibHac.FsSystem.NcaUtils;

namespace EmmcHaccGen.bis
{
    class BisExtractor
    {
        public byte[] bct, pkg1, pkg2;

        public BisExtractor() { }
        public BisExtractor(NcaFile nca)
        {
            this.Extract(nca);
        }

        private void Extract(NcaFile nca)
        {
            IFileSystem fs = nca.data.OpenFileSystem(NcaSectionType.Data, IntegrityCheckLevel.ErrorOnInvalid);

            bct = this.ReadFile("/nx/bct", ref fs);
            bct[0x210] = 0x77;
            pkg1 = this.ReadFile("/nx/package1", ref fs);
            pkg2 = this.ReadFile("/nx/package2", ref fs);

            fs.Dispose();
        }
        private byte[] ReadFile(string path, ref IFileSystem fs)
        {
            byte[] tempByte;

            fs.OpenFile(out IFile file, new U8Span(path), OpenMode.Read);
            file.GetSize(out long size);
            tempByte = new byte[size];
            file.Read(out long read, 0, tempByte);
            file.Dispose();

            return tempByte;
        }
    }
}
