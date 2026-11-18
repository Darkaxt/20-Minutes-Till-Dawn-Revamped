"""Retain source appearance associations and render contracts, without running the game."""
from pathlib import Path
import argparse, json, sys
ROOT=Path(__file__).resolve().parents[1]
sys.path.insert(0,str(ROOT/'installer'))
from asset_builder import load_android, read_mono, deref
from UnityPy.helpers.TypeTreeGenerator import TypeTreeGenerator

def main():
    parser=argparse.ArgumentParser();parser.add_argument('--android',type=Path,required=True);parser.add_argument('--dummy',type=Path,required=True);parser.add_argument('--output',type=Path,required=True)
    args=parser.parse_args();env=load_android(args.android)
    generator=TypeTreeGenerator('2022.3.56f1');generator.load_local_dll_folder(str(args.dummy));env.typetree_generator=generator
    rows=[]
    for obj in env.objects:
        if obj.type.name!='MonoBehaviour':continue
        script=obj.parse_monobehaviour_head().m_Script.deref().read_typetree()
        cls=script['m_ClassName']
        if cls not in ('ShopStyle','GunData','CharacterData','SpriteDicBox','SpriteDic','ShopColors','SetHeroSkinSummonSprite','DicedSpriteAtlas'):continue
        data=read_mono(obj)
        def resolve(value):
            if isinstance(value,list):return [resolve(v) for v in value]
            if not isinstance(value,dict):return value
            if set(value)=={'m_FileID','m_PathID'}:
                if not value['m_PathID']:return None
                target=deref(obj,value)
                return {'file':target.assets_file.name,'id':target.path_id,'type':target.type.name,'name':target.peek_name()}
            return {k:resolve(v) for k,v in value.items()}
        rows.append({'class':cls,'file':obj.assets_file.name,'id':obj.path_id,'data':resolve(data)})
    args.output.parent.mkdir(parents=True,exist_ok=True);args.output.write_text(json.dumps(rows,indent=2),encoding='utf8')
    print(json.dumps({'rows':len(rows),'classes':sorted({r['class'] for r in rows})}))
    for row in rows:
        if row['class']=='DicedSpriteAtlas':
            print(json.dumps(row,indent=2)[:5000]);break

if __name__=='__main__':main()
