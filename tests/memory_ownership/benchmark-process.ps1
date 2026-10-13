param(
    [Parameter(Mandatory=$true)][int]$TargetProcessId,
    [Parameter(Mandatory=$true)][string]$CounterTool,
    [Parameter(Mandatory=$true)][string]$OutputDirectory,
    [ValidateRange(10,1800)][int]$Seconds = 120,
    [string]$PythonExecutable,
    [string]$ControlScript,
    [string]$ControlProfile,
    [string]$ServerLog
)
$ErrorActionPreference = 'Stop'
New-Item -ItemType Directory -Path $OutputDirectory -Force | Out-Null
$target = Get-Process -Id $TargetProcessId
$startIdentity = $target.StartTime.ToUniversalTime().ToString('o')
$counterFile = Join-Path $OutputDirectory 'counters.csv'
if (Test-Path -LiteralPath $counterFile) { throw 'Refusing to overwrite benchmark counters' }
$logOffset = if ($ServerLog) { (Get-Item -LiteralPath $ServerLog).Length } else { 0 }
$duration = [TimeSpan]::FromSeconds($Seconds).ToString('hh\:mm\:ss')
$counter = Start-Process -FilePath $CounterTool -ArgumentList @('collect','--process-id',$TargetProcessId,'--counters','System.Runtime','--format','csv','--output',('"'+$counterFile+'"'),'--duration',$duration) -PassThru -WindowStyle Hidden -RedirectStandardOutput (Join-Path $OutputDirectory 'counter.stdout.log') -RedirectStandardError (Join-Path $OutputDirectory 'counter.stderr.log')
$clock = [Diagnostics.Stopwatch]::StartNew()
$samples = [Collections.Generic.List[object]]::new()
$topics = [Collections.Generic.List[object]]::new()
$previousCpu = $target.TotalProcessorTime.TotalSeconds
$previousTime = 0.0
$nextTopic = 0.0
try {
    while ($clock.Elapsed.TotalSeconds -lt $Seconds) {
        $target.Refresh()
        if ($target.HasExited -or $target.StartTime.ToUniversalTime().ToString('o') -ne $startIdentity) { throw 'Target process exited or changed identity' }
        $elapsed = $clock.Elapsed.TotalSeconds
        $cpu = $target.TotalProcessorTime.TotalSeconds
        $cpuCores = if ($elapsed -gt $previousTime) { ($cpu-$previousCpu)/($elapsed-$previousTime) } else { 0 }
        $samples.Add([pscustomobject]@{utc=[DateTime]::UtcNow.ToString('o');elapsed_seconds=$elapsed;private_bytes=$target.PrivateMemorySize64;working_bytes=$target.WorkingSet64;cpu_seconds=$cpu;cpu_cores=$cpuCores;threads=$target.Threads.Count})
        $previousCpu=$cpu; $previousTime=$elapsed
        if ($ControlProfile -and $elapsed -ge $nextTopic) {
            $latency=[Diagnostics.Stopwatch]::StartNew()
            $response = & $PythonExecutable $ControlScript --profile $ControlProfile topic-status
            $topicCode=$LASTEXITCODE
            $latency.Stop()
            if ($topicCode -ne 0) { throw 'Read-only game status request failed' }
            $status=($response -join "`n" | ConvertFrom-Json).status
            $topics.Add([pscustomobject]@{elapsed_seconds=$elapsed;request_ms=$latency.Elapsed.TotalMilliseconds;game_id=$status.game_id;players=$status.players;map=$status.map_name;map_age=$status.map_age;gamemode=$status.map_gamemode;round_deciseconds=$status.round_duration_in_ticks})
            $nextTopic=$elapsed+10
        }
        Start-Sleep -Milliseconds 1000
    }
    if (!$counter.WaitForExit(15000)) { throw 'Counter collection did not complete in its bounded window' }
    if ($counter.ExitCode -ne 0) { throw "Counter collector failed: $($counter.ExitCode)" }
} finally {
    if (!$counter.HasExited) { $counter.Kill(); $counter.WaitForExit() }
    $samples | Export-Csv -LiteralPath (Join-Path $OutputDirectory 'process.csv') -NoTypeInformation
    $topics | Export-Csv -LiteralPath (Join-Path $OutputDirectory 'topic.csv') -NoTypeInformation
    if ($ServerLog) {
        $stream=[IO.File]::Open($ServerLog,[IO.FileMode]::Open,[IO.FileAccess]::Read,[IO.FileShare]::ReadWrite)
        try { $stream.Position=$logOffset; $reader=[IO.StreamReader]::new($stream); $fragment=$reader.ReadToEnd(); [IO.File]::WriteAllText((Join-Path $OutputDirectory 'server-window.log'),$fragment) } finally { $stream.Dispose() }
    }
    $receipt=@{pid=$TargetProcessId;process_start_utc=$startIdentity;duration_seconds=$clock.Elapsed.TotalSeconds;samples=$samples.Count;topic_samples=$topics.Count;cpu_units='cores consumed; 1.0 means one fully occupied core';topic_latency='Includes Python launch and socket round trip; not a client frame latency measurement';scope='Passive process/CLR counters and optional read-only game status. No forced GC, gameplay actions, season transitions or server mutations.'}
    $receipt | ConvertTo-Json | Set-Content -LiteralPath (Join-Path $OutputDirectory 'receipt.json')
}
Write-Output "PASS collected $($samples.Count) process samples and $($topics.Count) game status samples"
