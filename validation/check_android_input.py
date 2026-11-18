"""Verify the single-input payload against the previous gameplay preparation.

Object IDs and exported sprite filenames are package-local, not gameplay.
Compare the fields consumed by Characters.cs and actual pixels/geometry instead.
"""
from pathlib import Path
import argparse, hashlib, json, sys
from PIL import Image

ROOT=Path(__file__).resolve().parents[1]
sys.path.insert(0,str(ROOT/'installer'))
from install import asset_contract

SKILL_FIELDS=('cooldown','cd','reduceCd','duration','EffectDuration','EffectRange','secondsPerTick',
    'chargeSpeedMulti','chargeDuration','chargeDamageMultiplier','baseKnockback','nextShotDamageMultiplier','damageAmount','triggerOnHit')

def character_contract(root):
    data=json.loads((root/'characters/characters.json').read_text(encoding='utf8'));sprites={}
    for row in data['sprites']:
        image=Image.open(root/'characters'/row['file']).convert('RGBA')
        sprites[row['key']]={k:row[k] for k in ('name','pivotX','pivotY','ppu')}
        sprites[row['key']].update(size=list(image.size),rgba_sha256=hashlib.sha256(image.tobytes()).hexdigest())
    def normalize(value):
        if isinstance(value,str) and value in sprites:return sprites[value]
        if isinstance(value,list):return [normalize(v) for v in value]
        if isinstance(value,dict):return {k:normalize(v) for k,v in value.items()}
        return value
    characters=[]
    for row in data['characters']:
        # Every SkillData field the runtime deserializes is retained. Other
        # fields are Android object pointers/metadata unused by the Windows port.
        row['skill']={k:row['skill'].get(k,False if k=='triggerOnHit' else 0) for k in SKILL_FIELDS}
        characters.append(normalize(row))
    return {'characters':characters,'sprites':sorted(sprites.values(),key=lambda v:json.dumps(v,sort_keys=True))}

def compare(before,after,output):
    old,new=asset_contract(before),asset_contract(after);assert len(new)==1717
    differences=[]
    for name in sorted(set(old)|set(new)):
        if name.startswith('characters/'):continue
        if name in ('audio/icicle-whoosh.json','audio/overload.json'):
            a=json.loads((before/name).read_text());b=json.loads((after/name).read_text())
            a['source'].pop('id');b['source'].pop('id')
            if a!=b:differences.append(name)
        elif old.get(name)!=new.get(name):differences.append(name)
    a,b=character_contract(before),character_contract(after)
    if a!=b:
        (output.parent/'old-character-contract.json').write_text(json.dumps(a,indent=2)+'\n')
        (output.parent/'new-character-contract.json').write_text(json.dumps(b,indent=2)+'\n')
        differences.append('characters/characters.json: consumed fields, sprites, geometry or animations')
    report={'previous_assets':str(before),'new_assets':str(after),'old_files':len(old),'new_files':len(new),
        'gameplay_and_render_differences':differences,'compared':'All non-character pixels/JSON/text/binary contracts; character runtime fields, animation timing, pixels/pivots/PPU and poses. Only package-local character sprite IDs/filenames and two audio provenance IDs are normalized.'}
    output.write_text(json.dumps(report,indent=2)+'\n');print(json.dumps(report,indent=2));assert not differences,'Inspect actual exported differences before changing game rules'

if __name__=='__main__':
    parser=argparse.ArgumentParser();parser.add_argument('--before',type=Path,required=True);parser.add_argument('--after',type=Path,required=True);parser.add_argument('--output',type=Path,required=True);args=parser.parse_args()
    compare(args.before,args.after,args.output)
