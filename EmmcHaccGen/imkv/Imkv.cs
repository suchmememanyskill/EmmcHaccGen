using System;
using System.Collections.Generic;
using System.Text;
using System.IO;
using EmmcHaccGen.nca;

namespace EmmcHaccGen.imkv
{
    class Imkv
    {
        public List<byte> bytes;
        public List<Imen> imenlist;

        public Imkv() { }

        public Imkv(ref NcaIndexer ncaIndex)
        {
            bytes = new List<byte>();

            this.MakeImenList(ref ncaIndex);
            this.Build();
        }
        public void MakeImenList(ref NcaIndexer ncaIndex)
        {
            imenlist = new List<Imen>();

            foreach(var entry in ncaIndex.sortedNcaDict)
            {
                imenlist.Add(new Imen(entry.Value));
            }
        }
        public void Build()
        {
            bytes.Add(0x49); // Spells out IMKV
            bytes.Add(0x4D);
            bytes.Add(0x4B);
            bytes.Add(0x56);

            bytes.Add(0x0); // Padding
            bytes.Add(0x0);
            bytes.Add(0x0);
            bytes.Add(0x0);

            bytes.AddRange(BitConverter.GetBytes((uint)imenlist.Count));
            foreach (var single in imenlist)
            {
                bytes.AddRange(single.bytes);
            }
        }

        public void DumpToFile(string path)
        {
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
