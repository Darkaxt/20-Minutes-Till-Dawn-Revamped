"""Build the owned-code package; extracted assets remain outside the archive."""
import argparse
import hashlib
import json
import os
import pathlib
import shutil
import subprocess
import sys
import zipfile

ROOT = pathlib.Path(__file__).resolve().parent.parent
sys.path.insert(0, str(ROOT / 'installer'))
from install import asset_contract, digest

parser = argparse.ArgumentParser(description=__doc__)
parser.add_argument('--scratch', required=True, type=pathlib.Path)
parser.add_argument('--prepared-assets', required=True, type=pathlib.Path)
parser.add_argument('--output', required=True, type=pathlib.Path)
parser.add_argument('--candidate', action='store_true')
args = parser.parse_args()
if args.output.exists():
    raise SystemExit('Choose a new exact package output directory')
revision = subprocess.check_output(['git', 'rev-parse', 'HEAD'], cwd=ROOT, text=True).strip()
if not args.candidate and subprocess.check_output(['git', 'status', '--porcelain'], cwd=ROOT, text=True).strip():
    raise SystemExit('Final package requires committed, verified source')
env = os.environ.copy()
env.update(TEMP=str(args.scratch), TMP=str(args.scratch), MODERNIZATION_SCRATCH=str(args.scratch),
           DOTNET_CLI_HOME=str(args.scratch / 'dotnet'), DOTNET_GENERATE_ASPNET_CERTIFICATE='false')
subprocess.run(['dotnet', 'build', str(ROOT / 'src/Modernization.csproj'), '-c', 'Release', '--no-restore',
                '--nologo', '-v:minimal', '-nodeReuse:false'], cwd=ROOT, env=env, check=True)
stage = args.output / '20-Minutes-Till-Dawn-Revamped-1.1.0'
(stage / 'Mod').mkdir(parents=True)
plugin = args.scratch / 'bin/Modernization/netstandard2.0/TwentyMinutes.Modernization.dll'
shutil.copy2(plugin, stage / 'Mod' / plugin.name)
shutil.copytree(ROOT / 'installer', stage / 'installer', ignore=shutil.ignore_patterns('__pycache__', '*.pyc'))
shutil.copytree(ROOT / 'docs', stage / 'docs')
for name in ('README.md', 'LICENSE', 'INSTALL.md'):
    shutil.copy2(ROOT / name, stage / name)
contract = asset_contract(args.prepared_assets)
if len(contract) != 593:
    raise SystemExit('Prepared asset contract must contain the complete 593-file local runtime set, including affixes, achievement icons/catalog, source display/camera data and both gameplay sounds with provenance')
release = {'product': '20 Minutes Till Dawn - Revamped', 'version': '1.1.0', 'source_commit': revision,
           'candidate': args.candidate, 'plugin_sha256': digest(plugin), 'assets': contract,
           'asset_delivery': 'Prepared locally from pinned owned inputs; no extracted game art/data in this archive'}
(stage / 'RELEASE.json').write_text(json.dumps(release, indent=2) + '\n', encoding='utf8', newline='\n')
files = {p.relative_to(stage).as_posix(): digest(p) for p in sorted(stage.rglob('*')) if p.is_file()}
(stage / 'PACKAGE.json').write_text(json.dumps({'version': '1.1.0', 'source_commit': revision, 'files': files}, indent=2), encoding='utf8', newline='\n')
archive = args.output / (stage.name + '.zip')
with zipfile.ZipFile(archive, 'w', compression=zipfile.ZIP_DEFLATED, compresslevel=9) as package:
    for path in sorted(stage.rglob('*')):
        if path.is_file():
            entry = zipfile.ZipInfo(stage.name + '/' + path.relative_to(stage).as_posix(), (1980, 1, 1, 0, 0, 0))
            entry.compress_type = zipfile.ZIP_DEFLATED
            package.writestr(entry, path.read_bytes())
(args.output / 'SHA256SUMS.txt').write_text(digest(archive) + '  ' + archive.name + '\n', encoding='ascii', newline='\n')
print(json.dumps({'archive': str(archive), 'sha256': digest(archive), 'source_commit': revision,
                  'candidate': args.candidate, 'public_files': len(files) + 1}, indent=2))
