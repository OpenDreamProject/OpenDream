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
            if (_resourceData == null && File.Exists(filePath)) {
                _resourceData = File.ReadAllBytes(filePath);
            }

            return _resourceData;
        }
    }

    private byte[]? _resourceData;
    #if TOOLS
    private ProfilerMemory? _tracyMemoryId;
    #endif

    public DreamResource(int id, byte[] data) : this(id, null, null) {
        _resourceData = data;
        #if TOOLS
        _tracyMemoryId = Profiler.BeginMemoryZone((ulong)(Unsafe.SizeOf<DreamResource>() + (ResourceData is null? 0 : ResourceData.Length)), "resource");
        #endif
    }

    /// <summary>
    /// Invalidates any caching this resource may have, causing it to be re-read from disk.
    /// Calling this alone will not update what clients are holding.
    /// </summary>
    public void ReloadFromDisk() {
        _resourceData = null;
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
        _resourceData = null;
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
