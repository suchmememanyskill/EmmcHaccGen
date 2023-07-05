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
        // TODO: Turn to read-only properties
        public BisExtractor normal, safe;
        public ByteHolder boot0, boot1, bcpkg2_1, bcpkg2_3;
        public NcaIndexer Indexer { get; }
        public bool Exfat { get; }
        public bool AutoRcm { get; }
        public bool Mariko { get; }
        
        public BisAssembler(NcaIndexer files, bool exfat, bool autorcm, bool mariko)
        {
            Indexer = files;
            Exfat = exfat;
            AutoRcm = autorcm;
            Mariko = mariko;
            boot0 = new ByteHolder(0x180000);
            boot1 = new ByteHolder(0x80000);
            bcpkg2_1 = new ByteHolder(0x800000);
            bcpkg2_3 = new ByteHolder(0x800000);
            
            Assemble();
        }

        public void Save(string folder)
        {
            boot0.DumpToFile($"{folder}/BOOT0.bin");
            boot1.DumpToFile($"{folder}/BOOT1.bin");
            bcpkg2_1.DumpToFile($"{folder}/BCPKG2-1-Normal-Main.bin");
            bcpkg2_1.DumpToFile($"{folder}/BCPKG2-2-Normal-Sub.bin");
            bcpkg2_3.DumpToFile($"{folder}/BCPKG2-3-SafeMode-Main.bin");
            bcpkg2_3.DumpToFile($"{folder}/BCPKG2-4-SafeMode-Sub.bin");
        }
        
        private void Assemble()
        {
            normal = new BisExtractor(Indexer.FindNca((Exfat) ? "010000000000081B" : "0100000000000819", NcaContentType.Data)!, AutoRcm, Mariko);
            safe = new BisExtractor(Indexer.FindNca((Exfat) ? "010000000000081C" : "010000000000081A", NcaContentType.Data)!, AutoRcm, Mariko);

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
            
            boot1.Write(safe.pkg1);
            boot1.Pad(0x40000 - safe.pkg1.Length);
            boot1.Write(safe.pkg1);
            boot1.Pad(0x40000 - safe.pkg1.Length);
            
            bcpkg2_1.Pad(0x4000);
            bcpkg2_1.Write(normal.pkg2);

            bcpkg2_3.Pad(0x4000);
            bcpkg2_3.Write(safe.pkg2);
        }
    }
}
