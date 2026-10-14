# Disk-backed savefile read cache

The disk-only helper releases cached bytes through the existing TOOLS-accounted setter in finally after virtual ReadAsString. Wrapper IDs, parsed roots, embedded data and close/error behavior remain unchanged. Explicit native fixture and positive/baseline/omit-release/drop-inmemory controls are retained from fork PR43. See ../retention_upstream/README.md for adaptation source pins and UNRUN limits.
