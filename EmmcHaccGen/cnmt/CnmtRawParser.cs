using System;
using System.Collections.Generic;
using System.Text;

namespace EmmcHaccGen.cnmt
{
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
            this.Parse();
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
                content.Add(temp);
            }

            for (int i = 0; i < content_meta_count; i++)
            {
                RawMetaContentRecord temp = new RawMetaContentRecord(ReadFromOffset(0x10, offset));
                offset += 0x10;
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
            this.Parse();
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
            this.Parse();
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
