using System;
using System.Collections.Generic;
using System.Text;
using EmmcHaccGen.nca;
using LibHac.FsSystem;
using LibHac.Tools.FsSystem.NcaUtils;

namespace EmmcHaccGen.bis
{
    class BisAssembler
    {
        public BisExtractor normal, safe;
        public ByteHolder boot0, boot1, bcpkg2_1, bcpkg2_3;
        private string destFolder;

        public BisAssembler() { }
        public BisAssembler(NcaIndexer files, string dest, bool exfat, bool autorcm, bool mariko)
        {
            boot0 = new ByteHolder(0x180000);
            boot1 = new ByteHolder(0x80000);
            bcpkg2_1 = new ByteHolder(0x800000);
            bcpkg2_3 = new ByteHolder(0x800000);

            destFolder = dest;
            Assemble(files, exfat, autorcm, mariko);
        }
        private void Assemble(NcaIndexer files, bool exfat, bool autorcm, bool mariko)
        {
            normal = new BisExtractor(files.FindNca((exfat) ? "010000000000081B" : "0100000000000819", NcaContentType.Data)!, autorcm, mariko);
            safe = new BisExtractor(files.FindNca((exfat) ? "010000000000081C" : "010000000000081A", NcaContentType.Data)!, autorcm, mariko);

            boot0.Write(normal.bct);
            boot0.Pad(0x4000 - normal.bct.Length);
            boot0.Write(safe.bct);
            boot0.Pad(0x4000 - safe.bct.Length);
            boot0.Write(normal.bct);
            boot0.Pad(0x4000 - normal.bct.Length);
            boot0.Write(safe.bct);
            boot0.Pad(0x4000 - safe.bct.Length);
            boot0.Pad(0xF0000);
            boot0.Write(normal.pkg1);
            boot0.Pad(0x40000 - normal.pkg1.Length);
            boot0.Write(normal.pkg1);
            boot0.Pad(0x40000 - normal.pkg1.Length);
            boot0.DumpToFile($"{destFolder}/BOOT0.bin");

            boot1.Write(safe.pkg1);
            boot1.Pad(0x40000 - safe.pkg1.Length);
            boot1.Write(safe.pkg1);
            boot1.Pad(0x40000 - safe.pkg1.Length);
            boot1.DumpToFile($"{destFolder}/BOOT1.bin");

            bcpkg2_1.Pad(0x4000);
            bcpkg2_1.Write(normal.pkg2);
            bcpkg2_1.DumpToFile($"{destFolder}/BCPKG2-1-Normal-Main.bin");
            bcpkg2_1.DumpToFile($"{destFolder}/BCPKG2-2-Normal-Sub.bin");

            bcpkg2_3.Pad(0x4000);
            bcpkg2_3.Write(safe.pkg2);
            bcpkg2_3.DumpToFile($"{destFolder}/BCPKG2-3-SafeMode-Main.bin");
            bcpkg2_3.DumpToFile($"{destFolder}/BCPKG2-4-SafeMode-Sub.bin");
        }
    }
}
