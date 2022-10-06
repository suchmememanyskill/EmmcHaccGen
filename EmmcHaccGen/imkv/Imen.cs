using System;
using System.Collections.Generic;
using System.Linq;
using EmmcHaccGen.nca;
using EmmcHaccGen.cnmt;
using LibHac.Tools.Ncm;

namespace EmmcHaccGen.imkv
{
    class Imen
    {
        public List<byte> bytes;
        private List<byte> key, value;
        private List<NcaFile> pair;
        private Cnmt cnmt
        {
            get
            {
                return pair[0].cnmt;
            }
        }
        private CnmtRawParser cnmtRaw
        {
            get
            {
                return pair[0].cnmt_raw;
            }
        }

        public Imen() { }
        public Imen(List<NcaFile> pair)
        {
            bytes = new List<byte>();
            key = new List<byte>();
            value = new List<byte>();

            this.pair = pair;
            this.Gen();
        }

        public void Gen()
        {
            bytes.Add(0x49); // Spells out IMEN
            bytes.Add(0x4D);
            bytes.Add(0x45);
            bytes.Add(0x4E);

            GenKey();
            GenValue();

            bytes.AddRange(BitConverter.GetBytes((UInt32)key.Count));
            bytes.AddRange(BitConverter.GetBytes((UInt32)value.Count));
            bytes.AddRange(key);
            bytes.AddRange(value);

            if (Config.verbose)
                Console.WriteLine($"{pair[0].titleId}: {BitConverter.ToString(bytes.ToArray()).Replace("-", "")}".ToLower());
        }
        private void GenKey()
        {
            key.AddRange(BitConverter.GetBytes(cnmt.TitleId));
            key.AddRange(BitConverter.GetBytes(cnmt.TitleVersion.Version));
            key.Add((byte)cnmt.Type);

            key.Add(0);
            key.Add(0);
            key.Add(0);

            if (key.Count != 0x10)
                throw new ArgumentException("Imen key is invalid!");
        }

        private void GenValue()
        {
            value.AddRange(cnmtRaw.raw_ext_header_size);
            value.AddRange(BitConverter.GetBytes((UInt16)(cnmt.ContentEntryCount + 1)));
            value.AddRange(BitConverter.GetBytes((UInt16)cnmt.MetaEntryCount));
            value.Add(cnmtRaw.raw_content_meta_attribs);
            value.Add(0);

            if (cnmtRaw.ext_header_loaded_size > 0)
            {
                value.AddRange(BitConverter.GetBytes(cnmtRaw.ext_header_loaded_size));
            }

            AddMetaNca();
            if (cnmt.ContentEntryCount >= 1)
                AddContentNca();

            foreach(var metaEntry in cnmtRaw.meta)
            {
                value.AddRange(metaEntry.GetRawRecord());
            }

            if (cnmtRaw.ext_data != null)
            {
                value.AddRange(cnmtRaw.ext_data);
            }
        }
        private void AddMetaNca()
        {
            value.AddRange(pair[0].hash);
            value.AddRange(BitConverter.GetBytes((UInt16)pair[0].header.NcaSize));

            value.Add(0);
            value.Add(0);
            value.Add(0);
            value.Add(0);

            value.Add(0);
            value.Add(0);
        }
        private void AddContentNca()
        {
            if (!pair[0].cnmt.ContentEntries[0].Hash.Take<byte>(16).SequenceEqual(cnmtRaw.content[0].raw_content_id))
                Console.WriteLine("LibHac cnmt does not match selfparsed cnmt");

            if (!cnmtRaw.content[0].raw_content_id.SequenceEqual(pair[1].hash))
                throw new Exception("[IMEN] Invalid hash given");

            value.AddRange(cnmtRaw.content[0].GetRawRecord());
        }
    }
}
