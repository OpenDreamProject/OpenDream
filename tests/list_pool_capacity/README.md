# Bounded idle list storage

Return pooling retains cleared arrays only when capacity exceeds the configured lower threshold and is at most65536, within the configured count cap. Live lists remain unrestricted. Explicit native fixtures and positive/baseline/nonclearing/no-pooling controls are retained from fork PR42. Each native method and unchanged NoRuntimes runs in a separate process. See ../retention_upstream/README.md for adaptation source pins and UNRUN limits.
