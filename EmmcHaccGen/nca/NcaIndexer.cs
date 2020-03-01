using System;
using System.Collections.Generic;
using System.Text;
using LibHac;
using System.IO;
using LibHac.FsSystem.NcaUtils;
using System.Linq;

namespace EmmcHaccGen.nca
{
    class NcaIndexer
    {
        string path;
        public List<NcaFile> files;
        public Dictionary<string, List<NcaFile>> sortedNcaDict;

        public NcaIndexer() 
        {
            path = Config.fwPath;
            files = new List<NcaFile>();
            sortedNcaDict = new Dictionary<string, List<NcaFile>>();

            this.Indexer();
            this.Sorter();
        }

        private void Indexer()
        {
            foreach (var file in Directory.EnumerateFiles(path, "*.nca"))
            {
                NcaFile ncaFile = new NcaFile();
                ncaFile.filename = file.Split(new char[]{'/', '\\' } ).Last();
                ncaFile.AddNcaInfo(file);
                ncaFile.GetHash();
                files.Add(ncaFile);
            }
        }

        private void Sorter()
        {
            Dictionary<string, List<NcaFile>> NcaDict = new Dictionary<string, List<NcaFile>>();
            List<string> exclude = new List<string>();

            if (Config.noExfat)
            {
                exclude.Add("010000000000081B");
                exclude.Add("010000000000081C");
            }

            foreach (var nca in files)
            {
                if (exclude.Contains(nca.titleId))
                    continue;

                if (NcaDict.ContainsKey(nca.titleId))
                    NcaDict[nca.titleId].Add(nca);
                else
                {
                    List<NcaFile> idpair = new List<NcaFile>();
                    idpair.Add(nca);
                    NcaDict.Add(nca.titleId, idpair);
                }
            }

            for (int i = 0; i < NcaDict.Count; i++)
            {
                var ncalist = NcaDict.ElementAt(i);
                var ncalistsorted = ncalist.Value.OrderBy(i => i.header.ContentType != NcaContentType.Meta).ToList();
                NcaDict[ncalist.Key] = ncalistsorted;
            }

            foreach (var item in NcaDict.OrderBy(x => Convert.ToInt64(x.Key, 16)))
                sortedNcaDict.Add(item.Key, item.Value);
        }

        public NcaFile FindNca(string titleid, NcaContentType type)
        {
            string titleID = titleid.ToUpper();
            return files.Find(x => x.titleId == titleID && x.header.ContentType == type);
        }
    }
}
