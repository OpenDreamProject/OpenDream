using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.Caching;
using OpenDreamShared.Json;

namespace DMCompiler;

public class IncrementalDMM
{
    // Use a cache to stop double FileSystemWatcher events from being problematic
    private readonly MemoryCache _cache;
    private const int CacheTimeMilliseconds = 500;
    private readonly CacheItemPolicy _cachePolicy;
    private readonly FileSystemWatcher _dmmWatcher; // The watcher needs to be saved somewhere

    public IncrementalDMM()
    {
        Console.WriteLine("DMM recompilation (--incremental-dmm) is an experimental feature. Use at your own risk. Only edits to existing DMM files are currently supported.");
        _cache = MemoryCache.Default;
        _cachePolicy = new CacheItemPolicy()
        {
            RemovedCallback = HandleWatcherEvent
        };
        var path = Path.GetDirectoryName(DMCompiler.Settings.Files?[0]) ?? AppDomain.CurrentDomain.BaseDirectory;
        _dmmWatcher = new FileSystemWatcher(path);
        _dmmWatcher.Filter = "*.dmm";
        _dmmWatcher.NotifyFilter = NotifyFilters.LastWrite;
        _dmmWatcher.IncludeSubdirectories = true;
        _dmmWatcher.EnableRaisingEvents = true;
        _dmmWatcher.Changed += OnDMMEdit;
    }
    private void OnDMMEdit(object sender, FileSystemEventArgs e)
    {
        if (e.ChangeType != WatcherChangeTypes.Changed)
        {
            return;
        }

        _cachePolicy.AbsoluteExpiration = DateTimeOffset.Now.AddMilliseconds(CacheTimeMilliseconds);
        _cache.AddOrGetExisting(e.Name, e, _cachePolicy);
    }

    private void HandleWatcherEvent(CacheEntryRemovedArguments args)
    {
        if (args.RemovedReason != CacheEntryRemovedReason.Expired) return;
        var mapevent = (FileSystemEventArgs) args.CacheItem.Value;

        // Only edits to existing files are supported right now
        if (mapevent.ChangeType != WatcherChangeTypes.Changed) return;
        DMMChanged(mapevent);
    }

    private void DMMChanged(FileSystemEventArgs args)
    {
        if (args.ChangeType != WatcherChangeTypes.Changed) return;

        Console.WriteLine($"DMM Changed: {args.Name} ({args.FullPath})");
        ReparseMaps();
    }

    private void ReparseMaps()
    {
        string outputFile = Path.ChangeExtension(DMCompiler.Settings.Files[0], "json");
        List<DreamMapJson> maps = DMCompiler.ConvertMaps(DMCompiler.Preprocessor.IncludedMaps);

        DMCompiler.SaveJson(maps, DMCompiler.Preprocessor.IncludedInterface, outputFile);
    }
}
