"""Saint Rosa's local source assets and exact-alpha palette extension."""
import json
from PIL import Image
from asset_builder import deref, read_mono
from cosmetic_builder import Exporter


def export_rosa(env, output):
    objects={(o.assets_file.name,o.path_id):o for o in env.objects}
    owner=objects['sharedassets0.assets',3378]
    source=read_mono(owner)
    exporter=Exporter(env,output,'saint-rosa')
    actors={}
    for field in ('shellPrefab','petalBurstPrefab','bloomShockPrefab','smallBloomShockPrefab'):
        root=deref(owner,source[field])
        components=[deref(root,p['component']) for p in root.read_typetree()['m_Component']]
        visuals=exporter.visuals(root)
        record={'name':root.peek_name(),'visuals':visuals}
        for component in components:
            if component.type.name=='MonoBehaviour':
                script=component.parse_monobehaviour_head().m_Script.deref().read_typetree()
                kind=script['m_ClassName']
                data=read_mono(component)
                if kind=='RoseBurstVisual':
                    record['duration']=data['duration']
                    record['frames']=[exporter.icon(component,p) for p in data['frames']]
                elif kind=='TimeToLive':
                    record['lifetime']=data['lifetime']
                elif kind=='RosethornShellProjectile':
                    record['projectile']=data
            elif component.type.name in ('CircleCollider2D','Rigidbody2D'):
                record[component.type.name]=component.read_typetree()
                if component.type.name=='CircleCollider2D':
                    record['radius']=record[component.type.name]['m_Radius']
        actors[field]=record
    pulse=objects['sharedassets1.assets',1683]
    if pulse.peek_name()!='PF_SummonPulseFX':
        raise ValueError('Android8.7 summon-pulse identity mismatch')
    pulse_record={'name':pulse.peek_name(),'visuals':exporter.visuals(pulse)}
    for entry in pulse.read_typetree()['m_Component']:
        component=deref(pulse,entry['component'])
        if component.type.name!='MonoBehaviour':
            continue
        kind=component.parse_monobehaviour_head().m_Script.deref().read_typetree()['m_ClassName']
        data=read_mono(component)
        if kind=='TimeToLive':
            pulse_record['lifetime']=data['lifetime']
        elif kind=='SpriteFX':
            pulse_record['frames']=[exporter.icon(component,p) for p in data['sprites']]
            pulse_record['interval']=data['secPerFrame']
            pulse_record['loop']=bool(data['loop'])
    actors['pulsePrefab']=pulse_record
    chamber=[exporter.icon(owner,p) for p in source['chamberUIFrames']]
    ui=read_mono(deref(owner,source['chamberUIPrefab']))
    manifest={'actors':[dict(field=field,**record) for field,record in actors.items()],
              'chamber':chamber,'ui':ui,'sprites':list(exporter.sprites.values())}
    for sprite in manifest['sprites']:
        original=exporter.root/sprite['file']
        colored=original.with_suffix('.colorful.png')
        recolor(original,colored,'effect')
        sprite['colorful']=str(colored.relative_to(exporter.root)).replace('\\','/')
    (exporter.root/'manifest.json').write_text(json.dumps(manifest,indent=2),encoding='utf8')


def recolor(original,destination,role):
    """Palette edits only; alpha and every pixel position remain untouched."""
    source=Image.open(original).convert('RGBA')
    painted=source.copy()
    pixels=[]
    for r,g,b,a in source.getdata():
        rgb=(r,g,b)
        if a:
            if role=='effect':
                # Petal bursts and shell highlights use rose/cream rather than
                # an opaque white flash. Dark authored outlines stay intact.
                if min(r,g,b)>150 and max(r,g,b)-min(r,g,b)<65:
                    rgb=(255,round(g*.73),round(b*.83))
            elif g>r and g>=b and g-r<70:
                # The original green midtones become warm skin/leather,
                # keeping the cream hair, crimson mantle and navy outlines.
                rgb=(min(255,round(g*1.30)),round(g*.86),round(g*.66))
        pixels.append((*rgb,a))
    painted.putdata(pixels)
    if painted.size!=source.size or painted.getchannel('A').tobytes()!=source.getchannel('A').tobytes():
        raise ValueError('Saint Rosa palette changed sprite geometry')
    painted.save(destination)
