using System;
using System.Collections.Generic;
using System.Text;
using LibHac.FsSystem.NcaUtils;

namespace EmmcHaccGen
{
    class ncaList
    {
        public string filename;
        public string titleid;
        public NcaContentType type;

        public ncaList()
        {
            filename = "";
            titleid = "";
            type = NcaContentType.Control;
        }
    }
}
