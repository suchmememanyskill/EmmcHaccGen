using System;
using System.Collections.Generic;
using System.Text;
using System.IO;
using System.Linq;

namespace EmmcHaccGen
{
    class bis
    {
        public List<Byte> bytes;
        public long remainingLength;

        public bis()
        {
            bytes = new List<byte>();
        }

        public bis(long remainingLength)
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
        public void Write(Byte[] inbytes)
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

            Console.WriteLine($"Wrote 0x{bytes.Count:x8} to {path}");
        }
    }
}
