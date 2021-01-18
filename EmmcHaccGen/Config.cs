using System;
using System.Collections.Generic;
using System.Text;
using LibHac;

namespace EmmcHaccGen
{
    public static class Config
    {
        public static Keyset keyset;
        public static string normalBisId, safeBisId, fwPath;
        public static bool noExfat, verbose, fixHashes, v5, noAutoRcm, marikoBoot;
    }
}
