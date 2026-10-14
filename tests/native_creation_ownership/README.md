# Native creator and decoded savefile resource ownership

The dedicated workflow runs seven fresh checkout modes: positive, resource-baseline,
hash-only, matrix-baseline, omit-creator, omit-arguments and typesof-baseline.
Each mode builds actual upstream source, explicitly selects the real host method
and launches unchanged GameTests.NoRuntimesTest in a separate testhost process.
The positive job invokes all three explicit classes, including the separate
constructor-bearing matrix method. No local native execution is authorized by
the presence of this runner; it requires an authenticated GitHub Actions checkout.

Resource baseline and collision controls must fail their one exact assertion;
the typesof baseline must fail its exact 18 ownership assertions. Those failures
are accepted only when original logs, TRX identity/counters, observations and
assertion stacks match the strict parsers. Matrix controls deliberately expect
the observed omitted-release counts and must pass those exact observations.
Unrelated failures, skips, missing markers and unexpected control successes fail.
The constructor project must emit its one genuine expected world.Error diagnostic;
it does not weaken the fresh ordinary project's error classification.

control.py retains the other changes in every source variant. Historical source
files are authenticated by SHA-256 and raw Git blob identity; source-manifest.json
pins the candidate and API inputs. Text checkout comparisons normalize CRLF to LF.
Prepared source and actual runtime/test/compiler binaries have independent byte
receipts. Raw SDK/build/test logs and TRX are bounded and retained in a ZIP with
byte hashes, so upload-artifact's ignored-file behavior cannot omit loose logs.
Archives are evidence retention, not an acceptance oracle. Source drift and new
native logger spellings fail closed and require a reviewed parser adjustment.

Pure mutation/parser tests are source checks only. Adapted upstream builds and
native acceptance remain unrun. Fork native evidence is documented in the frozen
upstream-resource-creation-followup-e513-candidate packet. Maintainer approval is
still required before upstream PR workflows can run.
