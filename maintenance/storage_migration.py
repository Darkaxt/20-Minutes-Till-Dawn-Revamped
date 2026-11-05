"""Migrate the six legacy 20MTD maintenance roots; deletion uses the installed cleanup helper."""
from pathlib import Path
from collections import Counter, defaultdict
import argparse, hashlib, importlib.util, json, os, re, shutil, stat, subprocess, sys, venv

ROOT = Path(__file__).resolve().parents[1]
EVIDENCE = Path('D:/Projects/20MTD-Modernization-evidence/storage-migration-20261105')
LEGACY = [Path('D:/Temp') / name for name in (
    '20mtd-branding-20261022', '20mtd-parity-acceptance-20261023',
    '20mtd-parity-assets-20261022-complete', '20mtd-achievement-api-20261023',
    '20mtd-pixel-api-20261023', '20mtd-droiddeck-20261022')]
PERMANENT = Path('D:/Projects/20MTD-Modernization-maintenance')
SCRATCH = Path('D:/Temp/20mtd-storage-verification-20261105')
REAL = Path('D:/Steam/steamapps/common/20MinuteTillDawn')
PROFILE = Path('C:/Users/darka/AppData/LocalLow/Flanne/MinutesTillDawn')
PUBLISHED = Path('D:/Projects/20MTD-Modernization-evidence/pair-familiarity-20261031/releases/windows-release-1.3.17-6e04bd8/20-Minutes-Till-Dawn-Revamped-1.3.17')

def sha(path):
    with path.open('rb') as stream:
        return hashlib.file_digest(stream, 'sha256').hexdigest()

def save(name, value):
    EVIDENCE.mkdir(parents=True, exist_ok=True)
    (EVIDENCE / name).write_text(json.dumps(value, indent=2) + '\n', encoding='utf8')

def files(root):
    for path in sorted(root.rglob('*')):
        if path.is_symlink() or path.lstat().st_file_attributes & stat.FILE_ATTRIBUTE_REPARSE_POINT:
            raise ValueError(f'Unexpected reparse member: {path}')
        if path.is_file():
            yield path

def snapshot(root):
    return {p.relative_to(root).as_posix(): sha(p) for p in files(root)}

def preservation():
    return {'game': snapshot(REAL), 'profile': snapshot(PROFILE),
            'published': snapshot(PUBLISHED), 'published_zip': sha(PUBLISHED.parent / (PUBLISHED.name + '.zip'))}

def copy_verified(source, target, receipt):
    if target.exists():
        raise FileExistsError(target)
    target.parent.mkdir(parents=True, exist_ok=True)
    before = sha(source)
    shutil.copy2(source, target)
    assert before == sha(target) == sha(source), source
    receipt.append({'source': str(source), 'target': str(target), 'sha256': before, 'bytes': target.stat().st_size})

def copy_tree(source, target, receipt):
    for item in files(source):
        if '__pycache__' not in item.parts and item.suffix != '.pyc':
            copy_verified(item, target / item.relative_to(source), receipt)

def run(command, *, env=None, log=None):
    if log:
        with (EVIDENCE / log).open('w', encoding='utf8') as stream:
            result = subprocess.run(command, cwd=ROOT, env=env, stdout=stream, stderr=subprocess.STDOUT)
        result.check_returncode()
        return (EVIDENCE / log).read_text(encoding='utf8')
    return subprocess.check_output(command, cwd=ROOT, env=env, text=True)

def migrate():
    assert not PERMANENT.exists(), 'Destination must be new; inspect any partial migration before resuming'
    save('preservation-before.json', preservation())
    receipt = []
    prepared = PERMANENT / 'prepared'; reference = PERMANENT / 'reference'
    for name in ('Assets', 'BepInEx', 'downloads'):
        copy_tree(LEGACY[1] / name, prepared / name, receipt)
    for name in ('prepared-assets.json', 'requirements.json', 'revamped-work.json'):
        copy_verified(LEGACY[1] / name, prepared / name, receipt)
    for name in ('android-analysis', 'pylibs'):
        copy_tree(LEGACY[0] / name, reference / name, receipt)
    # Retain the stock player and its separate QA identity, not old mod payloads/logs.
    stock = json.loads((ROOT / 'installer/supported-windows.json').read_text())
    for name, digest in stock.items():
        source = REAL / name
        assert sha(source) == digest, name
        if name.replace('\\', '/') == 'MinutesTillDawn_Data/globalgamemanagers':
            source = LEGACY[0] / 'game' / name
        copy_verified(source, reference / 'game' / name, receipt)
    for item in files(LEGACY[0] / 'game/BepInEx/config'):
        copy_verified(item, reference / 'game/BepInEx/config' / item.name, receipt)
    # Only this historical binary is read by the old acceptance-record generator.
    name = Path('bin/Modernization/netstandard2.0/TwentyMinutes.Modernization.dll')
    copy_verified(LEGACY[0] / name, reference / name, receipt)
    # Preserve the two unique local controller-helper sources as recovery inputs.
    for name in ('adb_device.py', 'droiddeckctl'):
        copy_verified(LEGACY[5] / 'controls' / name, reference / 'legacy-source' / name, receipt)
    duplicate_sources = []
    candidate = LEGACY[0] / 'candidate/20-Minutes-Till-Dawn-Revamped-1.0.1'
    for name in ('installer/asset_builder.py', 'installer/install.py'):
        original = subprocess.check_output(['git', 'show', '68a5c566232c7e5279b13f165bf9b41e4277f94b:' + name], cwd=ROOT)
        path = candidate / name
        identical = hashlib.sha256(original).hexdigest() == sha(path)
        if not identical:
            copy_verified(path, reference / 'legacy-source/candidate-1.0.1' / name, receipt)
        duplicate_sources.append({'path': str(path), 'matches_git': identical, 'git_revision': '68a5c566232c7e5279b13f165bf9b41e4277f94b'})
    # A freshly generated environment has correct launchers and activation paths.
    # Copy only verified pinned site-packages; no stale pip.exe or activation script.
    venv.EnvBuilder(with_pip=False).create(prepared / 'runtime')
    copy_tree(LEGACY[1] / 'runtime/Lib/site-packages', prepared / 'runtime/Lib/site-packages', receipt)
    save('retained-files.json', receipt)
    save('source-disposition.json', duplicate_sources)
    print(json.dumps({'retained_files': len(receipt), 'retained_bytes': sum(r['bytes'] for r in receipt), 'destination': str(PERMANENT)}))

def consumers():
    replacements = {LEGACY[1].as_posix(): (PERMANENT / 'prepared').as_posix(), LEGACY[0].as_posix(): (PERMANENT / 'reference').as_posix()}
    changes = []
    for path in sorted((ROOT / 'tools').iterdir()):
        if path.suffix not in ('.py', '.ps1'):
            continue
        before = path.read_text(encoding='utf8'); after = before
        # CLI-home is disposable per invocation, not part of the retained inputs.
        if path.suffix == '.ps1':
            after = after.replace("$env:DOTNET_CLI_HOME='D:/Temp/20mtd-branding-20261022/dotnet'", "$env:DOTNET_CLI_HOME=Join-Path $env:MODERNIZATION_SCRATCH 'dotnet'")
        else:
            after = after.replace("DOTNET_CLI_HOME='D:/Temp/20mtd-branding-20261022/dotnet'", "DOTNET_CLI_HOME=str(TEMP/'dotnet')")
        for old, new in replacements.items():
            after = after.replace(old, new).replace(old.replace('/', '\\'), new.replace('/', '\\'))
        if after != before:
            changes.append({'path': str(path), 'before_sha256': sha(path), 'before': before, 'after': after})
            path.write_text(after, encoding='utf8', newline='\n')
            changes[-1]['after_sha256'] = sha(path)
        if path.suffix == '.py':
            compile(after, str(path), 'exec')
    save('consumer-changes.json', changes)
    print(f'Updated {len(changes)} local helper sources; historical receipts and release scripts are unchanged')

def verify():
    prepared = PERMANENT / 'prepared'; reference = PERMANENT / 'reference'
    receipt = json.loads((EVIDENCE / 'retained-files.json').read_text())
    assert all(sha(Path(row['target'])) == row['sha256'] for row in receipt)
    runtime = prepared / 'runtime/Scripts/python.exe'
    assert Path(sys.executable).resolve() == runtime.resolve(), 'Run verification using the migrated Python'
    # Import and dependency checks happen through the relocated Python itself.
    run([str(runtime), '-B', '-m', 'pip', 'check'], log='python-dependencies.log')
    frozen = run([str(runtime), '-B', '-m', 'pip', 'freeze'])
    required = (ROOT / 'installer/requirements.txt').read_text().splitlines()
    def pins(lines):
        return {re.sub(r'[-_.]+', '-', name).lower(): version
                for name, version in (line.split('==') for line in lines if line.strip())}
    assert pins(frozen.splitlines()) == pins(required), frozen
    import UnityPy
    from PIL import Image
    sys.path.insert(0, str(reference / 'pylibs'))
    import capstone
    assert capstone.Cs(capstone.CS_ARCH_ARM64, capstone.CS_MODE_ARM)
    save('python-runtime.json', {'executable': str(runtime), 'packages': frozen.splitlines(), 'check': 'No broken requirements found'})
    assert not SCRATCH.exists(), 'Use a fresh registered exact build root'
    SCRATCH.mkdir()
    build_receipt = []
    copy_tree(reference / 'game', SCRATCH / 'game', build_receipt)
    copy_tree(prepared / 'BepInEx/BepInEx/core', SCRATCH / 'game/BepInEx/core', build_receipt)
    env = os.environ.copy()
    env.update(MODERNIZATION_SCRATCH=str(SCRATCH), MODERNIZATION_BUILD_ROOT=str(SCRATCH),
               MODERNIZATION_PREPARED_ASSETS=str(prepared / 'Assets'), TEMP=str(SCRATCH), TMP=str(SCRATCH),
               DOTNET_CLI_HOME=str(SCRATCH / 'dotnet'), DOTNET_GENERATE_ASPNET_CERTIFICATE='false',
               NUGET_PACKAGES=str(SCRATCH / 'nuget-packages'), NUGET_HTTP_CACHE_PATH=str(SCRATCH / 'nuget-http-cache'),
               PYTHONDONTWRITEBYTECODE='1', PYTHONUTF8='1')
    run(['dotnet', 'build', 'validation/Validation.csproj', '-c', 'Release', '--nologo', '-v:minimal',
         '-nodeReuse:false', '-m:1', '-p:UseSharedCompilation=false'], env=env, log='build.log')
    run([str(runtime), '-B', 'release/build.py', '--scratch', str(SCRATCH), '--prepared-assets', str(prepared / 'Assets'),
         '--output', str(SCRATCH / 'package'), '--candidate'], env=env, log='package.log')
    package = SCRATCH / 'package' / PUBLISHED.name
    manifest = json.loads((package / 'PACKAGE.json').read_text())
    assert all(sha(package / name) == digest for name, digest in manifest['files'].items())
    update = json.loads((package / 'UPDATE.json').read_text())
    prior = json.loads((PUBLISHED / 'UPDATE.json').read_text())
    assert len(update['files']) == 614 and update['schema'] == 2 and update['save_epoch'] == 1
    assert update['files'].keys() == prior['files'].keys()
    plugin = next(iter(update['payload']))
    assert all(value == prior['files'][name] for name, value in update['files'].items() if name != plugin)
    spec = importlib.util.spec_from_file_location('migration_install', package / 'installer/install.py')
    module = importlib.util.module_from_spec(spec); spec.loader.exec_module(module)
    game = SCRATCH / 'install-check'; work = SCRATCH / 'installer-work'
    stock = json.loads((ROOT / 'installer/supported-windows.json').read_text())
    for name, digest in stock.items():
        copy_verified(REAL / name, game / name, build_receipt)
    copy_verified(prepared / 'downloads/BepInEx.zip', work / 'downloads/BepInEx.zip', build_receipt)
    module.stock(game)
    installed = module.install(game, prepared / 'Assets', work)
    assert all(sha(game / name) == digest for name, digest in update['files'].items())
    rollback = module.uninstall(game)
    assert rollback['removed_exact_owned_files'] == 614
    assert snapshot(game) == {name.replace('\\', '/'): digest for name, digest in stock.items()}
    assert preservation() == json.loads((EVIDENCE / 'preservation-before.json').read_text())
    save('workflow-verification.json', {'status': 'PASS', 'copied_input_files': len(build_receipt),
        'build': 'validation and production Release assemblies', 'package': manifest['source_commit'],
        'package_file_hashes': len(manifest['files']), 'updater_targets': 614, 'unchanged_non_plugin_targets': 613,
        'install': installed, 'rollback': rollback, 'live_game_saves_published_release_unchanged': True})
    print('PASS: migrated runtime, Release build/package, 614-target install and exact rollback; live state unchanged')

def final():
    assert all(not path.exists() for path in [*LEGACY, SCRATCH]), 'Cleanup residuals remain'
    assert preservation() == json.loads((EVIDENCE / 'preservation-before.json').read_text())
    retained = json.loads((EVIDENCE / 'retained-files.json').read_text())
    assert all(sha(Path(row['target'])) == row['sha256'] for row in retained)
    for row in json.loads((EVIDENCE / 'consumer-changes.json').read_text()):
        assert sha(Path(row['path'])) == row['after_sha256']
    cleanup = json.loads(subprocess.check_output([sys.executable, '-B',
        'C:/Users/darka/.agents/skills/transactional-cleanup/scripts/invoke_transactional_cleanup.py',
        'status', '--transaction', 'a127effcc71dbe47b3f4271d32f6b4e6'], text=True))
    assert cleanup['ok'] and cleanup['state'] == 'applied' and not cleanup['errors'], cleanup
    save('cleanup-result.json', cleanup)
    save('final-verification.json', {'status': 'PASS', 'retained_files': len(retained), 'legacy_roots_absent': [str(p) for p in LEGACY],
        'verification_scratch_absent': str(SCRATCH), 'live_state_unchanged': True, 'permanent_root': str(PERMANENT),
        'removed_logical_bytes': cleanup['deleted_bytes'], 'retained_logical_bytes': sum(r['bytes'] for r in retained)})
    print('PASS: permanent delivery intact; all exact temporary roots absent; live state unchanged')

def retain_evidence():
    receipt = json.loads((EVIDENCE / 'retained-files.json').read_text())
    copy_verified(LEGACY[0] / 'managed-recipe-evidence.json', PERMANENT / 'reference/managed-recipe-evidence.json', receipt)
    save('retained-files.json', receipt)
    print('Retained the original managed-analysis evidence envelope without changing its historical paths')

def cleanup_inspect():
    helper = Path('C:/Users/darka/.agents/skills/transactional-cleanup/scripts/invoke_transactional_cleanup.py')
    offset = 0; rows = []; pages = 0
    while True:
        page = json.loads(subprocess.check_output([sys.executable, '-B', str(helper), 'inspect', '--transaction',
            'a127effcc71dbe47b3f4271d32f6b4e6', '--offset', str(offset), '--limit', '1000'], text=True))
        assert page['ok'] and page['state'] == 'reviewed', page
        rows.extend(page['page_items']); pages += 1
        if page['next_offset'] is None:
            break
        offset = page['next_offset']
    assert len(rows) == page['total_items']
    counts = Counter()
    for row in rows:
        path = Path(row['path'])
        assert any(path == root or root in path.parents for root in [*LEGACY, SCRATCH]), path
        counts[row['decision']] += 1
        assert row['decision'] == 'candidate', row
        assert row['links'] == 1 and not row['reparse'] and row['identity'], row
        assert row['git_status'] == 'not_tracked_or_outside_repository', row
        assert row['kind'] == 'explicit-generated-output', row
    save('cleanup-inspected.json', {'transaction': 'a127effcc71dbe47b3f4271d32f6b4e6',
        'pages': pages, 'items': len(rows), 'decisions': dict(counts), 'rows': rows})
    print(json.dumps({'pages': pages, 'items': len(rows), 'decisions': dict(counts)}))

def cleanup_register():
    helper = Path('C:/Users/darka/.agents/skills/transactional-cleanup/scripts/invoke_transactional_cleanup.py')
    results = []
    for root in [*LEGACY, SCRATCH]:
        command = [sys.executable, '-B', str(helper), 'register', '--transaction', 'a127effcc71dbe47b3f4271d32f6b4e6',
                   '--path', str(root), '--kind', 'explicit-generated-output', '--evidence',
                   '20MTD task-generated analysis/build/deployment output; unique maintenance inputs are hash-retained under D:/Projects/20MTD-Modernization-maintenance before review']
        if root in LEGACY:
            command.append('--regenerated')
        result = json.loads(subprocess.check_output(command, text=True))
        assert result['ok'], result
        results.append(result)
    save('cleanup-registrations.json', results)
    print('Registered the six legacy roots and one new native-build scratch root')

def inventory():
    results = []
    for root in LEGACY:
        groups = defaultdict(lambda: {'files': 0, 'bytes': 0, 'extensions': Counter()})
        unusual = []; special = []; stack = [root]
        while stack:
            directory = stack.pop()
            for path in directory.iterdir():
                info = path.lstat()
                if info.st_file_attributes & stat.FILE_ATTRIBUTE_REPARSE_POINT:
                    special.append(str(path)); continue
                if path.name == '.git': special.append(str(path))
                if path.is_dir(): stack.append(path); continue
                rel = path.relative_to(root); group = rel.parts[0] if len(rel.parts) > 1 else '(root files)'
                row = groups[group]; row['files'] += 1; row['bytes'] += info.st_size
                row['extensions'][path.suffix.lower()] += 1
                if path.suffix.lower() in ('.py', '.ps1', '.java', '.cs', '.kt') and not any(
                    part in ('runtime', 'pylibs', 'site-packages', 'DummyDll', 'obj', 'bin', 'dumper') for part in rel.parts):
                    unusual.append(str(rel))
        results.append({'root': str(root), 'groups': dict(groups), 'source_candidates': unusual, 'special': special})
    EVIDENCE.mkdir(parents=True, exist_ok=True)
    (EVIDENCE / 'inventory.json').write_text(json.dumps(results, indent=2), encoding='utf8')
    for item in results:
        print(item['root'])
        large = sorted(item['groups'].items(), key=lambda row: row[1]['bytes'], reverse=True)
        for name, row in large[:18]: print(f"  {name}: {row['files']} files, {row['bytes']:,} bytes")
        print('  Source candidates:', item['source_candidates'][:25], 'total', len(item['source_candidates']))
        print('  Special members:', item['special'])

if __name__ == '__main__':
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument('action', choices=('inventory', 'migrate', 'consumers', 'verify', 'final', 'retain_evidence', 'cleanup_register', 'cleanup_inspect'))
    globals()[parser.parse_args().action]()
