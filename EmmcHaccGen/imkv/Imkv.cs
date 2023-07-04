using System;
using System.Collections.Generic;
using System.Text;
using System.IO;
using System.Linq;
using EmmcHaccGen.nca;

namespace EmmcHaccGen.imkv
{
    class Imkv
    {
        private NcaIndexer _indexer;
        private List<string> _skipTitles;
        public byte[] Result { get; private set; }
        
        public Imkv(NcaIndexer ncaIndex, List<string>? skipTitles = null)
        {
            _indexer = ncaIndex;
            _skipTitles = skipTitles ?? new();
            Result = Build();
        }
        
        public byte[] Build()
        {
            List<byte> bytes = new();
            
            bytes.Add(0x49); // Spells out IMKV
            bytes.Add(0x4D);
            bytes.Add(0x4B);
            bytes.Add(0x56);

            bytes.Add(0x0); // Padding
            bytes.Add(0x0);
            bytes.Add(0x0);
            bytes.Add(0x0);

            List<Imen> imens = new();
            
            foreach(var entry in _indexer.SortedFiles.Where(x => !_skipTitles.Contains(x.Key)))
            {
                imens.Add(new Imen(entry.Value));
            }
            
            bytes.AddRange(BitConverter.GetBytes((uint)imens.Count));
            foreach (var single in imens)
            {
                bytes.AddRange(single.bytes);
            }

            return bytes.ToArray();
        }

        public void DumpToFile(string path)
        {
            if (File.Exists(path))
                File.Delete(path);

            using (Stream file = File.OpenWrite(path))
            {
                file.Write(Result.ToArray(), 0, Result.Length);
            }

            Console.WriteLine($"Wrote 0x{Result.Length:x8} bytes to {path}");
        }
    }
}
