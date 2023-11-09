using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using EmmcHaccGen.bis;
using EmmcHaccGen.imkv;
using EmmcHaccGen.nca;
using LibHac.Common;
using LibHac.Common.Keys;
using LibHac.Fs;
using LibHac.Fs.Fsa;
using LibHac.FsSystem;
using LibHac.Tools.FsSystem;
using LibHac.Tools.FsSystem.NcaUtils;
using LibHac.Tools.FsSystem.Save;
using Path = System.IO.Path;

namespace EmmcHaccGen;

public class LibEmmcHaccGen
{
    private KeySet? _keySet = null;
    public string FwPath { get; private set; }
    public NcaIndexer NcaIndexer { get; private set; }
    public bool HasExfatCompat = false;
    
    private static string[] FOLDERSTRUCTURE = new[]
    {
        "/SAFE",
        "/SYSTEM/Contents/placehld",
        "/SYSTEM/Contents/registered",
        "/SYSTEM/save",
        "/USER/Album",
        "/USER/Contents/placehld",
        "/USER/Contents/registered",
        "/USER/save",
        "/USER/saveMeta",
        "/USER/temp"
    };
    
    private void SetArchiveRecursively(string path)
    {
        foreach (var file in Directory.EnumerateFiles(path))
        {
            File.SetAttributes(file, FileAttributes.Archive);
        }
        foreach (var folder in Directory.EnumerateDirectories(path))
        {
            File.SetAttributes(folder, 0);
            SetArchiveRecursively(folder);
        }
    }

    public LibEmmcHaccGen(string keySetPath, string firmwarePath, bool fixHashes = false)
    {
        Load(keySetPath, firmwarePath, fixHashes);
    }

    private void Load(string keySetPath, string firmwarePath, bool fixHashes = false)
    {
        _keySet = KeySet.CreateDefaultKeySet();
        _keySet.SetMode(KeySet.Mode.Prod);
        var loadedAssemblies = AppDomain.CurrentDomain.GetAssemblies();
        var libHac = loadedAssemblies.FirstOrDefault(x => x.GetName().Name == "LibHac")!;
        var defaultKeySet = libHac.GetType("LibHac.Common.Keys.DefaultKeySet")!;
        List<KeyInfo> keyInfos = (List<KeyInfo>)defaultKeySet.GetMethod("CreateKeyList").Invoke(null, null)!;
        keyInfos.Add(new KeyInfo(252, KeyInfo.KeyType.CommonDrvd, "save_mac_key", (set, _) => set.DeviceUniqueSaveMacKeys[0]));
        
        using var storage = new FileStream(keySetPath, FileMode.Open, FileAccess.Read);
        ExternalKeyReader.ReadMainKeys(_keySet, storage, keyInfos);
        _keySet.DeriveKeys();
        
        FwPath = firmwarePath;
        
        int convertCount = 0;
        foreach (var foldername in Directory.GetDirectories(FwPath, "*.nca"))
        {
            convertCount++;
            File.Move($"{foldername}/00", $"{FwPath}/temp");
            Directory.Delete(foldername);
            File.Move($"{FwPath}/temp", foldername);
        }
        
        if (convertCount > 0)
            Console.WriteLine($"Converted folder ncas to files (count: {convertCount})");
        
        NcaIndexer = new(FwPath, _keySet, fixHashes);
        HasExfatCompat = NcaIndexer.FindNca("010000000000081B", NcaContentType.Data) != null &&
                         NcaIndexer.FindNca("010000000000081C", NcaContentType.Data) != null;
    }

    public void Write(string basePath, bool autorcm, bool exfat, bool mariko, bool overwrite, bool verbose = false)
    {
        string prefix = (mariko) ? "a" : "NX";
        string destFolder = $"{prefix}-{NcaIndexer.Version}" + (exfat ? "_exFAT" : "");
        string destPath = Path.Join(basePath, destFolder);

        if (Directory.Exists(destPath))
        {
            if (overwrite)
                Directory.Delete(destFolder, true);
            else
                throw new Exception("Output directory already exists");
        }

        Directory.CreateDirectory(destPath);
        
        WriteBis(destPath, autorcm, exfat, mariko);
        WriteSystem(destFolder, exfat, NcaIndexer.RequiresV5Save || mariko, verbose);
    }

    public void WriteBis(string path, bool autorcm, bool exfat, bool mariko)
    {
        if (exfat && !HasExfatCompat)
            throw new Exception("Provided firmware does not have exfat compatibility");

        if (autorcm && mariko)
            throw new Exception("Generating Mariko Firmware with Autorcm is not allowed");

        BisAssembler bisAssembler = new(NcaIndexer, exfat, autorcm, mariko);
        bisAssembler.Save(path);
        BisFileAssembler bisFileAssembler = new(bisAssembler);
        bisFileAssembler.Save($"{path}/boot.bis");
    }

    public void WriteSystem(string path, bool exfat, bool useV5Save, bool dumpImkvdb = false)
    {
        if (!useV5Save && NcaIndexer.RequiresV5Save)
            throw new Exception("Requested for a v4 save while a v5 save for the current firmware is required");
        
        foreach(string folder in FOLDERSTRUCTURE)
            Directory.CreateDirectory($"{path}{folder}");
        
        // Copy fw files
        foreach (var file in Directory.EnumerateFiles(FwPath))
        {
            File.Copy(file, $"{path}/SYSTEM/Contents/registered/{file.Split(new char[] { '/', '\\' }).Last().Replace(".cnmt.nca", ".nca")}", true);
        }
        
        // Archive bit setting
        SetArchiveRecursively($"{path}/SYSTEM");
        SetArchiveRecursively($"{path}/USER");
        
        //Imkv Generation
        List<string> skipNcas = new();

        if (!exfat)
        {
            skipNcas.Add("010000000000081B");
            skipNcas.Add("010000000000081C");
        }

        Imkv imkvdb = new Imkv(NcaIndexer, skipNcas);
        
        if (dumpImkvdb)
            imkvdb.DumpToFile($"{path}/data.arc");
        
        FileStream destSave = new FileStream($"{path}/SYSTEM/save/8000000000000120", FileMode.CreateNew);
        string saveType = (useV5Save) ? "v5" : "v4";
        Assembly.GetExecutingAssembly().GetManifestResourceStream($"EmmcHaccGen.save.save.stub.{saveType}")!.CopyTo(destSave);
        destSave.Close();

        using (IStorage outfile = new LocalStorage($"{path}/SYSTEM/save/8000000000000120", FileAccess.ReadWrite))
        {
            var save = new SaveDataFileSystem(_keySet, outfile, IntegrityCheckLevel.ErrorOnInvalid, true);
            UniqueRef<IFile> file = new();

            save.OpenFile(ref file, new U8Span("/meta/imkvdb.arc"), OpenMode.AllowAppend | OpenMode.ReadWrite);
            using (file)
            {
                file.Get.Write(0, imkvdb.Result, WriteOption.Flush).ThrowIfFailure();
            }
            save.Commit(_keySet).ThrowIfFailure();
        }
        
        Console.WriteLine($"Wrote save with an imvkdb size of 0x{imkvdb.Result.Length:X4}");
    }
}