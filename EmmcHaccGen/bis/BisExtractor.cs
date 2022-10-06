using EmmcHaccGen.nca;
using LibHac.Fs;
using LibHac.Common;
using LibHac.FsSystem;
using LibHac.Fs.Fsa;
using LibHac.Tools.FsSystem;

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
            string startPath = (Config.marikoBoot) ? "/a/" : "/nx/";
            bct = this.ReadFile(startPath + "bct", ref fs);

            if (!Config.noAutoRcm)
                bct[0x210] = 0x77;

            pkg1 = this.ReadFile(startPath + "package1", ref fs);
            pkg2 = this.ReadFile("/nx/package2", ref fs);

            fs.Dispose();
        }
        private byte[] ReadFile(string path, ref IFileSystem fs)
        {
            byte[] tempByte;
            UniqueRef<IFile> file = new();

            fs.OpenFile(ref file, new U8Span(path), OpenMode.Read);
            file.Get.GetSize(out long size);
            tempByte = new byte[size];
            file.Get.Read(out long read, 0, tempByte);
            file.Get.Dispose();

            return tempByte;
        }
    }
}
