using System;
using System.Collections.Generic;
using System.Text;

namespace EmmcHaccGen.bis
{
    class BisFileAssembler
    {
        public ByteHolder bytes, header;

        public BisFileAssembler()
        { }

        public BisFileAssembler(string version, ref BisAssembler bisAssembler, string path)
        {
            bytes = new ByteHolder(0x1200025);
            header = new ByteHolder(0x25);

            this.Assemble(version, ref bisAssembler, path);
        }

        public void Assemble(string version, ref BisAssembler bisAssembler, string path)
        {
            byte[] text = new byte[0x10] { 0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0 };
            Encoding.UTF8.GetBytes(version).CopyTo(text, 0);
            header.Write(text);

            byte[] args = new byte[1];
            args[0] = 0xF0;

            if (!Config.noExfat)
                args[0] += 0x01;

            header.Write(args);

            uint[] starts = new uint[4];

            starts[0] = (uint)bisAssembler.boot0.bytes.Count;
            starts[1] = (uint)bisAssembler.boot1.bytes.Count;
            starts[2] = (uint)bisAssembler.bcpkg2_1.bytes.Count;
            starts[3] = (uint)bisAssembler.bcpkg2_3.bytes.Count;

            foreach(uint i in starts)
            {
                header.Write(ConvertInt(i));
            }

            bytes.Write(header.bytes.ToArray());

            bytes.Write(bisAssembler.boot0.bytes.ToArray());
            bytes.Write(bisAssembler.boot1.bytes.ToArray());
            bytes.Write(bisAssembler.bcpkg2_1.bytes.ToArray());
            bytes.Write(bisAssembler.bcpkg2_3.bytes.ToArray());

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
