"""Export appearance-only assets from a player's local Android package.

Prices, ownership, seasonal gates and gameplay parameters are never exported.
"""
import csv
import io
import json
import math
import struct
from pathlib import Path

from asset_builder import deref, load_android, read_mono
from UnityPy.helpers.TypeTreeGenerator import TypeTreeGenerator


class Exporter:
    def __init__(self, env, output):
        self.env=env;self.root=Path(output)/'cosmetics';self.sprites={};self.controllers={}
        (self.root/'sprites').mkdir(parents=True,exist_ok=True)

    def sprite(self,obj):
        if obj is None:return None
        key=obj.assets_file.name+'_'+str(obj.path_id)
        if key not in self.sprites:
            data=obj.read()
            # Diced atlases contain intentional empty animation frames. They
            # have no sprite mesh; preserve that absence as transparency.
            if data.m_RD.m_VertexData.m_VertexCount==0:
                from PIL import Image
                image=Image.new('RGBA',(max(1,round(data.m_Rect.width)),max(1,round(data.m_Rect.height))))
            else:image=data.image
            name='sprites/'+key+'.png';image.save(self.root/name)
            self.sprites[key]={'key':key,'name':data.m_Name,'file':name,'pivotX':data.m_Pivot.x,'pivotY':data.m_Pivot.y,'ppu':data.m_PixelsToUnits}
        return key

    def pointer(self,obj,pointer):
        return deref(obj,pointer) if pointer['m_PathID'] else None

    def icon(self,obj,pointer):return self.sprite(self.pointer(obj,pointer))

    def clip(self,obj,source):
        data=obj.read_typetree();muscle=data['m_MuscleClip'];compiled=muscle['m_Clip']['data']
        bindings=data['m_ClipBindingConstant']['genericBindings'];mapping=data['m_ClipBindingConstant']['pptrCurveMapping']
        indexed=[]
        for binding in bindings:
            dimensions={1:3,2:4,3:3,4:3}.get(binding['attribute'],1) if binding['typeID']==4 else 1
            indexed.extend([binding]*dimensions)
        raw=compiled['m_StreamedClip']['data'];stream=io.BytesIO(struct.pack('<'+'I'*len(raw),*raw));frames=[]
        while stream.tell()<len(raw)*4:
            time,count=struct.unpack('<fI',stream.read(8))
            for _ in range(count):
                index,*coefficients=struct.unpack('<Iffff',stream.read(20));value=coefficients[-1]
                if index>=len(indexed):raise ValueError('Unsupported appearance curve index: '+data['m_Name'])
                binding=indexed[index]
                if not binding['isPPtrCurve']:continue
                if value!=int(value) or not 0<=value<len(mapping):raise ValueError('Invalid appearance sprite curve '+repr((data['m_Name'],time,index,value,len(mapping),binding)))
                sprite=self.icon(obj,mapping[int(value)])
                frames.append({'time':max(0,time),'path':binding['path'],'sprite':sprite})
        # Some imported clips retain explicit curves instead of streamed ones.
        for curve in data.get('m_PPtrCurves',[]):
            import zlib
            for key in curve['curve']:
                frames.append({'time':max(0,key['time']),'path':zlib.crc32(curve['path'].encode()),'sprite':self.icon(obj,key['value'])})
        if not frames:raise ValueError('Appearance clip has no decoded sprite animation: '+data['m_Name'])
        if not all(math.isfinite(f['time']) for f in frames):raise ValueError('Invalid appearance timing')
        return {'source':source,'name':data['m_Name'],'duration':muscle['m_StopTime'],'loop':muscle['m_LoopTime'],'frames':frames}

    def controller(self,obj):
        if obj is None:return []
        key=(obj.assets_file.name,obj.path_id)
        if key in self.controllers:return self.controllers[key]
        data=obj.read_typetree();clips=[]
        if obj.type.name=='AnimatorOverrideController':
            # Each source clip keeps its native semantic key. No runtime Unity
            # asset bundles or newer-engine animator controllers are loaded.
            for pair in data['m_Clips']:
                original=self.pointer(obj,pair['m_OriginalClip']);override=self.pointer(obj,pair['m_OverrideClip']) or original
                if original is not None and override is not None:clips.append(self.clip(override,original.peek_name()))
        elif obj.type.name=='AnimatorController':
            for pointer in data['m_AnimationClips']:
                clip=self.pointer(obj,pointer)
                if clip is not None:clips.append(self.clip(clip,clip.peek_name()))
        else:raise ValueError('Unsupported appearance controller '+obj.type.name)
        self.controllers[key]=clips;return clips

    def atlas(self,obj,pointer):
        target=self.pointer(obj,pointer)
        return [] if target is None else [self.icon(target,p) for p in read_mono(target)['sprites']]

    def visuals(self,root):
        result=[]
        def walk(go,path,poses):
            components=[deref(go,p['component']) for p in go.read_typetree()['m_Component']]
            transform=next(c for c in components if c.type.name=='Transform');td=transform.read_typetree()
            pose={'name':go.peek_name(),'position':td['m_LocalPosition'],'scale':td['m_LocalScale'],'rotation':td['m_LocalRotation']}
            chain=poses+[pose]
            animators=[c for c in components if c.type.name=='Animator']
            scripts=[(c,c.parse_monobehaviour_head().m_Script.deref().peek_name()) for c in components if c.type.name=='MonoBehaviour']
            for renderer in (c for c in components if c.type.name=='SpriteRenderer'):
                rd=renderer.read_typetree();frames=[];interval=.1
                for script,name in scripts:
                    if name=='SpriteFX':
                        fx=read_mono(script);frames=[self.icon(script,p) for p in fx['sprites']];interval=fx['secPerFrame']
                clips=[]
                for animator in animators:
                    ad=animator.read_typetree();clips.extend(self.controller(self.pointer(animator,ad['m_Controller'])))
                result.append({'path':path,'poses':chain,'sprite':self.icon(renderer,rd['m_Sprite']),'frames':frames,'interval':interval,'clips':clips,'color':rd['m_Color'],'order':rd['m_SortingOrder']})
            for child in td['m_Children']:
                target=deref(transform,child);childgo=deref(target,target.read_typetree()['m_GameObject'])
                walk(childgo,path+'/'+childgo.peek_name() if path else childgo.peek_name(),chain)
        walk(root,'',[]);return result


def cosmetic_assets(android,dummy,output):
    env=load_android(android);generator=TypeTreeGenerator('2022.3.56f1');generator.load_local_dll_folder(str(dummy));env.typetree_generator=generator
    exporter=Exporter(env,output);loc={};shops=[];characters=[];guns=[];colors=[];wing_poses=[];wing_interval=.1
    for obj in env.objects:
        if obj.type.name=='TextAsset' and obj.peek_name()=='localization':
            for row in csv.DictReader(io.StringIO(obj.read().m_Script)):loc.setdefault(row['key'],row.get('en',''))
        if obj.type.name!='MonoBehaviour':continue
        script=obj.parse_monobehaviour_head().m_Script.deref().read_typetree();cls=script['m_ClassName']
        if cls=='ShopStyle':shops.append((obj,read_mono(obj)))
        elif cls=='CharacterData':characters.append((obj,read_mono(obj)))
        elif cls=='GunData':guns.append((obj,read_mono(obj)))
        elif cls=='ShopColors':colors.append((obj,read_mono(obj)))
        elif cls=='PlayerCollider':
            player=read_mono(obj)
            if player.get('wingDicBox',{}).get('m_PathID'):
                box=deref(obj,player['wingDicBox']);box_data=read_mono(box);wing_interval=box_data['interval']
                renderer=deref(box,box_data['_renderer']);go=deref(renderer,renderer.read_typetree()['m_GameObject'])
                while True:
                    transform=next(deref(go,p['component']) for p in go.read_typetree()['m_Component'] if deref(go,p['component']).type.name=='Transform');td=transform.read_typetree()
                    if not td['m_Father']['m_PathID']:break
                    wing_poses.insert(0,{'name':go.peek_name(),'position':td['m_LocalPosition'],'scale':td['m_LocalScale'],'rotation':td['m_LocalRotation']})
                    parent=deref(transform,td['m_Father']);go=deref(parent,parent.read_typetree()['m_GameObject'])
    by_id={};items=[];color_bindings=[]
    for owner,data in colors:
        go=deref(owner,data['m_GameObject']);parts=[];root=go
        while True:
            transform=next(deref(root,p['component']) for p in root.read_typetree()['m_Component'] if deref(root,p['component']).type.name=='Transform')
            parent=transform.read_typetree()['m_Father']
            if not parent['m_PathID']:break
            parts.insert(0,root.peek_name());parent_transform=deref(transform,parent);root=deref(parent_transform,parent_transform.read_typetree()['m_GameObject'])
        style=read_mono(deref(owner,data['style']))
        color_bindings.append({'template':root.peek_name(),'path':'/'.join(parts),'channel':data['type'],'palette':style['list']})
    for obj,data in shops:
        for row in data['shopList']+data['giftList']:
            if row['type'] not in (2,4,7,9):continue
            if row['id'] in by_id:raise ValueError('Duplicate cosmetic shop identity')
            by_id[row['id']]=(obj,row)
    for identity,(obj,row) in sorted(by_id.items()):
        kind={2:'bullet',4:'wing',7:'weapon',9:'character'}[row['type']]
        item={'id':identity,'kind':kind,'name':loc.get(row['name']['key'],row['name']['key']),'icon':exporter.icon(obj,row['icon']),
              'owner':'','clips':[],'wingFrames':[],'summonClips':[],'summonFrames':[],'projectileClips':[],'projectileFrames':[],'projectileVisuals':[]}
        if kind=='weapon':
            matches=[d for _,d in guns if d['ID']==row['value'] and d['ID']>0]
            if not matches:raise ValueError('Skin has no source weapon '+str(identity))
            # Sword evolutions deliberately share the parent's source ID.
            item['owner']=next((d['m_Name'] for d in matches if not d['m_Name'].startswith(('SwordOf',))),matches[0]['m_Name'])
            item['clips']=exporter.controller(exporter.pointer(obj,row['GameController']))
            item['previewClips']=exporter.controller(exporter.pointer(obj,row['UIController']))
            projectile=exporter.pointer(obj,row['ProjectilePrefab'])
            if projectile is not None:
                item['projectileName']=projectile.peek_name()
                item['projectileVisuals']=exporter.visuals(projectile)
            item['groundSprite']=exporter.icon(obj,row['ProjectileOnGround'])
        elif kind=='wing':item['wingFrames']=exporter.atlas(obj,row['wingAtlas'])
        elif kind=='character':
            matches=[(owner,d,skin) for owner,d in characters for skin in d.get('SkinDatas',[]) if skin['Id']==identity]
            if len(matches)!=1:raise ValueError('Skin character association is ambiguous '+str(identity))
            owner,data,skin=matches[0];item['owner']=data['m_Name'];item['portrait']=exporter.icon(owner,skin['Portrait'])
            item['clips']=exporter.controller(exporter.pointer(owner,skin['AnimController']))
            item['summonSprite']=exporter.icon(owner,skin['SummonSprite'])
            item['summonClips']=exporter.controller(exporter.pointer(owner,skin['SummonAnimController']))
            item['summonFrames']=[exporter.icon(owner,p) for p in skin['SummonEffectSprites']]
        elif kind=='bullet':
            item['colorIndex']=row['colorIdx']
            styles=[read_mono(deref(owner,data['style'])) for owner,data in colors if data['type']==0]
            if not styles:raise ValueError('No source bullet colour style')
            item['color']=styles[0]['list'][row['colorIdx']]
        items.append(item)
    manifest={'version':1,'items':items,'sprites':list(exporter.sprites.values()),'colorBindings':color_bindings,'wingPoses':wing_poses,'wingInterval':wing_interval}
    (exporter.root/'manifest.json').write_text(json.dumps(manifest,indent=2),encoding='utf8')
    print(json.dumps({'cosmetics':len(items),'sprites':len(exporter.sprites),'counts':{kind:sum(i['kind']==kind for i in items) for kind in ('character','weapon','wing','bullet')}}),flush=True)


if __name__=='__main__':
    import argparse
    parser=argparse.ArgumentParser();parser.add_argument('--android',required=True,type=Path);parser.add_argument('--dummy',required=True,type=Path);parser.add_argument('--output',required=True,type=Path)
    args=parser.parse_args();cosmetic_assets(args.android,args.dummy,args.output)
