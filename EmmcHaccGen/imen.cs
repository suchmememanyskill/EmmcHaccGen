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
    class imkv
    {
        public List<imen> imenlist;
        public List<byte> bytes;

        public imkv() { }

        public imkv(List<imen> imenlist)
        {
            this.imenlist = imenlist;
            bytes = new List<byte>();
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

            Console.WriteLine($"Wrote 0x{bytes.Count:x8} to {path}");
        }
    }
    class imen
    {
        public List<Byte> bytes;
        public List<Byte> key, value;
        private List<ncaList> pair;
        private CnmtRawParser cnmtRawParser;


        public imen() { }

        public imen(List<ncaList> pair)
        {
            bytes = new List<Byte>();
            key = new List<Byte>();
            value = new List<Byte>();
            this.pair = pair;
        }

        public void Gen()
        {
            cnmtRawParser = new CnmtRawParser(pair[0].raw_cnmt);
            cnmtRawParser.Parse();

            GenKey();
            GenValue();

            bytes.Add(0x49); // Spells out IMEN
            bytes.Add(0x4D);
            bytes.Add(0x45);
            bytes.Add(0x4E);

            bytes.AddRange(BitConverter.GetBytes((UInt32)key.Count));
            bytes.AddRange(BitConverter.GetBytes((UInt32)value.Count));
            bytes.AddRange(key);
            bytes.AddRange(value);

            //Console.WriteLine($"{pair[0].titleid.ToLower()}: {BitConverter.ToString(bytes.ToArray()).Replace("-", "").ToLower()}");
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
            value.AddRange(cnmtRawParser.raw_ext_header_size);
            value.AddRange(BitConverter.GetBytes((UInt16)(cnmtRawParser.content_count + 1)));
            value.AddRange(BitConverter.GetBytes((UInt16)(cnmtRawParser.content_meta_count)));
            value.Add(cnmtRawParser.raw_content_meta_attribs);
            value.Add(0);

            if (cnmtRawParser.ext_header_loaded_size > 0)
            {
                value.AddRange(BitConverter.GetBytes(cnmtRawParser.ext_header_loaded_size));
            }

            AddContentValue(0);
            if (cnmtRawParser.content_count >= 1)
                AddContentValue(1);

            for (int i = 0; i < cnmtRawParser.content_meta_count; i++)
            {
                value.AddRange(cnmtRawParser.meta[i].GetRawRecord());
            }
            
            if (cnmtRawParser.ext_data != null)
            {
                value.AddRange(cnmtRawParser.ext_data);
            }
        }
        private void AddContentValue(int number)
        {
            
            if (number == 0)
            {
                value.AddRange(Enumerable.Range(0, pair[number].filename.Length - 4).Where(x => x % 2 == 0).Select(x => Convert.ToByte(pair[number].filename.Substring(x, 2), 16)));
                //value.AddRange(BitConverter.GetBytes(Convert.ToUInt16(pair[number].filename.Substring(0, pair[number].filename.Length - 4), 16)));

                value.AddRange(BitConverter.GetBytes((UInt16)pair[number].size));

                value.Add(0);
                value.Add(0);
                value.Add(0);
                value.Add(0);

                value.Add(0);
                value.Add(0);
            }
            else
            {
                foreach(var temp in cnmtRawParser.content)
                {
                    value.AddRange(temp.GetRawRecord());
                }
            }
        }
    }

    class CnmtRawParser
    {
        private byte[] raw_cnmt;
        public byte[] raw_title_id, raw_title_version, raw_ext_header_size, raw_content_count, raw_content_meta_count, raw_req_dl_system_version, ext_data;
        public uint ext_header_size, ext_header_loaded_size, content_count, content_meta_count;
        public byte raw_content_meta_type, raw_content_meta_attribs;
        private uint offset = 0x20;
        public List<RawContentRecord> content;
        public List<RawMetaContentRecord> meta;

        public CnmtRawParser() { }

        public CnmtRawParser(byte[] raw_cnmt)
        {
            this.raw_cnmt = raw_cnmt;
            content = new List<RawContentRecord>();
            meta = new List<RawMetaContentRecord>();
        }

        public void Parse()
        {
            raw_title_id = ReadFromOffset(0x8, 0x0);
            raw_title_version = ReadFromOffset(0x4, 0x8);
            raw_content_meta_type = raw_cnmt[0xC];
            raw_ext_header_size = ReadFromOffset(0x2, 0xE);
            raw_content_count = ReadFromOffset(0x2, 0x10);
            raw_content_meta_count = ReadFromOffset(0x2, 0x12);
            raw_content_meta_attribs = raw_cnmt[0x14];
            raw_req_dl_system_version = ReadFromOffset(0x4, 0x18);

            ext_header_size = BitConverter.ToUInt16(raw_ext_header_size, 0);
            content_count = BitConverter.ToUInt16(raw_content_count);
            content_meta_count = BitConverter.ToUInt16(raw_content_meta_count);

            if (ext_header_size > 0)
            {
                ext_header_loaded_size = BitConverter.ToUInt32(ReadFromOffset(ext_header_size, offset));
                offset += ext_header_size;
            }

            for (int i = 0; i < content_count; i++)
            {
                RawContentRecord temp = new RawContentRecord(ReadFromOffset(0x38, offset));
                offset += 0x38;
                temp.Parse();
                content.Add(temp);
            }

            for (int i = 0; i < content_meta_count; i++)
            {
                RawMetaContentRecord temp = new RawMetaContentRecord(ReadFromOffset(0x10, offset));
                offset += 0x10;
                temp.Parse();
                meta.Add(temp);
            }

            if (ext_header_loaded_size > 0)
            {
                ext_data = ReadFromOffset(ext_header_loaded_size, offset);
                offset += ext_header_loaded_size;
            }
                
        }
        private byte[] ReadFromOffset(uint amount, uint offset)
        {
            byte[] temp = new byte[amount];
            for (int i = 0; i < amount; i++)
                temp[i] = raw_cnmt[i + offset];
            return temp;
        }
    }

    class RawContentRecord
    {
        private byte[] record;
        public byte[] raw_hash, raw_content_id, raw_size;
        public byte raw_content_type, raw_id_offset;

        public RawContentRecord() { }

        public RawContentRecord(byte[] record)
        {
            this.record = record;
        }

        public void Parse()
        {
            raw_hash = ReadFromOffset(0x20, 0x0);
            raw_content_id = ReadFromOffset(0x10, 0x20);
            raw_size = ReadFromOffset(0x6, 0x30);
            raw_content_type = record[0x36];
            raw_id_offset = record[0x37];
        }
        private byte[] ReadFromOffset(int amount, int offset)
        {
            byte[] temp = new byte[amount];
            for (int i = 0; i < amount; i++)
                temp[i] = record[i + offset];
            return temp;
        }
        public byte[] GetRawRecord()
        {
            List<byte> temp = new List<byte>();
            temp.AddRange(raw_content_id);
            temp.AddRange(raw_size);
            temp.Add(raw_content_type);
            temp.Add(raw_id_offset);
            return temp.ToArray();
        }
    }
    class RawMetaContentRecord
    {
        private byte[] record;
        public byte[] raw_title_id, raw_version, raw_content_meta_attribs;
        public byte raw_content_meta_type;

        public RawMetaContentRecord() { }

        public RawMetaContentRecord(byte[] record)
        {
            this.record = record;
        }

        public void Parse()
        {
            raw_title_id = ReadFromOffset(0x8, 0x0);
            raw_version = ReadFromOffset(0x4, 0x8);
            raw_content_meta_type = record[0xC];
            raw_content_meta_attribs = ReadFromOffset(0x3, 0xD);
        }
        private byte[] ReadFromOffset(int amount, int offset)
        {
            byte[] temp = new byte[amount];
            for (int i = 0; i < amount; i++)
                temp[i] = record[i + offset];
            return temp;
        }
        public byte[] GetRawRecord()
        {
            List<byte> temp = new List<byte>();
            temp.AddRange(raw_title_id);
            temp.AddRange(raw_version);
            temp.Add(raw_content_meta_type);
            temp.AddRange(raw_content_meta_attribs);
            return temp.ToArray();
        }
    }
}
