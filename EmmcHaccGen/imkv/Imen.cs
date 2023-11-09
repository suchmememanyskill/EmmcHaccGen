using System;
using System.Collections.Generic;
using System.Linq;
using EmmcHaccGen.nca;
using EmmcHaccGen.cnmt;
using LibHac.Tools.Ncm;
using LibHac.Ncm;

namespace EmmcHaccGen.imkv
{
    class SaveDataAttribute {
        ProgramId saveid;

        public SaveDataAttribute(ProgramId saveid) {
            this.saveid = saveid;
        }

        public List<byte> ToBytes() {
            byte[] ret = new byte[0x40];

            Buffer.BlockCopy(BitConverter.GetBytes(saveid.Value), 0, ret, 0x18, 8);

            return ret.ToList();
        }
    }

    class SaveImenValue {
        ProgramId saveid;
        UInt64 save_size;

        public SaveImenValue(ProgramId saveid, UInt64 save_size) {
            this.saveid = saveid;
            this.save_size = save_size;
        }

        public List<byte> ToBytes() {
            byte[] ret = new byte[0x40];

            Buffer.BlockCopy(BitConverter.GetBytes(saveid.Value), 0, ret, 0, 8);
            Buffer.BlockCopy(BitConverter.GetBytes(save_size), 0, ret, 0x8, 8);

            return ret.ToList();
        }
    }

    class Imen
    {
        // TODO: Turn to read-only properties
        public List<byte> bytes;
        private List<byte> key, value;
        private List<NcaFile> pair;
        private Cnmt cnmt
        {
            get
            {
                return pair[0].Cnmt;
            }
        }
        private CnmtRawParser cnmtRaw
        {
            get
            {
                return pair[0].CnmtRaw;
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
        public Imen(ProgramId saveid, UInt64 save_size) {
            this.key = new SaveDataAttribute(saveid).ToBytes();
            this.value = new SaveImenValue(saveid, save_size).ToBytes();
            bytes = new List<byte>();
            pair = new List<NcaFile>();
            this.Gen();
        }

        public void Gen()
        {
            bytes.Add(0x49); // Spells out IMEN
            bytes.Add(0x4D);
            bytes.Add(0x45);
            bytes.Add(0x4E);

            if (key.Count() == 0 || value.Count() == 0) {
                GenKey();
                GenValue();
            }

            bytes.AddRange(BitConverter.GetBytes((UInt32)key.Count));
            bytes.AddRange(BitConverter.GetBytes((UInt32)value.Count));
            bytes.AddRange(key);
            bytes.AddRange(value);
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
            value.AddRange(pair[0].Hash);
            value.AddRange(BitConverter.GetBytes((UInt16)pair[0].Header.NcaSize));

            value.Add(0);
            value.Add(0);
            value.Add(0);
            value.Add(0);

            value.Add(0);
            value.Add(0);
        }
        private void AddContentNca()
        {
            if (!pair[0].Cnmt.ContentEntries[0].Hash.Take<byte>(16).SequenceEqual(cnmtRaw.content[0].raw_content_id))
                Console.WriteLine("LibHac cnmt does not match selfparsed cnmt");

            if (!cnmtRaw.content[0].raw_content_id.SequenceEqual(pair[1].Hash))
                throw new Exception("[IMEN] Invalid hash given");

            value.AddRange(cnmtRaw.content[0].GetRawRecord());
        }
    }
}
