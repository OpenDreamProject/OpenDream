"""Exact additive read-cache composition; preserves frozen baseline originals."""
CACHE_HELPER = b'    internal void ReleaseFileDataCache() {\n        // In-memory resources own their bytes; only disk-backed data can be reloaded.\n        if (string.IsNullOrEmpty(filePath))\n            return;\n\n        ResourceData = null; // The setter also balances TOOLS memory accounting.\n    }\n\n'
OLD_READ = b'            var data = Resource.ReadAsString();'
READ_FINALLY = b'            string? data;\n            try {\n                data = Resource.ReadAsString();\n            } finally {\n                Resource.ReleaseFileDataCache();\n            }'

def compose_read(original):
 assert original.count(OLD_READ)==1 and b"Resource.ReleaseFileDataCache();" not in original
 return original.replace(OLD_READ,READ_FINALLY)

def compose_helper(original):
 assert original.count(b"    public void Clear()") == 1 and b"internal void ReleaseFileDataCache()" not in original
 return original.replace(b"    public void Clear()",CACHE_HELPER+b"    public void Clear()")
