# Generated icon texture ownership

The duplicate cache path consumes an incoming texture: a new resource adopts it,
while an equal existing resource releases it unless it already owns that same
instance. Ownership comparison reads the private texture reference and does not
decode a byte-only cached image.

`run.py --dotnet <absolute-dotnet-path> --output <receipt-directory>` compiles the
production texture/byte overloads and MD5 method plus the full IconResource class
with ImageSharp 3.1.12 from the pinned dependency catalog. Resource publication,
resource metadata and DMI parsing are inert adapters. It is not the full engine.

One thousand identical live inputs retain strong image references while a real
ImageSharp allocator wrapper counts outstanding pixel owners and bytes. Cache
and provider entries must stay at one; discarded images must be immediately
disposed, and the cached image/alias remain usable. Additional cases exercise
byte-only lazy cache hits, unequal hash-bucket collisions, unique image adoption
and provider failure after adoption. Four source controls must build then fail
their intended ownership assertion: omitted disposal, blind disposal, lazy
decoding and omitted collision byte comparison. No GC or finalizer is requested.
Logs and receipts retain immutable source/harness hashes and dependency version.

This fixes deterministic disposal and avoidable image memory pressure. It does
not prove permanently rooted retention or explain Gaia's observed live growth.
Unique generated resources still have their existing cache lifetime. Pre-handoff
encoding/pixel-buffer exception cleanup and stream capacity padding are separate
leads; this patch never disposes a texture already adopted into a cached resource.
