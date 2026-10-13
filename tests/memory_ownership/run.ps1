param(
    [Parameter(Mandatory=$true)][string]$Dotnet,
    [ValidateSet('none','baseline','filtered-dispose','async-clear','stack-clear','scheduler-fault','caller-clear')][string]$NegativeControl = 'none'
)
$ErrorActionPreference = 'Stop'
$repository = (Resolve-Path -LiteralPath (Join-Path $PSScriptRoot '../..')).Path
$probeTemp = Join-Path ([IO.Path]::GetTempPath()) ('opendream-ownership-' + [Guid]::NewGuid().ToString('N'))
New-Item -ItemType Directory -Path $probeTemp | Out-Null
$probeEnvNames = @('DOTNET_CLI_HOME', 'DOTNET_SKIP_FIRST_TIME_EXPERIENCE', 'DOTNET_NOLOGO', 'DOTNET_CLI_WORKLOAD_UPDATE_NOTIFY_DISABLE', 'DOTNET_GENERATE_ASPNET_CERTIFICATE', 'DOTNET_ADD_GLOBAL_TOOLS_TO_PATH')
$priorEnvironment = @{}
foreach ($name in $probeEnvNames) { $priorEnvironment[$name] = [Environment]::GetEnvironmentVariable($name, 'Process') }
try {
    $sourceRoot = Join-Path $probeTemp 'source'
    $procDirectory = Join-Path $sourceRoot 'OpenDreamRuntime/Procs'
    New-Item -ItemType Directory -Path $procDirectory -Force | Out-Null
    foreach ($name in @('DreamEnumerators.cs','AsyncNativeProc.cs','DMProc.cs','ProcScheduler.cs')) {
        $relative = 'OpenDreamRuntime/Procs/' + $name
        if ($NegativeControl -eq 'baseline') {
            $baseline = & git -C $repository show ('b1a452e7:' + $relative)
            if ($LASTEXITCODE -ne 0) { throw 'Cannot load immutable baseline source' }
            $text = $baseline -join "`n"
        } else { $text = Get-Content -LiteralPath (Join-Path $repository $relative) -Raw }
        if ($NegativeControl -eq 'scheduler-fault' -and $name -eq 'ProcScheduler.cs') {
            $original = & git -C $repository show ('d443a95f:' + $relative)
            if ($LASTEXITCODE -ne 0) { throw 'Cannot load scheduler negative control' }
            $text = $original -join "`n"
        }
        if ($NegativeControl -eq 'filtered-dispose' -and $name -eq 'DreamEnumerators.cs') {
            $start = $text.IndexOf('internal sealed class FilteredDreamValueArrayEnumerator')
            $head = $text.Substring(0, $start)
            $tail = $text.Substring($start)
            $tail = [regex]::Replace($tail, '        for \(int i = _current \+ 1; i < values.Length; i\+\+\)\r?\n            values\[i\]\.Dispose\(\);\r?\n', '')
            $text = $head + $tail
        }
        if ($NegativeControl -eq 'async-clear' -and $name -eq 'AsyncNativeProc.cs') {
            $text = $text.Replace('Array.Clear(_arguments, 0, ArgumentCount);', '')
        }
        if ($name -eq 'DMProc.cs') {
            $match = [regex]::Match($text, 'DreamValuePool\.Return\(_stack(?:,\s*clearArray:\s*true)?\);')
            if (!$match.Success) { throw 'Production stack-return statement not found' }
            $statement = $match.Value.Replace('DreamValuePool','pool').Replace('_stack','stack')
            if ($NegativeControl -eq 'stack-clear') { $statement = $statement.Replace(', clearArray: true', '') }
            "using System.Buffers; using OpenDreamRuntime; public static class ProductionStackReturn { public static void Return(ArrayPool<DreamValue> pool, DreamValue[] stack) { $statement } }" | Set-Content -LiteralPath (Join-Path $probeTemp 'StackReturn.cs')
        } else {
            # Fully qualify injection annotations to avoid .NET DependencyAttribute alias ambiguity.
            $text.Replace('[Dependency]', '[Robust.Shared.IoC.DependencyAttribute]') | Set-Content -LiteralPath (Join-Path $procDirectory $name)
        }
    }
    $threadSource = Get-Content -LiteralPath (Join-Path $repository 'OpenDreamRuntime/DreamThread.cs') -Raw
    $stateStart = $threadSource.IndexOf('public abstract class ProcState')
    $dispose = [regex]::Match($threadSource.Substring($stateStart), 'public virtual void Dispose\(\) \{[^}]+\}')
    if (!$dispose.Success) { throw 'Production ProcState.Dispose not found' }
    $disposeBody = $dispose.Value.Replace('public virtual void', 'public void')
    if ($NegativeControl -in @('caller-clear','baseline')) { $disposeBody = $disposeBody.Replace('Caller = null;', '') }
    "using OpenDreamRuntime; public class ProductionStateDispose { public object? Thread, Caller; public DreamValue Result; public bool WaitFor; public int Id; $disposeBody }" | Set-Content -LiteralPath (Join-Path $probeTemp 'StateDispose.cs')
    $env:DOTNET_CLI_HOME = Join-Path $probeTemp 'cli'
    $env:DOTNET_SKIP_FIRST_TIME_EXPERIENCE = '1'
    $env:DOTNET_NOLOGO = '1'
    $env:DOTNET_CLI_WORKLOAD_UPDATE_NOTIFY_DISABLE = 'true'
    $env:DOTNET_GENERATE_ASPNET_CERTIFICATE = 'false'
    $env:DOTNET_ADD_GLOBAL_TOOLS_TO_PATH = 'false'
    & $Dotnet run --project (Join-Path $PSScriptRoot 'OwnershipProbe.csproj') --configuration Release --property:ImportDirectoryPackagesProps=false --property:SourceDirectory=$sourceRoot --property:ProbeGeneratedDirectory=$probeTemp --property:BaseIntermediateOutputPath="$probeTemp/obj/" --property:OutputPath="$probeTemp/bin/" --verbosity quiet
    $result = $LASTEXITCODE
    if ($result -ne 0) { throw "Ownership probe failed ($result), control=$NegativeControl" }
} finally {
    foreach ($name in $probeEnvNames) { [Environment]::SetEnvironmentVariable($name, $priorEnvironment[$name], 'Process') }
    # This directory is uniquely created above under the system temporary directory.
    Remove-Item -LiteralPath $probeTemp -Recurse -Force
}
