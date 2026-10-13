"""Real ImageSharp image-owner regression around production icon cache methods."""
import argparse
import hashlib
import json
import os
from pathlib import Path
import subprocess
import tempfile
import xml.etree.ElementTree as ET

ROOT = Path(__file__).resolve().parents[2]
HERE = Path(__file__).resolve().parent


def body(source, signature):
    start = source.index(signature)
    opening = source.index('{', start)
    level, end = 1, opening + 1
    while level:
        level += (source[end] == '{') - (source[end] == '}')
        end += 1
    return source[start:end]


def check(dotnet, output, control):
    files = ['OpenDreamRuntime/Resources/DreamResourceManager.cs', 'OpenDreamRuntime/Resources/IconResource.cs']
    sources = {name: (ROOT / name).read_text() for name in files}
    props = ROOT / 'RobustToolbox/Directory.Packages.props'
    versions = [node.attrib['Version'] for node in ET.parse(props).iter('PackageVersion')
                if node.attrib.get('Include') == 'SixLabors.ImageSharp']
    assert versions == ['3.1.12'], 'Revisit ownership check when installed dependency changes'
    texture = body(sources[files[0]], 'public IconResource CreateIconResource(byte[] data, Image<Rgba32> texture,')
    disposal = 'if (!possibleDuplicate.OwnsTexture(texture))\n                    texture.Dispose();'
    assert texture.count(disposal) == 1
    icon = sources[files[1]]
    if control == 'omit-disposal':
        texture = texture.replace(disposal, '')
    elif control == 'blind-disposal':
        texture = texture.replace(disposal, 'texture.Dispose();')
    elif control == 'lazy-decode':
        assert icon.count('ReferenceEquals(_texture, texture)') == 1
        icon = icon.replace('ReferenceEquals(_texture, texture)', 'ReferenceEquals(Texture, texture)')
    elif control == 'collision-guard':
        assert texture.count('if (data.SequenceEqual(possibleDuplicate.ResourceData))') == 1
        texture = texture.replace('if (data.SequenceEqual(possibleDuplicate.ResourceData))', 'if (true)')
    methods = '\n'.join([texture,
                         body(sources[files[0]], 'public IconResource CreateIconResource(byte[] data)'),
                         body(sources[files[0]], 'private string CalculateMd5(')])
    with tempfile.TemporaryDirectory(prefix='opendream-icon-owners-') as directory:
        temp = Path(directory)
        (temp / 'Probe.csproj').write_text('<Project Sdk="Microsoft.NET.Sdk"><PropertyGroup><OutputType>Exe</OutputType>'
            '<TargetFramework>net10.0</TargetFramework><Nullable>enable</Nullable><ImplicitUsings>enable</ImplicitUsings>'
            '</PropertyGroup><ItemGroup><PackageReference Include="SixLabors.ImageSharp" Version="3.1.12" />'
            '</ItemGroup></Project>')
        (temp / 'NuGet.Config').write_text('<configuration><packageSources><clear/>'
            '<add key="nuget.org" value="https://api.nuget.org/v3/index.json" /></packageSources></configuration>')
        for name in ('Program.cs', 'Stubs.cs'):
            (temp / name).write_bytes((HERE / name).read_bytes())
        (temp / 'IconResource.cs').write_text(icon)
        (temp / 'ProductionManager.cs').write_text('using System.Security.Cryptography; using System.Text; '
            'using SixLabors.ImageSharp; using SixLabors.ImageSharp.PixelFormats; using OpenDreamShared.Resources; '
            'namespace OpenDreamRuntime.Resources; public sealed partial class DreamResourceManager { ' + methods + ' }')
        env = dict(os.environ, DOTNET_CLI_HOME=str(temp / 'cli'), DOTNET_SKIP_FIRST_TIME_EXPERIENCE='1',
                   DOTNET_CLI_TELEMETRY_OPTOUT='1', DOTNET_NOLOGO='1', MSBUILDDISABLENODEREUSE='1')
        commands = [[dotnet, 'restore', str(temp / 'Probe.csproj'), '--configfile', str(temp / 'NuGet.Config'), '--disable-parallel'],
                    [dotnet, 'build', str(temp / 'Probe.csproj'), '-c', 'Release', '--no-restore', '--disable-build-servers',
                     '-m:1', '-p:UseSharedCompilation=false'],
                    [dotnet, str(temp / 'bin/Release/net10.0/Probe.dll')]]
        for index, command in enumerate(commands):
            result = subprocess.run(command, cwd=temp, env=env, capture_output=True, text=True, timeout=120)
            log = result.stdout + result.stderr
            (output / f'{control or "positive"}-{index}.log').write_text(log)
            if index < 2:
                assert result.returncode == 0, 'Preparation/build failure is not ownership evidence: ' + log
        expected = {'omit-disposal': 'equal-live incoming owners must be released immediately',
                    'blind-disposal': 'cached texture alias must not be disposed',
                    'lazy-decode': 'duplicate ownership check must not decode lazy cached texture',
                    'collision-guard': 'unequal collision bytes must adopt unique image'}
        if control:
            assert result.returncode == 1 and 'REGRESSION: ' + expected[control] in log, log
        else:
            assert result.returncode == 0 and 'PASS 1000 equal-live duplicates' in log, log
        receipt = {'head': subprocess.check_output(['git', '-c', f'safe.directory={ROOT.as_posix()}', 'rev-parse', 'HEAD'], cwd=ROOT, text=True).strip(),
                   'sources': {name: hashlib.sha256((ROOT / name).read_bytes()).hexdigest() for name in files},
                   'harness': {name: hashlib.sha256((HERE / name).read_bytes()).hexdigest() for name in ('Program.cs', 'Stubs.cs', 'run.py')},
                   'dependency': 'SixLabors.ImageSharp 3.1.12', 'control': control,
                   'result': 'expected ownership failure detected' if control else 'passed',
                   'scope': 'Real ImageSharp allocations/disposal and production icon cache methods/full IconResource with inert resource/provider/DMI adapters; no game or forced GC'}
        if not control:
            receipt['equal_live_samples'] = json.loads(log.splitlines()[-1])
        (output / f'{control or "positive"}-receipt.json').write_text(json.dumps(receipt, indent=2))
        print(json.dumps({'control': control, 'result': receipt['result']}))


if __name__ == '__main__':
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument('--dotnet', required=True)
    parser.add_argument('--output', required=True, type=Path)
    args = parser.parse_args()
    args.output.mkdir(parents=True, exist_ok=True)
    for control in (None, 'omit-disposal', 'blind-disposal', 'lazy-decode', 'collision-guard'):
        check(args.dotnet, args.output, control)
