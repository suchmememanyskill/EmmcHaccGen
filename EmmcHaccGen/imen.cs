using System;
using System.Collections.Generic;
using LibHac;
using LibHac.FsSystem.NcaUtils;
using LibHac.Fs;
using LibHac.FsSystem;
using System.IO;
using System.Linq;
using LibHac.Ncm;

namespace EmmcHaccGen
{
    class imen
    {
        public List<Byte> bytes;
        public List<Byte> key, value;
        //private Cnmt title;
        private Keyset keyset;
        //private byte[] cnmt_raw;
        private List<ncaList> pair;
        private int offset = 0x20;

        public imen() { }

        public imen(Keyset keyset, List<ncaList> pair)
        {
            bytes = new List<Byte>();
            key = new List<Byte>();
            value = new List<Byte>();
            this.keyset = keyset;
            this.pair = pair;
        }

        public void Gen()
        {
            GenKey();
            GenValue();
            Console.WriteLine($"Titleid: {pair[0].titleid}");
            Console.WriteLine($"{BitConverter.ToString(key.ToArray()).Replace("-", "")}");
            Console.WriteLine($"{BitConverter.ToString(value.ToArray()).Replace("-", "").ToLower()}");
            //'0000020000000000dab17bb26feb93ad1523d55a7b59f6e9000e00000000000000ae6772e51a6a19e9d0a45a2ca2e8450068110000000100'

            /*
            Structure of to_content_info()
            self.raw.content_id == filename without .nca (hash)
            self.raw.size == nca.header.size

            */
        }
        private void GenKey()
        {
            key.AddRange(BitConverter.GetBytes(pair[0].cnmt.TitleId).ToList());
            key.AddRange(BitConverter.GetBytes(pair[0].cnmt.TitleVersion.Version).ToList());
            key.Add((Byte)pair[0].cnmt.Type);
            key.Add(0);
            key.Add(0);
            key.Add(0);

            if (key.Count != 0x10)
                throw new ArgumentException("Imen key is invalid!");    
        }
        private void GenValue()
        {
            
            UInt16 raw_ext_header_size = BitConverter.ToUInt16(pair[0].raw_cnmt, 0xE);
            value.AddRange(BitConverter.GetBytes(raw_ext_header_size));
            value.AddRange(BitConverter.GetBytes((UInt16)(pair[0].cnmt.ContentEntryCount + 1)));
            value.AddRange(BitConverter.GetBytes((UInt16)pair[0].cnmt.MetaEntryCount));
            value.Add(pair[0].raw_cnmt[0x14]);
            value.Add(0);

            if (raw_ext_header_size > 0)
            {
                byte[] ext_header_size = new byte[raw_ext_header_size];
                for (offset = 0x20; offset < raw_ext_header_size + 0x20; offset++)
                    ext_header_size[offset - 0x20] = pair[0].raw_cnmt[offset];


                value.AddRange(ext_header_size.ToList());
            }

            AddContentValue(0);
            AddContentValue(1);


            // Add Content info
            // Add content_meta info

            //title.ContentEntries[0].


        }
        private void AddContentValue(int number)
        {
            value.AddRange(Enumerable.Range(0, pair[number].filename.Length - 4).Where(x => x % 2 == 0).Select(x => Convert.ToByte(pair[number].filename.Substring(x, 2), 16)));
            if (number == 0)
            {
                //value.AddRange(BitConverter.GetBytes(0x000e00000000));

                value.Add(0);
                value.Add(0xe);
                value.Add(0);
                value.Add(0);
                value.Add(0);
                value.Add(0);

                value.Add(0);
                value.Add(0);
            }
            else
            {
                //todo: cnmt needs rawparser
                value.AddRange(BitConverter.GetBytes(BitConverter.ToUInt16(pair[0].raw_cnmt, offset + 0x30)));
                value.AddRange(BitConverter.GetBytes(BitConverter.ToUInt16(pair[0].raw_cnmt, offset + 0x32)));
                value.AddRange(BitConverter.GetBytes(BitConverter.ToUInt16(pair[0].raw_cnmt, offset + 0x34)));

                value.Add((Byte)pair[0].cnmt.ContentEntries[0].Type);
                value.Add(0);
            }
        }
    }
}
