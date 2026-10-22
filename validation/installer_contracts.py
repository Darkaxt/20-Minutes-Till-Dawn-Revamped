"""Focused installer ownership/recovery tests on exact task-owned fixtures."""
import argparse
import importlib.util
import json
import pathlib
import shutil

parser = argparse.ArgumentParser(description=__doc__)
parser.add_argument('--package', required=True, type=pathlib.Path)
parser.add_argument('--assets', required=True, type=pathlib.Path)
parser.add_argument('--tool-work', required=True, type=pathlib.Path)
parser.add_argument('--test-root', required=True, type=pathlib.Path)
parser.add_argument('--report', required=True, type=pathlib.Path)
args = parser.parse_args()
assert not args.test_root.exists(), 'A new exact fixture directory is required'
args.test_root.mkdir(parents=True)
spec = importlib.util.spec_from_file_location('candidate_installer', args.package / 'installer/install.py')
module = importlib.util.module_from_spec(spec)
spec.loader.exec_module(module)
checks = []


def files(root):
    return {p.relative_to(root).as_posix(): module.digest(p) for p in root.rglob('*') if p.is_file()}


def rejected(action):
    try:
        action()
    except ValueError:
        return
    raise AssertionError('Expected rejection did not occur')


unsupported = args.test_root / 'unsupported'
unsupported.mkdir()
(unsupported / 'MinutesTillDawn.exe').write_bytes(b'unsupported build fixture')
before = files(unsupported)
rejected(lambda: module.stock(unsupported))
assert files(unsupported) == before
checks.append('Unsupported original game is rejected without writes')

conflict = args.test_root / 'conflict'
conflict.mkdir()
(conflict / 'winhttp.dll').write_bytes(b'other loader must survive')
(conflict / 'user-owned.txt').write_text('keep')
before = files(conflict)
rejected(lambda: module.install(conflict.resolve(), args.assets, args.tool_work))
assert files(conflict) == before
checks.append('Conflicting loader is preserved; preflight makes no game-directory writes')

fresh = args.test_root / 'fresh'
fresh.mkdir()
(fresh / 'user-owned.txt').write_text('keep')
original = files(fresh)
module.install(fresh.resolve(), args.assets, args.tool_work)
receipt, pending = module.manifest_paths(fresh.resolve())
record = module.read_manifest(receipt)
assert all(r['owned'] for r in record['files'])
receipt.replace(pending)  # Exact interrupted commit boundary, no process killing.
module.install(fresh.resolve(), args.assets, args.tool_work)
assert module.read_manifest(receipt) == record and not pending.exists()
module.uninstall(fresh.resolve())
assert files(fresh) == original
checks.append('Matching pending installation completes with exact ownership and rolls back all new files')

shared = args.test_root / 'shared-loader'
shared.mkdir()
loader = args.tool_work / 'BepInEx'
for name in ('winhttp.dll', 'doorstop_config.ini'):
    shutil.copy2(loader / name, shared / name)
shutil.copytree(loader / 'BepInEx/core', shared / 'BepInEx/core')
(shared / 'BepInEx/config').mkdir()
(shared / 'BepInEx/config/user.cfg').write_text('user setting')
original = files(shared)
module.install(shared.resolve(), args.assets, args.tool_work)
receipt, _ = module.manifest_paths(shared.resolve())
record = module.read_manifest(receipt)
assert all(not r['owned'] for r in record['files'] if r['path'] in original)
owned = next(r for r in record['files'] if r['owned'])
target = module.member(shared.resolve(), owned['path'])
saved = target.read_bytes()
target.write_bytes(saved + b'modified')
modified = files(shared)
rejected(lambda: module.uninstall(shared.resolve()))
assert files(shared) == modified
target.write_bytes(saved)  # Restore the explicit test mutation, not a cleanup sweep.
module.uninstall(shared.resolve())
assert files(shared) == original
checks.append('Shared compatible loader/settings survive; modified owned files are rejected before removal')

tampered = args.test_root / 'tampered'
tampered.mkdir()
exe = tampered / 'MinutesTillDawn.exe'
exe.write_bytes(b'original game fixture')
receipt, _ = module.manifest_paths(tampered.resolve())
module.atomic_json(receipt, {'product': '20MTD-Revamped', 'files': [
    {'path': 'MinutesTillDawn.exe', 'sha256': module.digest(exe), 'owned': True}]})
before = files(tampered)
rejected(lambda: module.uninstall(tampered.resolve()))
assert files(tampered) == before
checks.append('Edited manifests cannot claim stock game files as mod-owned rollback targets')
args.report.write_text(json.dumps({'scope': 'Installer function contracts on task-owned fixtures; full CLI/native delivery is verified separately', 'checks': checks}, indent=2), encoding='utf8')
print(json.dumps({'checks': checks}, indent=2))
