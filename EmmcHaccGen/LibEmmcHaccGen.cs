using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.Serialization;
using System.Security.Cryptography;
using EmmcHaccGen.bis;
using EmmcHaccGen.imkv;
using EmmcHaccGen.nca;
using LibHac.Common;
using LibHac.Common.Keys;
using LibHac.Fs;
using LibHac.Fs.Fsa;
using LibHac.FsSystem;
using LibHac.Ncm;
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
        _keySet = ExternalKeyReader.ReadKeyFile(keySetPath);
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

    void CursedSavedataIdPatching(FileStream fp, ProgramId saveid) {
        // im sorry

        var id = BitConverter.GetBytes(saveid.Value);
    
        // patch Extra Data A + B
        fp.Seek(0x6d8 + 0x18, SeekOrigin.Begin);
        fp.Write(id, 0, 0x8);
        fp.Seek(0x8d8 + 0x18, SeekOrigin.Begin);
        fp.Write(id, 0, 0x8);

        // fixup DISF hash that we just invalidated
        var buf = new byte[0x4000 - 0x300];
        fp.Seek(0x300, SeekOrigin.Begin);
        fp.Read(buf, 0, 0x4000 - 0x300);
        var hasher = SHA256.Create();
        byte[] hash = hasher.ComputeHash(buf);
        fp.Seek(0x100 + 0x8, SeekOrigin.Begin);
        fp.Write(hash, 0, 0x20);
    }

    void WriteSave(string path, ProgramId saveid, IDictionary<string, byte[]> files, bool useV5Save) {
        FileStream destSave = new FileStream($"{path}/SYSTEM/save/{saveid}", FileMode.CreateNew);
        string saveType = (useV5Save) ? "v5" : "v4";
        Assembly.GetExecutingAssembly().GetManifestResourceStream($"EmmcHaccGen.save.save.stub.{saveType}")!.CopyTo(destSave);
        CursedSavedataIdPatching(destSave, saveid);
        destSave.Close();

        Console.WriteLine($"Writing save [{path}/SYSTEM/save/{saveid}]...");
        using (IStorage outfile = new LocalStorage($"{path}/SYSTEM/save/{saveid}", FileAccess.ReadWrite))
        {
            var save = new SaveDataFileSystem(_keySet, outfile, IntegrityCheckLevel.ErrorOnInvalid, true);

            foreach (KeyValuePair<string, byte[]> kv in files) {
                UniqueRef<IFile> file = new();

                var create_path = new LibHac.Fs.Path();
                create_path.Initialize(new U8Span(kv.Key));
                save.CreateFile(create_path, kv.Value.Length);

                save.OpenFile(ref file, new U8Span(kv.Key), OpenMode.AllowAppend | OpenMode.ReadWrite).ThrowIfFailure();
                file.Get.Write(0, kv.Value, WriteOption.Flush).ThrowIfFailure();
                Console.WriteLine($"  save://{saveid}/{kv.Key} [0x{kv.Value.Length:x04}]");
                save.Commit(_keySet).ThrowIfFailure();
            }
        }
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

        Imkv ncm_imkvdb = new Imkv(NcaIndexer, skipNcas);
        
        if (dumpImkvdb)
            ncm_imkvdb.DumpToFile($"{path}/data.arc");

        var ncm_saveid = new ProgramId(0x8000000000000120);
        WriteSave(path, ncm_saveid, new Dictionary<string, byte[]>(){
            {"/meta/imkvdb.arc", ncm_imkvdb.Result}
        }, useV5Save);

        // build the fs save index imkvdb, for now this only contains the ncm content index save
        UInt64 save_size = (UInt64)new FileInfo($"{path}/SYSTEM/save/8000000000000120").Length;
        Imen imen = new Imen(ncm_saveid, save_size);
        Imkv fs_save_index = new Imkv(new List<Imen>(){imen});

        WriteSave(path, new ProgramId(0x8000000000000000), new Dictionary<string, byte[]>(){
            {"/imkvdb.arc", fs_save_index.Result},
            {"/lastPublishedId", BitConverter.GetBytes((ulong)0)}
        }, useV5Save);
    }
}
