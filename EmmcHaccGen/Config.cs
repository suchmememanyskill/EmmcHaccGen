using LibHac.Common.Keys;

namespace EmmcHaccGen
{
    public static class Config
    {
        public static KeySet keyset;
        public static string normalBisId, safeBisId, fwPath;
        public static bool noExfat, verbose, fixHashes, v5, noAutoRcm, marikoBoot;
    }
}
