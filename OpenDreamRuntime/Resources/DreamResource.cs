using System.IO;
using System.Runtime.CompilerServices;
using System.Text;

namespace OpenDreamRuntime.Resources;

[Virtual]
public class DreamResource(int id, string? filePath, string? resourcePath) {
    public readonly string? ResourcePath = resourcePath;
    public readonly int Id = id;

    public byte[]? ResourceData {
        get {
            if (_resourceDataBacking == null && File.Exists(filePath)) {
                _resourceDataBacking = File.ReadAllBytes(filePath);
                #if TOOLS
                _tracyMemoryId?.ReleaseMemory();
                _tracyMemoryId = Profiler.BeginMemoryZone((ulong)(Unsafe.SizeOf<DreamResource>() + (_resourceDataBacking?.Length ?? 0)), "resource");
                #endif
            }
            return _resourceDataBacking;
        }
        private set {
            #if TOOLS
            _tracyMemoryId?.ReleaseMemory();
            _tracyMemoryId = Profiler.BeginMemoryZone((ulong)(Unsafe.SizeOf<DreamResource>() + (value?.Length ?? 0)), "resource");
            #endif
            _resourceDataBacking = value;
        }
    }

    #if TOOLS
    private ProfilerMemory? _tracyMemoryId;
    #endif
    private byte[]? _resourceDataBacking;

    public DreamResource(int id, byte[] data) : this(id, null, null) {
        ResourceData = data;
    }

    /// <summary>
    /// Invalidates any caching this resource may have, causing it to be re-read from disk.
    /// Calling this alone will not update what clients are holding.
    /// </summary>
    public void ReloadFromDisk() {
        _resourceDataBacking = null;
    }

    public virtual string? ReadAsString() {
        if (ResourceData == null) return null;

        string resourceString = Encoding.ASCII.GetString(ResourceData);

        resourceString = resourceString.Replace("\r\n", "\n");
        return resourceString;
    }

    public void Clear() {
        if (string.IsNullOrEmpty(filePath))
            return;

        CreateDirectory();
        File.WriteAllText(filePath, string.Empty);
    }

    public virtual void Output(DreamValue value) {
        if (ResourcePath == null)
            throw new Exception("Cannot write to resource without a path");

        string? text;
        if (value.IsNull) {
            text = string.Empty;
        } else if (!value.TryGetValueAsString(out text)) {
            throw new Exception($"Invalid output operation '{ResourcePath}' << {value}");
        }

        CreateDirectory();
        File.AppendAllText(ResourcePath, text + "\r\n");
        _resourceDataBacking = null;
    }

    private void CreateDirectory() {
        if (filePath == null)
            return;

        string? directory = Path.GetDirectoryName(filePath);
        if (string.IsNullOrEmpty(directory))
            return;

        Directory.CreateDirectory(directory);
    }

    public override string ToString() {
        return $"'{ResourcePath}'";
    }
}
