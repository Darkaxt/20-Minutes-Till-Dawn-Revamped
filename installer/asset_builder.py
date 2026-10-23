"""Read whitelisted assets from the player's own pinned inputs, locally only."""
import collections
import csv
import hashlib
import io
import json
import math
import pathlib
import re
import struct
import wave
import zipfile

import UnityPy
from PIL import Image
from UnityPy.classes.PPtr import PPtr
from UnityPy.helpers.TypeTreeGenerator import TypeTreeGenerator


def load_android(apkm):
    with zipfile.ZipFile(apkm) as outer:
        with zipfile.ZipFile(io.BytesIO(outer.read('base.apk'))) as base:
            data = {n.removeprefix('assets/bin/Data/'): base.read(n) for n in base.namelist()
                    if n.startswith('assets/bin/Data/') and not n.endswith('/')}
    parts = collections.defaultdict(dict)
    for name in list(data):
        match = re.fullmatch(r'(.+)\.split(\d+)', name)
        if match:
            parts[match[1]][int(match[2])] = data.pop(name)
    for name, pieces in parts.items():
        if sorted(pieces) != list(range(len(pieces))):
            raise ValueError('Noncontiguous Unity split: ' + name)
        data[name] = b''.join(pieces[i] for i in range(len(pieces)))
    env = UnityPy.Environment()
    for name, value in data.items():
        env.load_file(value, name=name)
    return env


def deref(obj, pointer):
    return PPtr(m_FileID=pointer['m_FileID'], m_PathID=pointer['m_PathID'], assetsfile=obj.assets_file).deref()


def read_mono(obj):
    # Generated layouts need the real enabled-byte alignment and primitive-vector
    # outer type. Keep UnityPy's complete object-byte-consumption checks enabled.
    from UnityPy.helpers import TypeTreeHelper as helper
    helper.read_typetree_boost = None

    def fix(node):
        if [x.m_Name for x in node.m_Children[:4]] == ['m_GameObject', 'm_Enabled', 'm_Script', 'm_Name']:
            node.m_Children[1].m_MetaFlag = 16384
        if node.m_Children and node.m_Children[0].m_Type == 'Array':
            subtype = node.m_Children[0].m_Children[1]
            if node.m_Type in helper.FUNCTION_READ_MAP and not (node.m_Type == 'string' and subtype.m_Type == 'char'):
                node.m_Type = 'vector'
        for child in node.m_Children:
            fix(child)

    node = obj._get_typetree_node()
    fix(node)
    data = obj.read_typetree(nodes=node)
    head = obj.parse_monobehaviour_head()
    if data['m_Script'] != {'m_FileID': head.m_Script.m_FileID, 'm_PathID': head.m_Script.m_PathID}:
        raise ValueError('Generated script identity mismatch')
    return data


def weapon_audio(env, output):
    """Prepare the two source gameplay clips absent from the native player."""
    audio = output / 'audio'
    audio.mkdir(exist_ok=True)
    for identity, name, stem, expected_frames in (
        (392, 'Spell_Ice_Small_Whoosh_01', 'icicle-whoosh', 44100),
        (399, 'Fire Spelll 27', 'overload', 184012),
    ):
        obj = next(o for o in env.objects if o.assets_file.name == 'sharedassets1.assets' and o.path_id == identity)
        clip = obj.read()
        if obj.type.name != 'AudioClip' or clip.m_Name != name:
            raise ValueError('Gameplay sound whitelist identity mismatch: ' + stem)
        decoded = clip.samples
        if set(decoded) != {name + '.wav'}:
            raise ValueError('Unexpected gameplay audio samples: ' + stem)
        wav_bytes = decoded[name + '.wav']
        with wave.open(io.BytesIO(wav_bytes), 'rb') as wav:
            channels, frequency, frames = wav.getnchannels(), wav.getframerate(), wav.getnframes()
            if (channels, frequency, frames, wav.getsampwidth(), wav.getcomptype()) != (2, 44100, expected_frames, 2, 'NONE'):
                raise ValueError('Unexpected pinned gameplay PCM format: ' + stem)
            pcm = wav.readframes(frames)
        samples = [sample / 32768.0 for sample, in struct.iter_unpack('<h', pcm)]
        payload = struct.pack('<iii', channels, frequency, frames) + struct.pack('<' + str(len(samples)) + 'f', *samples)
        (audio / (stem + '.pcm')).write_bytes(payload)
        (audio / (stem + '.json')).write_text(json.dumps({
            'source': {'file': obj.assets_file.name, 'id': obj.path_id, 'name': clip.m_Name},
            'format': 'three little-endian int32: channels, frequency, frames; interleaved little-endian float32 PCM',
            'channels': channels, 'frequency': frequency, 'frames': frames,
            'decoded_wav_sha256': hashlib.sha256(wav_bytes).hexdigest(),
            'pcm_sha256': hashlib.sha256(payload).hexdigest(),
        }, indent=2), encoding='utf8')


def android_assets(apkm, dummy, output):
    env = load_android(apkm)
    generator = TypeTreeGenerator('2022.3.56f1')
    generator.load_local_dll_folder(str(dummy))
    env.typetree_generator = generator
    objects = {(o.assets_file.name, o.path_id): o for o in env.objects}
    weapon_audio(env, output)
    loc = {}
    for obj in env.objects:
        if obj.type.name == 'TextAsset' and obj.peek_name() == 'localization':
            loc.update({r['key']: r.get('en', '') for r in csv.DictReader(io.StringIO(obj.read().m_Script))})
    dump = (dummy.parent / 'dump.cs').read_text(encoding='utf8')
    enum = re.search(r'public enum StatType\b[^\{]*\{(.*?)\n\}', dump, re.S).group(1)
    stats = {int(value): name for name, value in re.findall(r'public const StatType (\w+) = (\d+);', enum)}
    sprite_records = {}
    art = output / 'characters'
    (art / 'sprites').mkdir(parents=True, exist_ok=True)

    def export_sprite(obj):
        key = obj.assets_file.name + '_' + str(obj.path_id)
        if key not in sprite_records:
            d = obj.read()
            file = 'sprites/' + key + '.png'
            d.image.save(art / file)
            sprite_records[key] = {'key': key, 'name': d.m_Name, 'file': file,
                                   'pivotX': d.m_Pivot.x, 'pivotY': d.m_Pivot.y, 'ppu': d.m_PixelsToUnits}
        return key

    def transform_chain(renderer, root):
        def transform(go):
            return next(deref(go, p['component']) for p in go.read_typetree()['m_Component']
                        if deref(go, p['component']).type.name in ('Transform', 'RectTransform'))
        current, root_transform = transform(deref(renderer, renderer.read_typetree()['m_GameObject'])), transform(root)
        chain = []
        while (current.assets_file, current.path_id) != (root_transform.assets_file, root_transform.path_id):
            d = current.read_typetree()
            chain.insert(0, {'name': deref(current, d['m_GameObject']).read_typetree()['m_Name'],
                             'position': d['m_LocalPosition'], 'scale': d['m_LocalScale'], 'rotation': d['m_LocalRotation']})
            current = deref(current, d['m_Father'])
        return chain

    def clip_frames(clip):
        d = clip.read_typetree()
        raw = d['m_MuscleClip']['m_Clip']['data']['m_StreamedClip']['data']
        stream, frames = io.BytesIO(struct.pack('<' + 'I' * len(raw), *raw)), []
        mapping = d['m_ClipBindingConstant']['pptrCurveMapping']
        if len(d['m_ClipBindingConstant']['genericBindings']) != 1:
            raise ValueError('Unsupported animation binding')
        while stream.tell() < len(raw) * 4:
            time, count = struct.unpack('<fI', stream.read(8))
            for _ in range(count):
                index, *coefficients = struct.unpack('<Iffff', stream.read(20))
                value = coefficients[-1]
                if index != 0 or value != int(value) or not 0 <= value < len(mapping):
                    raise ValueError('Unsupported sprite animation index')
                frames.append({'time': max(0, time), 'sprite': export_sprite(deref(clip, mapping[int(value)]))})
        if not frames or frames[0]['time'] != 0 or not all(math.isfinite(f['time']) for f in frames):
            raise ValueError('Invalid animation timeline')
        return {'name': d['m_Name'], 'duration': d['m_MuscleClip']['m_StopTime'],
                'loop': d['m_MuscleClip']['m_LoopTime'], 'frames': frames}

    characters = []
    for key, character_id, passive_id, power_ids in (
            ('Haruka', 2989, 3369, (2990, 2991, 2992)),
            ('Dimond_Portrait', 2984, 3562, (2986, 2987, 2988))):
        character, passive = objects['sharedassets0.assets', character_id], objects['sharedassets0.assets', passive_id]
        d, pd = read_mono(character), read_mono(passive)
        if d['m_Name'] != key:
            raise ValueError('Character whitelist identity mismatch')
        controller = next(o for o in env.objects if o.type.name == 'AnimatorOverrideController' and o.peek_name() == 'AC_' + key)
        clips = [clip_frames(deref(controller, pair['m_OverrideClip'])) for pair in controller.read_typetree()['m_Clips']]
        root = deref(passive, pd['m_GameObject'])
        effects = []
        for field in ('spriteEffect', 'spriteFX1', 'spriteFX2'):
            if field not in pd:
                continue
            obj = deref(passive, pd[field])
            fd = read_mono(obj)
            renderer = deref(obj, fd['spriteRenderer'])
            effects.append({'field': field, 'secondsPerFrame': fd['secPerFrame'], 'loop': fd['loop'],
                            'frames': [export_sprite(deref(obj, p)) for p in fd['sprites']],
                            'alternateFrames': [export_sprite(deref(passive, p)) for p in pd.get('skillSpts2', [])] if field == 'spriteEffect' else [],
                            'transforms': transform_chain(renderer, root)})
        powers = []
        for power_id in power_ids:
            power = objects['sharedassets0.assets', power_id]
            data = read_mono(power)
            powers.append({'key': data['m_Name'], 'icon': export_sprite(deref(power, data['icon'])),
                           'name': loc[data['nameStringID']['key']], 'description': loc.get(data['descriptionStringID']['key'], ''),
                           'stats': [dict(change, type=stats[change['type']]) for change in data['statChanges']]})
        fog = None
        if 'fogRed' in pd:
            renderer = deref(passive, pd['fogRed'])
            fog = {'sprite': export_sprite(deref(renderer, renderer.read_typetree()['m_Sprite'])),
                   'transforms': transform_chain(renderer, root)}
        characters.append({'key': key, 'name': loc[d['nameStringID']['key']], 'description': loc[d['descriptionStringID']['key']],
                           'startHP': d['startHP'], 'moveSpeed': d['MoveSpeed'], 'shootMoveSpeedMultiplier': d['ShootMoveSpeedMultiplier'],
                           'portrait': export_sprite(deref(character, d['portrait'])),
                           'icon': export_sprite(deref(character, d['icon'])) if d['icon']['m_PathID'] else None,
                           'skillIcon': export_sprite(deref(passive, pd['SkillSpt'])), 'clips': clips, 'skill': pd,
                           'colliderRadius': deref(passive, pd['circleCollider']).read_typetree()['m_Radius'],
                           'effects': effects, 'powers': powers, 'fog': fog})
    (art / 'characters.json').write_text(json.dumps({'characters': characters, 'sprites': list(sprite_records.values())}, indent=2), encoding='utf8')
    expected = {f'T_ChargeCannon_SS_{i}' for i in range(7)} | {f'weapon-{letter}_{i}' for letter in 'abc' for i in range(1, 4)}
    expected |= {'Affix_Fire', 'Affix_Thunder', 'Affix_Ice', 'Affix_BulletDamageReduce'}
    found = collections.Counter()
    for obj in env.objects:
        if obj.type.name == 'Sprite' and obj.peek_name() in expected:
            d = obj.read()
            d.image.save(output / (d.m_Name + '.png'))
            found[d.m_Name] += 1
    if set(found) != expected or any(count != 1 for count in found.values()):
        raise ValueError('Gameplay sprite whitelist mismatch: ' + repr(found))
    affixes = {'icons': [], 'actors': []}
    for obj in env.objects:
        if obj.type.name == 'Sprite' and obj.peek_name().startswith('Affix_') and obj.peek_name() in expected:
            sprite = obj.read()
            affixes['icons'].append({'name': sprite.m_Name, 'ppu': sprite.m_PixelsToUnits,
                                    'pivotX': sprite.m_Pivot.x, 'pivotY': sprite.m_Pivot.y})
        elif obj.type.name == 'MonoBehaviour':
            head = obj.parse_monobehaviour_head()
            script = head.m_Script.deref().read_typetree()
            if script.get('m_Namespace') != 'flanne' or script.get('m_ClassName') != 'Affix':
                continue
            data = read_mono(obj)
            root = head.m_GameObject.deref()
            parent = deref(obj, data['iconParent'])
            prefab = deref(obj, data['iconPrefab'])
            renderer = next(deref(prefab, p['component']) for p in prefab.read_typetree()['m_Component']
                            if deref(prefab, p['component']).type.name == 'SpriteRenderer').read_typetree()
            actor = {'name': root.peek_name(), 'poses': transform_chain(parent, root),
                     'sortingLayer': renderer['m_SortingLayerID'], 'sortingOrder': renderer['m_SortingOrder']}
            # Repeated scene instances carry the same local contract. Collapse
            # only fully identical records, never pick an arbitrary variant.
            if actor not in affixes['actors']:
                affixes['actors'].append(actor)
    (output / 'affixes.json').write_text(json.dumps(affixes, indent=2), encoding='utf8')
    fallback = output / 'powerups'
    fallback.mkdir()
    powers = []
    for name, path_id in zip(('Adrenaline', 'HeavyCaliber', 'LargeMag', 'LightenLoad', 'NightVision', 'PeaShooter'), range(3591, 3597)):
        obj = objects['sharedassets1.assets', path_id]
        data = read_mono(obj)
        if data['m_Name'] != name:
            raise ValueError('Fallback whitelist identity mismatch')
        icon = deref(obj, data['icon']).read()
        icon.image.save(fallback / (name + '.png'))
        powers.append({'key': name, 'nameKey': data['nameStringID']['key'], 'name': loc[data['nameStringID']['key']],
                       'description': loc.get(data['descriptionStringID']['key'], ''), 'duration': data.get('duration', 0),
                       'stats': [{'type': stats[c['type']], 'flatValue': c.get('flatValue', 0),
                                  'isFlatMod': int(c.get('isFlatMod', 0)), 'value': c['value']} for c in data['statChanges']],
                       'icon': name + '.png', 'ppu': icon.m_PixelsToUnits})
    (fallback / 'manifest.json').write_text(json.dumps({'powers': powers}, indent=2), encoding='utf8')
    achievements = output / 'achievements'
    achievements.mkdir()

    def achievement_icon(owner, pointer, filename):
        if not pointer['m_PathID']:
            return None
        deref(owner, pointer).read().image.save(achievements / filename)
        return filename

    catalog = objects['sharedassets1.assets', 3399]
    catalog_data = read_mono(catalog)
    if catalog_data['m_Name'] != 'AchieveProfile':
        raise ValueError('Achievement catalog whitelist identity mismatch')
    entries = []
    for pointer in catalog_data['achievePool']:
        obj = deref(catalog, pointer)
        data = read_mono(obj)
        identity = data['type']
        steam_id = 'ACH_MAGIC_BOW' if data['m_Name'] == 'ACH_MAGICPOW' else data['m_Name'].upper()
        entries.append({'type': identity, 'steamId': steam_id,
                        'name': loc[data['nameStringID']['key']],
                        'description': loc[data['descriptionStringID']['key']],
                        'icon': achievement_icon(obj, data['icon'], f'{identity}-earned.png'),
                        'iconGray': achievement_icon(obj, data['iconGray'], f'{identity}-locked.png')})
    reward_profile = objects['sharedassets1.assets', 3400]
    reward_data = read_mono(reward_profile)
    if reward_data['m_Name'] != 'AchieveRewardProfile':
        raise ValueError('Achievement reward whitelist identity mismatch')
    rewards = []
    for row in reward_data['AchieveRewardDatas']:
        reward = row['rewardData']
        if reward['etype'] != 0:  # Only the source Gold claims; skins/wings are excluded.
            continue
        rewards.append({'id': row['id'], 'count': row['achieveCount'], 'amount': reward['value'],
                        'icon': achievement_icon(reward_profile, reward['icon'], f'reward-{row["id"]}.png')})
    (achievements / 'manifest.json').write_text(json.dumps({'entries': entries, 'rewards': rewards}, indent=2), encoding='utf8')
    controller = objects['level2', 3848]
    controller_data = read_mono(controller)
    controller_script = deref(controller, controller_data['m_Script']).read()
    if controller_script.m_ClassName != 'GameController' or controller_script.m_Namespace != 'flanne.Core':
        raise ValueError('Damage-display controller whitelist identity mismatch')
    (output / 'damage-display.json').write_text(json.dumps({
        'threshold': controller_data['lowFpsThreshold'], 'duration': controller_data['lowFpsDuration'],
        'interval': 1.0, 'tipDuration': 2.0, 'tip': loc['damage_display_auto']}, indent=2), encoding='utf8')
    camera_maps = []
    for file, identity, name in [
            ('sharedassets1.assets', 3447, '10M_Forest'), ('sharedassets1.assets', 3448, '20M_Forest'),
            ('sharedassets1.assets', 3449, '20M_Forest_new'), ('sharedassets1.assets', 3450, '20M_PumpkinPatch'),
            ('sharedassets1.assets', 3451, '20M_Temple'), ('sharedassets1.assets', 3452, 'Challenge_Forest'),
            ('sharedassets1.assets', 3453, 'Endless_Forest'), ('sharedassets2.assets', 212, 'Dev_Map')]:
        data = read_mono(objects[file, identity])
        if data['m_Name'] != name:
            raise ValueError('Camera map whitelist identity mismatch')
        camera_maps.append({'name': name, 'height': data['cameraHeight']})
    (output / 'camera-height.json').write_text(json.dumps({'maps': camera_maps}, indent=2), encoding='utf8')
    print('Recovered character/weapon contracts, affix icons/placements, fallback upgrades, achievement rewards, damage-display tuning and map view heights.', flush=True)


def style_assets(game_data, archive, output):
    env = UnityPy.load(str(game_data))
    objects = {(o.assets_file.name, o.path_id): o for o in env.objects}
    textures = {o.peek_name(): o for o in env.objects if o.type.name == 'Texture2D'}
    recipe = json.loads((pathlib.Path(__file__).parent / 'style-recipe.json').read_text())
    destination = output / 'reborn'
    destination.mkdir()
    lines = []
    with zipfile.ZipFile(archive) as upstream:
        for row in recipe['upstream']:
            d = objects[row['file'], row['id']].read()
            if d.m_Name != row['name'] or (d.m_Width, d.m_Height) != (row['width'], row['height']):
                raise ValueError('Native texture identity mismatch')
            raw_hash = hashlib.sha256(bytes(d.get_image_data())).hexdigest()
            if raw_hash != row['original_raw_sha256']:
                raise ValueError('Native texture content mismatch: ' + row['name'])
            suffix = 'plugins/Textures/Sprites-Reborn/' + row['source'].replace('\\', '/')
            members = [n for n in upstream.namelist() if n.endswith('/' + suffix)]
            if len(members) != 1:
                raise ValueError('Upstream texture identity mismatch')
            d.image.convert('RGBA').save(destination / (row['name'] + '.original.png'))
            Image.open(io.BytesIO(upstream.read(members[0]))).convert('RGBA').save(destination / (row['name'] + '.colorful.png'))
            lines.append(f"{row['name']}\t{row['width']}\t{row['height']}\t{raw_hash}")
    for row in recipe['manual']:
        name = row['name']
        d = textures[name].read()
        original = d.image.convert('RGBA')
        mapping = {tuple(map(int, key.split(','))): tuple(value) for key, value in row['mapping'].items()}
        painted = original.copy()
        painted.putdata([(*mapping.get(p[:3], p[:3]), p[3]) if p[3] else p for p in original.getdata()])
        raw = bytes(d.get_image_data())
        if row['original_format'] == 12:
            colored = bytearray(raw)
            endpoints = {33904: (115, 69, 115), 33871: (107, 65, 107), 63159: (249, 215, 239), 61112: (239, 207, 231)}
            for offset in range(0, len(raw), 16):
                for delta in (8, 10):
                    rgb = endpoints[struct.unpack_from('<H', raw, offset + delta)[0]]
                    value = ((rgb[0] * 31 // 255) << 11) | ((rgb[1] * 63 // 255) << 5) | (rgb[2] * 31 // 255)
                    struct.pack_into('<H', colored, offset + delta, value)
            (destination / (name + '.original.dxt5')).write_bytes(raw)
            (destination / (name + '.colorful.dxt5')).write_bytes(colored)
            d.image_data = bytes(colored)
            painted = d.image.convert('RGBA')
        if original.size != tuple(row['dimensions']) or painted.getchannel('A').tobytes() != original.getchannel('A').tobytes():
            raise ValueError('Palette edit changed geometry/alpha: ' + name)
        original.save(destination / (name + '.original.png'))
        painted.save(destination / (name + '.colorful.png'))
        lines.append(f"{name}\t{original.width}\t{original.height}\t{hashlib.sha256(raw).hexdigest()}")
    (destination / 'textures.tsv').write_text('\n'.join(lines) + '\n', encoding='utf8')
    (destination / 'CREDITS.txt').write_text('Sprites Reborn: Law; updated by Daramos; textures by Darkly77; original game art by Flanne.\n'
        'Source: https://github.com/YuriyDaramos/20MTD_Sprites-Reborn/tree/77d1a9a7316c1333ce7cd61e276da5ef5a277398\n'
        'Compatible textures prepared locally from their published source; no third-party mod DLL is installed.\n'
        'Additional original-pixel palette extension by Darkaxt.\n', encoding='utf8')
    print('Prepared 140 upstream style sheets and 63 deterministic palette extensions with exact native alpha.', flush=True)
