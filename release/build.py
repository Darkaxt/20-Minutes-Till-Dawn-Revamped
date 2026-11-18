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
plugin = args.scratch / 'bin/Modernization/netstandard2.0/TwentyMinutes.Modernization.dll'
inputs = list((ROOT / 'src').glob('*.cs')) + list((ROOT / 'src').glob('*.csproj')) + [ROOT / 'Directory.Build.props']
if not plugin.is_file() or any(path.stat().st_mtime_ns > plugin.stat().st_mtime_ns for path in inputs):
    raise SystemExit('Run the managed Release build against the complete current source before packaging; this command never launches an implicit compiler')
stage = args.output / '20-Minutes-Till-Dawn-Revamped-1.4.1'
(stage / 'Mod').mkdir(parents=True)
shutil.copy2(plugin, stage / 'Mod' / plugin.name)
shutil.copytree(ROOT / 'installer', stage / 'installer', ignore=shutil.ignore_patterns('__pycache__', '*.pyc'))
shutil.copytree(ROOT / 'docs', stage / 'docs')
# Keep the README's release-note and updater-instruction links usable offline.
for note in sorted((ROOT / 'release').glob('NOTES-*.md')):
    (stage / 'release').mkdir(exist_ok=True)
    shutil.copy2(note, stage / 'release' / note.name)
(stage / 'android-updater').mkdir()
shutil.copy2(ROOT / 'android-updater/README.md', stage / 'android-updater/README.md')
for name in ('README.md', 'LICENSE', 'INSTALL.md'):
    shutil.copy2(ROOT / name, stage / name)
contract = asset_contract(args.prepared_assets)
appearance = json.loads((args.prepared_assets / 'cosmetics/manifest.json').read_text(encoding='utf8'))
if len(appearance['items']) != 147 or len(contract) != 593 + 1 + len(appearance['sprites']):
    raise SystemExit('Prepared assets must contain the complete existing gameplay set and the locally prepared 147-entry appearance catalog')
release = {'product': '20 Minutes Till Dawn - Revamped', 'version': '1.4.1', 'source_commit': revision,
           'candidate': args.candidate, 'plugin_sha256': digest(plugin), 'assets': contract,
           'asset_delivery': 'Prepared locally from pinned owned inputs; no extracted game art/data in this archive'}
(stage / 'RELEASE.json').write_text(json.dumps(release, indent=2) + '\n', encoding='utf8', newline='\n')
# Public hashes of the complete installed payload let the Android client
# identify updates requiring locally prepared assets before changing files.
# Only our compiled plugin is carried here; original game assets remain local.
loader = args.prepared_assets.parent / 'BepInEx'
installed = {'winhttp.dll': digest(loader / 'winhttp.dll'), 'doorstop_config.ini': digest(loader / 'doorstop_config.ini')}
installed.update({p.relative_to(loader).as_posix(): digest(p) for p in (loader / 'BepInEx/core').rglob('*') if p.is_file()})
installed.update({'BepInEx/plugins/20MTDModernization/Assets/' + p.relative_to(args.prepared_assets).as_posix(): digest(p)
                  for p in args.prepared_assets.rglob('*') if p.is_file()})
plugin_path = 'BepInEx/plugins/20MTDModernization/TwentyMinutes.Modernization.dll'
installed[plugin_path] = digest(plugin)
if len(installed) != len(contract) + 21:
    raise SystemExit('Updater contract requires the complete artwork, loader and plugin payload')
(stage / 'UPDATE.json').write_text(json.dumps({'schema': 2, 'minimum_updater': '0.2.0', 'save_epoch': 1, 'product': '20MTD-Revamped', 'version': '1.4.1',
    'source_commit': revision, 'files': installed, 'payload': {plugin_path: 'Mod/' + plugin.name}}, indent=2) + '\n', encoding='utf8', newline='\n')
files = {p.relative_to(stage).as_posix(): digest(p) for p in sorted(stage.rglob('*')) if p.is_file()}
(stage / 'PACKAGE.json').write_text(json.dumps({'version': '1.4.1', 'source_commit': revision, 'files': files}, indent=2), encoding='utf8', newline='\n')
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
