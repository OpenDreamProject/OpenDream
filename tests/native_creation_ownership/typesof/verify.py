"""Fail-closed classification of actual connected typesof native creator ownership evidence.
Pure adversarial controls are parser tests, never engine/runtime evidence.
"""
import argparse
import json
from pathlib import Path
import re
import xml.etree.ElementTree as ET

NS = {"t": "http://microsoft.com/schemas/VisualStudio/TeamTest/2010"}
CLASS = "Content.IntegrationTests.TypesOfOwnershipTests"
TEST = "RejectedTypesOfReleasesCreator"
MARKER = "TYPESOF_COMPLETE: 18 rejection cases x 1000 compiled calls; exact new list identities, owners, contents, cleanup and success controls"
SPEC = json.loads(Path(__file__).with_name("expected-rejection-observations.json").read_text())
SUCCESS = "TYPESOF_SUCCESS: returned=6; empty=0; drop=17; released=1; owners=[1,1,1]; contents=type-only"
LIMIT = 8 * 1024 * 1024
ERROR = re.compile(r"\[ERRO\]|Unhandled exception|Runtime occurred|runtime error|test run aborted|testhost[^\n]*(?:crash|exit)|Isolated DM fixture compilation failed|(?:error|warning) (?:CS|DM|MSB)\d+|:\s*error\b|[1-9]\d* (?:Error|Warning)\(s\)|Build FAILED|Failed to (?:compile|start|launch)|NUnit.*(?:adapter error|discovery failed)", re.I)
ROW = re.compile(r"^(TYPESOF_REJECT_[a-z_]+): owners=\[([0-9,]+)\]; new=(\d+); ref1=(\d+); content=(\d+); lists=(\d+) expected=(\d+); datums=(\d+) expected=(\d+)$")

# This fixture is deliberately standalone. No GameTests/engine/foreign assertion frames are accepted.
FIXTURE_FILE = r"(?:[^\r\n]+/)?Content\.IntegrationTests/TypesOfOwnershipTests\.cs:line [1-9]\d*"
INTEGRATION_FILE = r"(?:[^\r\n]+/)?RobustToolbox/Robust\.UnitTesting/RobustIntegrationTest\.cs:line [1-9]\d*"
FIXTURE_METHODS = [
    r"RejectedTypesOfReleasesCreator\(\)",
    r"Reject\(DreamManager manager, DreamObjectTree tree, DreamRefManager refs, DreamProc proc, DreamProc dropped, String route, String kind, String position\)",
    r"<>c__DisplayClass\d+_\d+\.<RejectedTypesOfReleasesCreator>b__\d+\(\)",
]
FRAME_PATTERNS = [r"at " + re.escape(CLASS) + r"\." + method + r" in " + FIXTURE_FILE for method in FIXTURE_METHODS] + [
    r"at NUnit\.Framework\.Assert\.Multiple\(TestDelegate testDelegate\)",
    r"at Robust\.UnitTesting\.RobustIntegrationTest\.IntegrationGameLoop\.(?:SingleThreadRunUntilEmpty|Run)\(\) in " + INTEGRATION_FILE,
    r"at Robust\.UnitTesting\.RobustIntegrationTest\.ServerIntegrationInstance\._serverMain\(\) in " + INTEGRATION_FILE,
    r"at System\.Threading\.Thread\.StartHelper\.Callback\(Object state\)",
]
FRAME_PATTERNS += [
    r"at Robust\.UnitTesting\.RobustIntegrationTest\.IntegrationInstance\.WaitIdleImplAsync\(Boolean throwOnUnhandled, CancellationToken cancellationToken\) in " + INTEGRATION_FILE,
    r"at Robust\.UnitTesting\.RobustIntegrationTest\.IntegrationInstance\.WaitAssertion\(Action assertion\) in " + INTEGRATION_FILE,
] + [r"at " + re.escape(frame) for frame in (
    "NUnit.Framework.Internal.TaskAwaitAdapter.GenericAdapter`1.BlockUntilCompleted()",
    "NUnit.Framework.Internal.MessagePumpStrategy.NoMessagePumpStrategy.WaitForCompletion(AwaitAdapter awaiter)",
    "NUnit.Framework.Internal.AsyncToSyncAdapter.Await[TResult](TestExecutionContext context, Func`1 invoke)",
    "NUnit.Framework.Internal.AsyncToSyncAdapter.Await(TestExecutionContext context, Func`1 invoke)",
    "NUnit.Framework.Internal.Commands.TestMethodCommand.RunTestMethod(TestExecutionContext context)",
    "NUnit.Framework.Internal.Commands.TestMethodCommand.Execute(TestExecutionContext context)",
    "NUnit.Framework.Internal.Execution.SimpleWorkItem.<>c__DisplayClass3_0.<PerformWork>b__0()",
    "NUnit.Framework.Internal.ContextUtils.<>c__DisplayClass1_0`1.<DoIsolated>b__0(Object _)",
)]
FRAMES = [re.compile(pattern) for pattern in FRAME_PATTERNS]
ASYNC_BOUNDARY = "--- End of stack trace from previous location ---"
NEGATIVE_FOOTER = re.compile(
    r'Test Run Failed\.\nTotal tests: 1\n +Failed: 1\n Total time: [0-9]+(?:\.[0-9]+)? Seconds\n'
    r' +1>Project "(?P<project>/[^"\n]+/Content\.IntegrationTests/Content\.IntegrationTests\.csproj)" on node 1 \(VSTest target\(s\)\)\.\n'
    r' +1>_VSTestConsole:\n +MSB4181: The "VSTestTask" task returned false but did not log an error\.\n'
    r' +1>Done Building Project "(?P=project)" \(VSTest target\(s\)\) -- FAILED\.\n\n'
    r'Build FAILED\.\n +0 Warning\(s\)\n +0 Error\(s\)\n\nTime Elapsed [0-9]{2}:[0-9]{2}:[0-9]{2}\.[0-9]{2}\n?\Z')

def diagnostic_log(log, mode):
    normalized = log.replace("\r\n", "\n")
    matches = list(NEGATIVE_FOOTER.finditer(normalized))
    require(len(matches) <= 1, "duplicate VSTest negative footer")
    if matches:
        require(mode == "baseline", "negative footer in positive run")
        # This exact VSTest task epilogue denotes the already checked test failure.
        normalized = normalized[:matches[0].start()]
    return normalized

def aggregate_stack(text, blocks):
    matches = list(re.finditer(r"(?m)^([0-9]+)\)\s+(?=at )", text))
    if not matches:
        check_stack(text)
        return
    require(len(matches) == len(blocks), "wrong numbered assertion stack count")
    check_stack(text[:matches[0].start()])
    for index, match in enumerate(matches):
        require(int(match.group(1)) == index + 1, "wrong numbered assertion stack order")
        end = matches[index + 1].start() if index + 1 < len(matches) else len(text)
        stack = text[match.end():end]
        embedded = blocks[index].splitlines()
        first = next((i for i, line in enumerate(embedded) if line.strip().startswith("at ")), len(embedded))
        expected = "\n".join(line.strip() for line in embedded[first:] if line.strip())
        actual = "\n".join(line.strip() for line in stack.splitlines() if line.strip())
        require(expected and actual == expected, "numbered stack differs from corresponding assertion")
        check_stack(stack)


def runner_stdout_log(log, mode, stdout):
    header = "  Standard Output Messages:\n"
    require(log.count(header) <= 1, "duplicate adapter stdout section")
    if header not in log:
        return log
    require(mode == "baseline", "unexpected adapter stdout section")
    before, after = log.split(header)
    match = re.fullmatch(r"(?P<capture>[\s\S]*?)\n\nResults File: /[^\n]+/typesof-results/typesof\.trx\n(?P<tail>[\s\S]*)", after)
    require(match is not None, "malformed adapter stdout boundary")
    lines = match['capture'].splitlines()
    require(all(not line or line.startswith(" ") for line in lines), "wrong adapter stdout indentation")
    capture = "\n".join(line[1:] if line else line for line in lines).strip()
    require(capture == stdout.strip(), "adapter stdout reprint differs from original TRX")
    return before + "\nResults File: /adapter/typesof-results/typesof.trx\n" + match['tail']


def require(value, message):
    if not value:
        raise AssertionError(message)


def check_spec():
    expected = [f"TYPESOF_REJECT_{route}_{kind}_{position}" for route in ("caught", "unwind")
                for kind in ("type", "proc", "verb") for position in ("first", "middle", "last")]
    require(len(SPEC) == 18 and [case["id"] for case in SPEC] == expected, "case specification order/identity drift")
    for case in SPEC:
        require(case["iterations"] == 1000 and case["owners"] == [1,1,1]
                and case["baseline_extra_lists"] == 1000 and case["candidate_extra_lists"] == 0,
                "case expectations drift")
        require(case["contents_groups"] == {"first":0,"middle":1,"last":3}[case["position"]], "content oracle drift")


def check_stack(text, require_fixture=True):
    text = text.replace("\\", "/")
    lines = [line.strip() for line in text.splitlines() if line.strip()]
    require(lines, "missing assertion stack")
    require(lines.count(ASYNC_BOUNDARY) <= 1, "duplicate async boundary")
    for line in lines:
        if line == ASYNC_BOUNDARY:
            continue
        require(any(pattern.fullmatch(line) for pattern in FRAMES), "foreign/unknown assertion stack frame: " + line)
    if require_fixture:
        require(any(CLASS + "." in line and "Content.IntegrationTests/TypesOfOwnershipTests.cs:line " in line for line in lines), "missing fixture/source frame")


def stdout_rows(text, mode, runner=False):
    lines = [line.strip() for line in text.splitlines() if line.strip().startswith("TYPESOF_")]
    if runner and mode == "baseline":
        allowed = {case["id"] + "_UNRETURNED_LISTS_1000" for case in SPEC}
        lines = [line for line in lines if line not in allowed]
    expected_count = 20 if mode == "positive" else 19
    require(len(lines) == expected_count and lines[0] == SUCCESS, "missing/extra/foreign fixture observation")
    require(text.count(SUCCESS) == 1, "wrong success marker count")
    require(text.count(MARKER) == (1 if mode == "positive" else 0), "wrong completion marker count")
    if mode == "positive": require(lines[-1] == MARKER, "completion marker order")
    rows = []
    for case, line in zip(SPEC, lines[1:19]):
        match = ROW.fullmatch(line)
        require(match is not None, "malformed rejection observation")
        identifier, owners, *numbers = match.groups()
        values = list(map(int, numbers))
        new, ref1, content, lists, expected_lists, datums, expected_datums = values
        require(identifier == case["id"] and list(map(int, owners.split(","))) == case["owners"], "identity/order/owner mismatch")
        extra = 1000 if mode == "baseline" else 0
        require(new == ref1 == content == extra and lists == expected_lists + extra and datums == expected_datums,
                "wrong new-list identities/refcounts/content or bucket delta")
        require(expected_lists >= 1 and expected_datums >= 2, "negative pristine bucket baseline")
        base = (expected_lists - 1, expected_datums - 2)
        if not rows: pristine = base
        else: require(base == pristine, "pristine bucket drift")
        rows.append((identifier, *values))
    return rows


def message_blocks(message, count=96):
    require(message.startswith("Multiple failures or warnings in test:"), "not a NUnit aggregate failure")
    remainder = message[len("Multiple failures or warnings in test:"):]
    matches = list(re.finditer(r"(?m)^\s*(\d+)\)\s*", remainder))
    require(len(matches) == count, "wrong exact assertion failure count")
    require(not remainder[:matches[0].start()].strip(), "unknown aggregate preamble")
    blocks = []
    for index, match in enumerate(matches):
        require(int(match.group(1)) == index + 1, "wrong assertion numbering/order")
        end = matches[index + 1].start() if index + 1 < len(matches) else len(remainder)
        blocks.append(remainder[match.end():end].strip())
    return blocks


def check_failure(block, marker, expression, body):
    lines = block.splitlines()
    require(lines and lines[0].strip() == marker, "unknown/missing/duplicate assertion ID")
    frame_start = next((index for index, line in enumerate(lines) if line.strip().startswith("at ")), len(lines))
    content = "\n".join(lines[1:frame_start]).strip()
    pattern = re.escape(expression) + r"\s+" + body
    require(re.fullmatch(pattern, content) is not None, "unexpected NUnit assertion message: " + marker)
    # Embedded assertion stacks may be omitted by NUnit; present frames are strictly checked.
    if frame_start < len(lines):
        check_stack("\n".join(lines[frame_start:]))


def verify(path, mode, exit_code, log):
    check_spec()
    require(mode in ("positive", "baseline"), "unknown verification mode")
    require(isinstance(log, str) and 0 < len(log.encode()) <= LIMIT, "missing/oversized original runner log")
    path = Path(path)
    require(0 < path.stat().st_size <= LIMIT, "empty/oversized TRX")
    tree = ET.parse(path)
    require(not ERROR.search(diagnostic_log(log, mode) + "\n" + "\n".join(tree.getroot().itertext())), "runtime/compile/runner error")
    rows = tree.findall(".//t:UnitTestResult", NS)
    counters_nodes = tree.findall(".//t:ResultSummary/t:Counters", NS)
    require(len(rows) == 1 and len(counters_nodes) == 1, "missing/extra result or counters")
    row, counters = rows[0], counters_nodes[0]
    allowed_counters = {"total", "executed", "passed", "failed", "error", "timeout", "aborted", "inconclusive", "notRunnable", "notExecuted", "disconnected", "warning", "passedButRunAborted", "inProgress", "pending", "completed"}
    require(set(counters.attrib).issubset(allowed_counters) and all(re.fullmatch(r"\d+", value) for value in counters.attrib.values()), "unknown/non-numeric runner counter")
    require(counters.get("completed", "1") in ("0", "1"), "wrong completed counter")
    require(row.get("testName") == TEST, "wrong test result identity")
    definitions = tree.findall(".//t:TestDefinitions/t:UnitTest", NS)
    require(len(definitions) == 1 and definitions[0].get("id") == row.get("testId"), "wrong/missing/extra definition")
    methods = definitions[0].findall("t:TestMethod", NS)
    require(len(methods) == 1 and methods[0].get("className") == CLASS and methods[0].get("name") == TEST, "wrong fixture/method")
    require(counters.get("total") == counters.get("executed") == "1", "not exactly one executed test")
    for key in ("error", "timeout", "aborted", "inconclusive", "notRunnable", "notExecuted", "disconnected", "warning", "passedButRunAborted", "inProgress", "pending"):
        require(int(counters.get(key, "0")) == 0, "runner/skipped counter: " + key)
    require(not tree.findall(".//t:RunInfo[@outcome='Error']", NS) and not tree.findall(".//t:RunInfo[@outcome='Warning']", NS), "runner error/warning record")
    require(not any((node.text or "").strip() for node in tree.findall(".//t:StdErr", NS)), "unexpected stderr")
    outcome = "Passed" if mode == "positive" else "Failed"
    require(exit_code == (0 if mode == "positive" else 1) and row.get("outcome") == outcome, "wrong exit/outcome")
    require(counters.get("passed") == ("1" if mode == "positive" else "0") and counters.get("failed") == ("0" if mode == "positive" else "1"), "wrong pass/fail counters")
    result_lines = re.findall(r"(?m)^\s*(Passed|Failed|Skipped|NotExecuted)\s+([^\r\n]+)$", log)
    require(len(result_lines) == 1 and result_lines[0][0] == outcome and re.fullmatch(re.escape(TEST) + r"(?:\s+\[[^\r\n]+\])?", result_lines[0][1].strip()), "missing/extra/foreign runner test result")
    summary = "Test Run Successful." if mode == "positive" else "Test Run Failed."
    require(log.count(summary) == 1 and log.count("Test Run Successful.") + log.count("Test Run Failed.") == 1, "wrong/duplicate runner summary")
    stdout_nodes = row.findall("t:Output/t:StdOut", NS)
    require(len(stdout_nodes) == 1, "missing/extra stdout")
    observations = stdout_rows(stdout_nodes[0].text or "", mode)
    require(stdout_rows(runner_stdout_log(log, mode, stdout_nodes[0].text or ""), mode, runner=True) == observations, "runner log differs from original TRX stdout observations")
    allowed_markers = {case["id"] for case in SPEC} | {"TYPESOF_SUCCESS"}
    if mode == "baseline":
        allowed_markers |= {case["id"] + "_UNRETURNED_LISTS_1000" for case in SPEC}
        for case in SPEC:
            require(log.count(case["id"] + "_UNRETURNED_LISTS_1000") == 1, "missing/duplicate log assertion ID")
    else: allowed_markers.add("TYPESOF_COMPLETE")
    for marker in re.findall(r"\bTYPESOF_[A-Za-z0-9_]+", log):
        require(marker in allowed_markers, "unknown/foreign fixture marker in original log")
    for line in log.replace("\\", "/").splitlines():
        frame = re.sub(r"^[0-9]+\)\s+(?=at )", "", line.strip())
        if frame.startswith("at "):
            check_stack(frame, require_fixture=False)
    errors = tree.findall(".//t:ErrorInfo", NS)
    if mode == "positive":
        require(not errors, "positive run has assertion failures")
    else:
        require(len(errors) == 1 and errors[0] is row.find("t:Output/t:ErrorInfo", NS), "not exactly one dedicated failure aggregate")
        error = errors[0]
        require(len(error.findall("t:Message", NS)) == len(error.findall("t:StackTrace", NS)) == 1, "missing/extra failure message/stack")
        require(log.count("  Error Message:\n") == log.count("  Stack Trace:\n") == 1, "missing/duplicate adapter failure sections")
        logged_message = log.split("  Error Message:\n")[1].split("  Stack Trace:\n")[0]
        normalize = lambda text: "\n".join(line.strip() for line in text.splitlines() if line.strip())
        require(normalize(logged_message) == normalize(error.findtext("t:Message", "", NS)), "original log failure message differs from TRX")
        blocks = message_blocks(error.findtext("t:Message", "", NS), 18)
        for case, block in zip(SPEC, blocks):
            check_failure(block, case["id"] + "_UNRETURNED_LISTS_1000", "Assert.That(fresh.Length, Is.EqualTo(0))",
                          r"Expected:\s+0\s+But was:\s+1000")
        aggregate_stack(error.findtext("t:StackTrace", "", NS), blocks)
    return True


if __name__ == "__main__":
    parser = argparse.ArgumentParser()
    parser.add_argument("trx", type=Path)
    parser.add_argument("mode", choices=["positive", "baseline"])
    parser.add_argument("exit_code", type=int)
    parser.add_argument("--log", type=Path, required=True)
    args = parser.parse_args()
    verify(args.trx, args.mode, args.exit_code, args.log.read_text())
    print("Strict dedicated construction TRX and original log accepted; this classifier does not create runtime evidence")
