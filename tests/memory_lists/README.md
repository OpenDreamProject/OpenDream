# List ownership and range filtering regressions

Run `run.py --dotnet <absolute .NET 10 SDK executable>` with Python 3.
The probe compiles production list methods and the production `HandleRange`
filter with inert reference-count and appearance adapters in temporary storage.

Nine assertions cover 1,000 swaps, identical indices, failed second reads,
1,000 associative appends, self-append, the invisibility 100/101 boundary,
area deduplication, ordering and avoiding mutable appearance copies.

`--baseline b1a452e7` must reject the historical swap implementation.
`--control swap`, `--control append` and `--control range` independently restore
the historical implementation and must reject its corresponding assertion.
Negative controls pass only when the expected runtime assertion fails.

This is a focused production-body regression with isolated dependencies.
It does not execute the complete DM engine or measure server memory/gameplay.
