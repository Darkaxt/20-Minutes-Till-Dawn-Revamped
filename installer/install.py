"""Versioned local preparation/install/verify/uninstall; never uploads game data."""
import argparse
import hashlib
import json
import os
import pathlib
import shutil
import subprocess
import sys
import urllib.request
import venv
import zipfile

HERE = pathlib.Path(__file__).resolve().parent
ROOT = HERE.parent
VERSION = '1.1.0'
APKM_SHA256 = 'c2336baac8f71d9c974e6e7b120225de0fb441463aecf4c1c6bfd7e2f6021d8f'
MOD = pathlib.PurePosixPath('BepInEx/plugins/20MTDModernization')


def digest(path):
    with pathlib.Path(path).open('rb') as stream:
        return hashlib.file_digest(stream, 'sha256').hexdigest()


def atomic_json(path, value):
    path.parent.mkdir(parents=True, exist_ok=True)
    temp = path.with_name(path.name + '.writing')
    with temp.open('w', encoding='utf8', newline='\n') as stream:
        json.dump(value, stream, indent=2)
        stream.flush()
        os.fsync(stream.fileno())
    os.replace(temp, path)


def member(game, relative):
    name = pathlib.PurePosixPath(relative.replace('\\', '/'))
    if name.is_absolute() or '..' in name.parts or ':' in relative:
        raise ValueError('Unsafe installation member: ' + relative)
    path = game.joinpath(*name.parts)
    if not path.resolve().is_relative_to(game):
        raise ValueError('Installation member leaves the chosen game directory')
    return path


def stock(game):
    expected = json.loads((HERE / 'supported-windows.json').read_text())
    wrong = [name for name, sha in expected.items() if not member(game, name).is_file() or digest(member(game, name)) != sha]
    if wrong:
        raise ValueError('Unsupported or modified Windows build; no installation files changed: ' + ', '.join(wrong[:8]))
    return {'build': 'Steam 12679152 / Unity 2019.4.40f1 Mono x64', 'verified_original_files': len(expected)}


def work_directory(path, game):
    path = path.resolve()
    if path == pathlib.Path(path.anchor) or path == game or path.is_relative_to(game) or game.is_relative_to(path):
        raise ValueError('Choose a separate exact scratch directory, outside the game')
    owner = path / 'revamped-work.json'
    if path.exists() and any(path.iterdir()) and not owner.is_file():
        raise ValueError('Scratch directory is nonempty and is not owned by this installer')
    path.mkdir(parents=True, exist_ok=True)
    if owner.is_file() and json.loads(owner.read_text()).get('product') != '20MTD-Revamped-installer':
        raise ValueError('Scratch directory owner mismatch')
    atomic_json(owner, {'product': '20MTD-Revamped-installer', 'version': VERSION})
    return path


def runtime(work, args):
    python = work / 'runtime/Scripts/python.exe'
    requirements = digest(HERE / 'requirements.txt')
    stamp = work / 'requirements.json'
    environment = os.environ.copy()
    environment.update(TEMP=str(work), TMP=str(work), PIP_CACHE_DIR=str(work / 'pip-cache'),
                       PYTHONPYCACHEPREFIX=str(work / 'pycache'), DOTNET_CLI_HOME=str(work / 'dotnet'),
                       DOTNET_ROLL_FORWARD='Major', DOTNET_GENERATE_ASPNET_CERTIFICATE='false')
    os.environ.update({key: environment[key] for key in ('TEMP', 'TMP', 'PIP_CACHE_DIR', 'PYTHONPYCACHEPREFIX', 'DOTNET_CLI_HOME')})
    if not python.is_file():
        venv.EnvBuilder(with_pip=True).create(work / 'runtime')
    if not stamp.is_file() or json.loads(stamp.read_text()).get('sha256') != requirements:
        subprocess.run([str(python), '-m', 'pip', 'install', '--disable-pip-version-check', '--no-input', '--no-compile',
                        '-r', str(HERE / 'requirements.txt')], env=environment, check=True)
        atomic_json(stamp, {'sha256': requirements})
    return subprocess.call([str(python), str(__file__), *args, '--runtime-ready'], env=environment)


def download(pin, work, name):
    path = work / 'downloads' / name
    path.parent.mkdir(exist_ok=True)
    if not path.exists():
        writing = path.with_name(path.name + '.writing')
        with urllib.request.urlopen(urllib.request.Request(pin['url'], headers={'User-Agent': '20MTD-Revamped/1.1.0'})) as response, writing.open('wb') as output:
            shutil.copyfileobj(response, output)
        if digest(writing) != pin['sha256']:
            raise ValueError('Downloaded dependency digest mismatch: ' + name)
        os.replace(writing, path)
    if digest(path) != pin['sha256']:
        raise ValueError('Cached dependency digest mismatch: ' + name)
    return path


def extract_zip(archive, target):
    with zipfile.ZipFile(archive) as package:
        for info in package.infolist():
            dest = member(target.resolve(), info.filename)
            if info.is_dir():
                dest.mkdir(parents=True, exist_ok=True)
            else:
                dest.parent.mkdir(parents=True, exist_ok=True)
                with package.open(info) as source, dest.open('wb') as output:
                    shutil.copyfileobj(source, output)


def prepare(android, game, work):
    if digest(android) != APKM_SHA256:
        raise ValueError('Unsupported Android input: this release requires the exact 8.6 (1093) APKM listed in INSTALL.md')
    from asset_builder import android_assets, style_assets
    pins = json.loads((HERE / 'pins.json').read_text())
    identity = {name: digest(HERE / name) for name in ('asset_builder.py', 'style-recipe.json', 'pins.json', 'requirements.txt')}
    identity['android'] = APKM_SHA256
    ready, output = work / 'prepared-assets.json', work / 'Assets'
    if ready.is_file():
        saved = json.loads(ready.read_text())
        if saved['inputs'] != identity or any(not member(output, name).is_file() or digest(member(output, name)) != sha for name, sha in saved['files'].items()):
            raise ValueError('Prepared assets changed; choose a new exact scratch directory')
        return output
    if output.exists() and any(output.iterdir()):
        raise ValueError('An incomplete asset preparation is preserved; choose another scratch directory to prepare again')
    output.mkdir(exist_ok=True)
    dumper_zip = download(pins['Perfare/Il2CppDumper'], work, 'Il2CppDumper.zip')
    dumper = work / 'dumper'
    extract_zip(dumper_zip, dumper)
    config = json.loads((dumper / 'config.json').read_text())
    config['RequireAnyKey'] = False
    atomic_json(dumper / 'config.json', config)
    with zipfile.ZipFile(android) as outer:
        import io
        with zipfile.ZipFile(io.BytesIO(outer.read('base.apk'))) as base:
            (work / 'global-metadata.dat').write_bytes(base.read('assets/bin/Data/Managed/Metadata/global-metadata.dat'))
        with zipfile.ZipFile(io.BytesIO(outer.read('split_config.arm64_v8a.apk'))) as arm:
            (work / 'libil2cpp.so').write_bytes(arm.read('lib/arm64-v8a/libil2cpp.so'))
    subprocess.run(['dotnet', str(dumper / 'Il2CppDumper.dll'), str(work / 'libil2cpp.so'),
                    str(work / 'global-metadata.dat'), str(dumper)], cwd=work, check=True,
                   creationflags=subprocess.CREATE_NO_WINDOW if os.name == 'nt' else 0)
    android_assets(android, dumper / 'DummyDll', output)
    upstream = download(pins['SpritesReborn'], work, 'SpritesReborn.zip')
    style_assets(game / 'MinutesTillDawn_Data', upstream, output)
    files = {p.relative_to(output).as_posix(): digest(p) for p in sorted(output.rglob('*')) if p.is_file()}
    atomic_json(ready, {'inputs': identity, 'files': files})
    return output


def manifest_paths(game):
    return member(game, str(MOD / 'install-manifest.json')), member(game, str(MOD / 'install-pending.json'))


def read_manifest(path):
    data = json.loads(path.read_text())
    if data.get('product') != '20MTD-Revamped':
        raise ValueError('Installation owner mismatch')
    seen = set()
    for row in data['files']:
        name = row['path']
        allowed = (name in ('winhttp.dll', 'doorstop_config.ini', str(MOD / 'TwentyMinutes.Modernization.dll')) or
                   name.startswith('BepInEx/core/') or name.startswith(str(MOD / 'Assets') + '/'))
        if not allowed or name in seen or not isinstance(row['owned'], bool):
            raise ValueError('Manifest member is outside the mod/loader ownership contract: ' + name)
        seen.add(name)
    for name in data.get('created_directories', []):
        if not (name in ('BepInEx', 'BepInEx/core', 'BepInEx/plugins', str(MOD)) or
                name.startswith('BepInEx/core/') or name.startswith(str(MOD / 'Assets'))):
            raise ValueError('Directory is outside installation ownership: ' + name)
    return data


def asset_contract(assets):
    from PIL import Image
    result = {}
    for path in sorted(assets.rglob('*')):
        if not path.is_file():
            continue
        if path.suffix == '.png':
            image = Image.open(path).convert('RGBA')
            value = {'kind': 'rgba', 'size': list(image.size), 'sha256': hashlib.sha256(image.tobytes()).hexdigest()}
        elif path.suffix == '.json':
            value = {'kind': 'json', 'sha256': hashlib.sha256(json.dumps(json.loads(path.read_text(encoding='utf8')), sort_keys=True, separators=(',', ':')).encode()).hexdigest()}
        elif path.suffix in ('.tsv', '.txt'):
            value = {'kind': 'text', 'sha256': hashlib.sha256(path.read_text(encoding='utf8').encode()).hexdigest()}
        else:
            value = {'kind': 'binary', 'sha256': digest(path)}
        result[path.relative_to(assets).as_posix()] = value
    return result


def verify(game):
    path, pending = manifest_paths(game)
    if pending.exists() or not path.is_file():
        raise ValueError('Installation is incomplete or absent; run install to complete an interrupted matching installation')
    data = read_manifest(path)
    wrong = [r['path'] for r in data['files'] if not member(game, r['path']).is_file() or digest(member(game, r['path'])) != r['sha256']]
    if wrong:
        raise ValueError('Installed files changed or missing: ' + ', '.join(wrong[:8]))
    return {'version': data['version'], 'source_commit': data['source_commit'], 'verified_files': len(data['files']), 'game': str(game)}


def install(game, assets, work):
    release = json.loads((ROOT / 'RELEASE.json').read_text())
    plugin = ROOT / 'Mod/TwentyMinutes.Modernization.dll'
    if release['version'] != VERSION or digest(plugin) != release['plugin_sha256']:
        raise ValueError('Release plugin does not match its manifest')
    if asset_contract(assets) != release['assets']:
        raise ValueError('Prepared asset pixels/contracts do not match this verified release')
    path, pending_path = manifest_paths(game)
    if path.is_file():
        result = verify(game)
        if result['version'] != VERSION or result['source_commit'] != release['source_commit']:
            raise ValueError('Another Revamped release is installed; uninstall it first, preserving saves/settings')
        return result
    pins = json.loads((HERE / 'pins.json').read_text())
    bep = work / 'BepInEx'
    extract_zip(download(pins['BepInEx/BepInEx'], work, 'BepInEx.zip'), bep)
    sources = {'winhttp.dll': bep / 'winhttp.dll', 'doorstop_config.ini': bep / 'doorstop_config.ini'}
    sources.update({p.relative_to(bep).as_posix(): p for p in (bep / 'BepInEx/core').rglob('*') if p.is_file()})
    sources[str(MOD / plugin.name)] = plugin
    sources.update({str(MOD / 'Assets' / p.relative_to(assets).as_posix()): p for p in assets.rglob('*') if p.is_file()})
    pending = read_manifest(pending_path) if pending_path.is_file() else None
    prior = {r['path']: r for r in pending['files']} if pending else {}
    files = []
    for name, source in sorted(sources.items()):
        target, sha = member(game, name), digest(source)
        if target.exists() and (not target.is_file() or digest(target) != sha):
            raise ValueError('Conflicting existing file preserved: ' + name)
        files.append({'path': name, 'sha256': sha, 'owned': prior[name]['owned'] if name in prior else not target.exists()})
    directories = set()
    for name in sources:
        parent = member(game, name).parent
        while parent != game:
            if not parent.exists():
                directories.add(parent.relative_to(game).as_posix())
            parent = parent.parent
    data = {'product': '20MTD-Revamped', 'version': VERSION, 'source_commit': release['source_commit'], 'files': files,
            'created_directories': pending['created_directories'] if pending else sorted(directories)}
    if pending and pending != data:
        raise ValueError('Interrupted installation belongs to a different file manifest')
    atomic_json(pending_path, data)
    for row in files:
        target = member(game, row['path'])
        if target.exists():
            continue
        target.parent.mkdir(parents=True, exist_ok=True)
        writing = target.with_name(target.name + '.revamped-writing')
        shutil.copyfile(sources[row['path']], writing)
        if digest(writing) != row['sha256']:
            raise ValueError('Staged installation digest mismatch')
        os.replace(writing, target)
    atomic_json(path, data)
    pending_path.unlink()
    return verify(game)


def uninstall(game):
    path, pending = manifest_paths(game)
    source = path if path.is_file() else pending
    if not source.is_file():
        raise ValueError('No Revamped owned-file manifest; nothing removed')
    data = read_manifest(source)
    changed = [r['path'] for r in data['files'] if r['owned'] and member(game, r['path']).exists() and digest(member(game, r['path'])) != r['sha256']]
    if changed:
        raise ValueError('Modified owned files preserved; inspect before uninstall: ' + ', '.join(changed[:8]))
    removed = 0
    for row in data['files']:
        target = member(game, row['path'])
        if row['owned'] and target.is_file():
            target.unlink()
            removed += 1
    # Saves/configuration and reused loader files are deliberately outside ownership.
    source.unlink()
    for name in sorted(data.get('created_directories', []), key=lambda n: len(pathlib.PurePosixPath(n).parts), reverse=True):
        directory = member(game, name)
        if directory.is_dir() and not any(directory.iterdir()):
            directory.rmdir()
    return {'version': data['version'], 'removed_exact_owned_files': removed, 'preserved': 'saves, settings, other mods and reused loader files'}


def main():
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument('action', choices=('prepare', 'install', 'verify', 'uninstall'))
    parser.add_argument('--game', required=True, type=pathlib.Path)
    parser.add_argument('--android', type=pathlib.Path)
    parser.add_argument('--work-dir', type=pathlib.Path, default=pathlib.Path('D:/Temp/20MTD-Revamped-Install'))
    parser.add_argument('--runtime-ready', action='store_true', help=argparse.SUPPRESS)
    args = parser.parse_args()
    game = args.game.resolve()
    if args.action == 'uninstall':
        result = uninstall(game)
    elif args.action == 'verify':
        result = {**stock(game), **verify(game)}
    else:
        stock(game)
        if args.android is None:
            parser.error('--android is required for prepare/install')
        work = work_directory(args.work_dir, game)
        if not args.runtime_ready:
            return runtime(work, sys.argv[1:])
        assets = prepare(args.android, game, work)
        result = {'prepared_assets': str(assets)} if args.action == 'prepare' else install(game, assets, work)
    print(json.dumps(result, indent=2), flush=True)
    return 0


if __name__ == '__main__':
    try:
        sys.exit(main())
    except (OSError, ValueError, subprocess.CalledProcessError) as error:
        print('Revamped: ' + str(error), file=sys.stderr)
        sys.exit(1)
