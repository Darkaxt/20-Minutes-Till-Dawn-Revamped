"""Static appearance/source checks; does not compile or launch the game."""
from pathlib import Path
import argparse, ast, collections, hashlib, json
import UnityPy

def main():
    parser=argparse.ArgumentParser();parser.add_argument('--assets',type=Path,required=True);parser.add_argument('--windows',type=Path,required=True);parser.add_argument('--output',type=Path,required=True)
    args=parser.parse_args();manifest=json.loads((args.assets/'cosmetics/manifest.json').read_text(encoding='utf8'))
    env=UnityPy.load(str(args.windows));native={o.peek_name() for o in env.objects if o.type.name=='AnimationClip'}
    items=manifest['items'];counts=dict(collections.Counter(i['kind'] for i in items));assert counts=={'bullet':7,'wing':49,'weapon':85,'character':6},counts
    assert len({i['id'] for i in items})==147
    sprites={s['key']:s for s in manifest['sprites']};assert len(sprites)==len(manifest['sprites'])
    clip_report=[]
    for item in items:
        for field in ('icon','portrait','summonSprite','groundSprite'):
            key=item.get(field)
            if key is not None:assert key in sprites,(item['id'],field,key)
        for field in ('wingFrames','summonFrames','projectileFrames'):
            assert all(key is None or key in sprites for key in item[field])
        for field in ('clips','previewClips','summonClips'):
            for clip in item.get(field,[]):
                assert clip['duration']>=0 and clip['frames']
                assert all(frame['sprite'] is None or frame['sprite'] in sprites for frame in clip['frames'])
        if item['kind'] in ('weapon','character'):
            source=[c['source'] for c in item['clips']];matching=sorted(set(source)&native)
            clip_report.append({'id':item['id'],'owner':item['owner'],'source_clips':source,'native_matches':matching})
        if item['kind']=='wing':assert item['wingFrames']
    assert manifest['wingPoses'] and manifest['wingInterval']>0
    for record in sprites.values():assert (args.assets/'cosmetics'/record['file']).is_file()
    root=Path(__file__).resolve().parents[1]
    for script in ('installer/cosmetic_builder.py','installer/install.py','release/build.py','tools/inspect_cosmetics.py'):ast.parse((root/script).read_text(encoding='utf8'))
    result={'counts':counts,'sprite_files':len(sprites),'native_clip_count':len(native),'clip_bindings':clip_report,'unmatched_items':[r for r in clip_report if not r['native_matches']],
            'source_manifest_sha256':hashlib.sha256((args.assets/'cosmetics/manifest.json').read_bytes()).hexdigest(),'limits':'Static key/asset evidence only; runtime animation/controller/collection acceptance remains unverified.'}
    args.output.parent.mkdir(parents=True,exist_ok=True);args.output.write_text(json.dumps(result,indent=2),encoding='utf8')
    print(json.dumps({k:v for k,v in result.items() if k!='clip_bindings'},indent=2))

if __name__=='__main__':main()
