using System;
using System.Collections.Generic;
using System.Text;
using EmmcHaccGen.nca;

namespace EmmcHaccGen.bis
{
    class BisFileAssembler
    {
        private BisAssembler _assembler;

        public BisFileAssembler(BisAssembler bisAssembler)
        {
            _assembler = bisAssembler;
        }

        public void Save(string path)
        {
            ByteHolder bytes = new(0x1200025);
            ByteHolder header = new(0x25);
            
            byte[] text = new byte[0x10] { 0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0 };
            Encoding.UTF8.GetBytes($"{_assembler.Indexer.Version}{((_assembler.Exfat) ? "_exFAT" : "")}").CopyTo(text, 0);
            header.Write(text);

            byte[] args = new byte[1];
            args[0] = 0xF0;

            if (_assembler.Exfat)
                args[0] += 0x01;

            header.Write(args);

            uint[] starts = new uint[4];

            starts[0] = (uint)_assembler.boot0.bytes.Count;
            starts[1] = (uint)_assembler.boot1.bytes.Count;
            starts[2] = (uint)_assembler.bcpkg2_1.bytes.Count;
            starts[3] = (uint)_assembler.bcpkg2_3.bytes.Count;

            foreach(uint i in starts)
            {
                header.Write(ConvertInt(i));
            }

            bytes.Write(header.bytes.ToArray());

            bytes.Write(_assembler.boot0.bytes.ToArray());
            bytes.Write(_assembler.boot1.bytes.ToArray());
            bytes.Write(_assembler.bcpkg2_1.bytes.ToArray());
            bytes.Write(_assembler.bcpkg2_3.bytes.ToArray());

            bytes.DumpToFile(path);
        }
        private byte[] ConvertInt(uint data)
        {
            byte[] convert = new byte[4];

            for (int i = 0; i < 4; i++)
            {
                convert[i] = (byte)(data >> ((3 - i) * 8));
            }

            return convert;
        }
    }
}
