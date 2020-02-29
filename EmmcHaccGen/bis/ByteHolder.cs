using System;
using System.Collections.Generic;
using System.Text;
using System.Linq;
using System.IO;

namespace EmmcHaccGen.bis
{
    class ByteHolder
    {
        public List<Byte> bytes;
        private long remainingLength;

        public ByteHolder() { }

        public ByteHolder(long remainingLength)
        {
            this.remainingLength = remainingLength;
            bytes = new List<byte>();
        }

        public void Pad(long padamount)
        {
            for (int i = 0; i < padamount; i++)
                bytes.Add(0);

            remainingLength -= padamount;
        }
        public void Write(byte[] inbytes)
        {
            bytes.AddRange(inbytes.ToList());
            remainingLength -= inbytes.Length;
        }
        public void DumpToFile(string path)
        {
            this.Pad(remainingLength);

            if (File.Exists(path))
                File.Delete(path);

            using (Stream file = File.OpenWrite(path))
            {
                file.Write(bytes.ToArray(), 0, bytes.Count);
            }

            Console.WriteLine($"Wrote 0x{bytes.Count:x8} bytes to {path}");
        }
    }
}
