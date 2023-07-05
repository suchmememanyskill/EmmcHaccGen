using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using LibHac.Common.Keys;
using LibHac.FsSystem;
using LibHac.Tools.FsSystem.NcaUtils;

namespace EmmcHaccGen.nca
{
    public class NcaIndexer
    {
        public List<NcaFile> Files { get; private set; }
        public Dictionary<string, List<NcaFile>> SortedFiles { get; private set; }
        public string Version { get; private set; }
        public bool RequiresV5Save { get; private set; }
        private string _fwPath;
        private KeySet _keySet;
        private bool _fixHashes = false;
        
        public NcaIndexer(string fwPath, KeySet keySet, bool fixHashes = false) 
        {
            Files = new();
            SortedFiles = new();
            _fwPath = fwPath;
            _keySet = keySet;
            _fixHashes = fixHashes;
            
            Indexer();
            Sorter();
            VersionExtract();
        }

        private void Indexer()
        {
            foreach (var file in Directory.EnumerateFiles(_fwPath, "*.nca").ToArray())
            {
                NcaFile ncaFile = new(_keySet, file, _fixHashes);
                Files.Add(ncaFile);
            }
        }

        private void Sorter()
        {
            Dictionary<string, List<NcaFile>> ncaDict = new();

            foreach (var nca in Files)
            {
                if (ncaDict.ContainsKey(nca.TitleId))
                    ncaDict[nca.TitleId].Add(nca);
                else
                {
                    List<NcaFile> idpair = new List<NcaFile>();
                    idpair.Add(nca);
                    ncaDict.Add(nca.TitleId, idpair);
                }
            }

            for (int i = 0; i < ncaDict.Count; i++)
            {
                var ncalist = ncaDict.ElementAt(i);
                var ncalistsorted = ncalist.Value.OrderBy(i => i.Header.ContentType != NcaContentType.Meta).ToList();
                ncaDict[ncalist.Key] = ncalistsorted;
            }

            foreach (var item in ncaDict.OrderBy(x => Convert.ToInt64(x.Key, 16)))
                SortedFiles.Add(item.Key, item.Value);
        }

        private void VersionExtract()
        {
            NcaFile file = FindNca("0100000000000809", NcaContentType.Meta);

            if (file == null)
                throw new Exception("Version NCA (PID: 0100000000000809) was not found");
            
            TitleVersion extractedVersion = file.Cnmt.TitleVersion;
            Version = $"{extractedVersion.Major}.{extractedVersion.Minor}.{extractedVersion.Patch}";
            RequiresV5Save = extractedVersion.Major >= 5;
        }

        public NcaFile? FindNca(string titleId, NcaContentType type)
            => SortedFiles.ContainsKey(titleId.ToUpper())
                ? SortedFiles[titleId.ToUpper()]?.Find(x => x.Header.ContentType == type)
                : null;
    }
}
