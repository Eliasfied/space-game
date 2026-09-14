"""Author modular Unity prefabs and update only the Aegis environment in Asterion.
Uses native cube meshes and the project's serialized component formats; no editor automation.
"""
from pathlib import Path
import hashlib, json, math, re

ROOT = Path(__file__).resolve().parents[1]
SCENE = ROOT / 'Assets/Scenes/Asterion.unity'
PREFIX = 'Assets/Resources/Environment/Aegis'
HEADER = '%YAML 1.1\n%TAG !u! tag:unity3d.com,2011:\n'
BASE = 7000000000000000

def guid(path):
    meta = ROOT / (path + '.meta')
    if meta.exists():
        return re.search(r'^guid: (\w+)', meta.read_text(), re.M)[1]
    return hashlib.md5(('aegis-level:' + path).encode()).hexdigest()

def write(path, content, importer='NativeFormatImporter', file_id=0):
    dest = ROOT / path
    dest.parent.mkdir(parents=True, exist_ok=True)
    dest.write_text(content, encoding='utf-8', newline='\n')
    meta = Path(str(dest) + '.meta')
    if not meta.exists():
        extra = f'  mainObjectFileID: {file_id}\n' if file_id else ''
        meta.write_text(f'fileFormatVersion: 2\nguid: {guid(path)}\n{importer}:\n  externalObjects: {{}}\n{extra}  userData: \n  assetBundleName: \n  assetBundleVariant: \n')

def vec(v):
    return '{' + ', '.join(f'{k}: {n:.7g}' for k, n in zip('xyzw', v)) + '}'

source = SCENE.read_text(encoding='utf-8')
blocks = re.findall(r'--- !u!\d+ &-?\d+[^\n]*\n.*?(?=--- !u!|\Z)', source, re.S)
docs = {int(re.search(r'&(-?\d+)', b)[1]): b for b in blocks}

def field(text, key, value):
    result, count = re.subn(r'^  ' + re.escape(key) + r':[^\n]*', '  ' + key + ': ' + value, text, flags=re.M)
    assert count == 1, (key, count)
    return result

def template(class_id, object_id=None):
    return next(b for b in blocks if b.startswith(f'--- !u!{class_id} ') and
                (object_id is None or f'  m_GameObject: {{fileID: {object_id}}}\n' in b))

renderer_template = template(23, 2118621401)
box_template = template(65, 2118621401)
light_template = template(108)

def common(go):
    return f'  m_ObjectHideFlags: 0\n  m_CorrespondingSourceObject: {{fileID: 0}}\n  m_PrefabInstance: {{fileID: 0}}\n  m_PrefabAsset: {{fileID: 0}}\n  m_GameObject: {{fileID: {go}}}\n'

def node(go, name, parent=0, children=(), position=(0,0,0), scale=(1,1,1), yaw=0, components=()):
    a = math.radians(yaw)/2
    ids = (go+1, *components)
    obj = f'--- !u!1 &{go}\nGameObject:\n  m_ObjectHideFlags: 0\n  m_CorrespondingSourceObject: {{fileID: 0}}\n  m_PrefabInstance: {{fileID: 0}}\n  m_PrefabAsset: {{fileID: 0}}\n  serializedVersion: 6\n  m_Component:\n'
    obj += ''.join(f'  - component: {{fileID: {i}}}\n' for i in ids)
    obj += f'  m_Layer: 0\n  m_Name: {name}\n  m_TagString: Untagged\n  m_Icon: {{fileID: 0}}\n  m_NavMeshLayer: 0\n  m_StaticEditorFlags: 0\n  m_IsActive: 1\n'
    obj += f'--- !u!4 &{go+1}\nTransform:\n' + common(go)
    obj += f'  serializedVersion: 2\n  m_LocalRotation: {vec((0,math.sin(a),0,math.cos(a)))}\n  m_LocalPosition: {vec(position)}\n  m_LocalScale: {vec(scale)}\n  m_ConstrainProportionsScale: 0\n'
    obj += '  m_Children:' + ('\n' + ''.join(f'  - {{fileID: {i}}}\n' for i in children) if children else ' []\n')
    return obj + f'  m_Father: {{fileID: {parent}}}\n  m_LocalEulerAnglesHint: {{x: 0, y: {yaw}, z: 0}}\n'

def copy_component(text, new_id, go):
    text = re.sub(r'&-?\d+', f'&{new_id}', text, count=1)
    return field(text, 'm_GameObject', f'{{fileID: {go}}}')

palette = {
    'Deck': ((.56,.61,.64),.22,.28,0), 'DeckDark': ((.46,.52,.56),.25,.28,0),
    'Graphite': ((.13,.17,.21),.5,.3,0), 'Armor': ((.3,.36,.41),.55,.34,0),
    'Bronze': ((.48,.32,.18),.65,.36,0), 'Ivory': ((.76,.8,.79),.25,.3,0),
    'Recess': ((.035,.048,.06),.1,.2,0), 'Crimson': ((.85,.045,.085),.3,.4,1.8),
    'Guide': ((.24,.76,.81),.2,.35,1.5), 'WhiteLight': ((.92,.91,.8),.1,.35,1.4),
}
material_template = (ROOT/'Assets/Art/Materials/Floor.mat').read_text().split('--- !u!114')[0]
for name, (color, metal, smooth, emission) in palette.items():
    text = field(material_template, 'm_Name', 'Aegis '+name)
    text = field(text, 'm_EnableInstancingVariants', '1')
    if emission: text = text.replace('  m_ValidKeywords: []', '  m_ValidKeywords:\n  - _EMISSION')
    for key, val in [('_Metallic',metal),('_Smoothness',smooth)]:
        text = re.sub(r'(- '+key+r': )[^\n]*', lambda m: m[1]+str(val), text)
    for key, rgb in [('_BaseColor',color),('_Color',color),('_EmissionColor',tuple(c*emission for c in color))]:
        value = '{r: %g, g: %g, b: %g, a: 1}' % rgb
        text = re.sub(r'(- '+key+r': )[^\n]*', lambda m: m[1]+value, text)
    write(f'{PREFIX}/Materials/{name}.mat', text, file_id=2100000)

def part(name, pos, size, mat, yaw=0): return (name,pos,size,mat,yaw)

def prefab(name, parts, collider=None):
    existing=ROOT/f'{PREFIX}/{name}.prefab'
    # The imported wall visuals keep these original prefab GUIDs and colliders.
    # Regenerating the level must not put the former blockout meshes back.
    if name in ('WallModule','LowWall') and existing.exists() and 'Crimson wall visual' in existing.read_text():return
    if name in ('Cover','Terminal','Pillar') and existing.exists() and 'Imported Aegis prop' in existing.read_text():return
    root = 100000
    ids = [root+10+i*10 for i in range(len(parts))]
    colliders = collider if isinstance(collider,list) else ([collider] if collider else [])
    output = HEADER + node(root, 'Aegis '+name, children=[i+1 for i in ids], components=[root+2+i for i in range(len(colliders))])
    for i,(size,center) in enumerate(colliders):
        box = copy_component(box_template, root+2+i, root)
        output += field(field(box,'m_Size',vec(size)), 'm_Center',vec(center))
    for go, (label,pos,size,mat,yaw) in zip(ids,parts):
        output += node(go,label,root+1,position=pos,scale=size,yaw=yaw,components=(go+2,go+3))
        output += f'--- !u!33 &{go+2}\nMeshFilter:\n'+common(go)+'  m_Mesh: {fileID: 10202, guid: 0000000000000000e000000000000000, type: 0}\n'
        rend = copy_component(renderer_template,go+3,go)
        rend = re.sub(r'(  m_Materials:\n)(?:  -[^\n]*\n)+', lambda m:m[1]+f'  - {{fileID: 2100000, guid: {guid(f"{PREFIX}/Materials/{mat}.mat")}, type: 2}}\n',rend)
        output += rend
    write(f'{PREFIX}/{name}.prefab', output, 'PrefabImporter')

for name, surface in [('FloorTile','Deck'),('FloorTileDark','DeckDark')]:
    prefab(name,[part('Deck substrate',(0,-.15,0),(4,.3,4),'Graphite'),part('Inset alloy plate',(0,-.005,0),(3.9,.03,3.9),surface),
        part('Service seam',(0,.012,1.64),(2.8,.014,.025),'Armor'),part('Bronze ID tab',(-1.55,.018,-1.64),(.4,.016,.055),'Bronze')],((4,.3,4),(0,-.15,0)))
wall=[part('Armored spine',(0,1.55,0),(4,3.1,.7),'Graphite'),part('Recessed panel',(0,1.68,-.4),(3.6,2.38,.12),'Armor'),
      part('Upper cap',(0,3.04,0),(4.04,.18,.96),'Bronze'),part('Lower skirt',(0,.35,-.45),(3.8,.62,.18),'Graphite'),
      part('Light strip',(0,2.65,-.48),(2.3,.07,.035),'WhiteLight'),part('Crimson conduit',(1.55,1.55,-.48),(.075,1.7,.04),'Crimson')]
for side in [-1,1]: wall.append(part('Ivory brace',(side*1.86,1.55,-.45),(.2,2.8,.2),'Ivory'))
prefab('WallModule',wall,((4,3.15,.8),(0,1.575,0)))
prefab('LowWall',[part('Low armored parapet',(0,.57,0),(4,1.14,.7),'Graphite'),part('Bronze top',(0,1.15,0),(4.05,.12,.85),'Bronze'),
    part('Inset armor',(0,.65,-.39),(3.6,.65,.08),'Armor'),part('Rim light',(0,1.22,0),(2.7,.025,.07),'WhiteLight')],((4,1.25,.8),(0,.625,0)))
prefab('Pillar',[part('Base shoe',(0,.18,0),(1.25,.36,1.25),'Graphite'),part('Graphite column',(0,1.8,0),(.8,3.3,.8),'Graphite'),
    part('Bronze armor',(0,1.9,-.45),(.55,2.8,.2),'Bronze'),part('Red spine',(0,1.9,-.56),(.09,2.25,.045),'Crimson'),
    part('Crown',(0,3.5,0),(1.2,.24,1.2),'Ivory')],((1.25,3.65,1.25),(0,1.825,0)))
prefab('DoorFrame',[part('Left jamb',(-4.5,1.9,0),(1,3.8,1.2),'Graphite'),part('Right jamb',(4.5,1.9,0),(1,3.8,1.2),'Graphite'),
    part('Header',(0,3.9,0),(10,.5,1.1),'Bronze'),part('Header inset',(0,3.9,-.58),(6.8,.16,.09),'Ivory'),
    part('Left edge',(-4.05,1.8,-.65),(.08,3.3,.045),'Crimson'),part('Right edge',(4.05,1.8,-.65),(.08,3.3,.045),'Crimson')],
    [((1,3.8,1.2),(-4.5,1.9,0)),((1,3.8,1.2),(4.5,1.9,0)),((10,.5,1.1),(0,3.9,0))])
prefab('DoorLeaf',[part('Door slab',(0,1.8,0),(3.98,3.6,.48),'Graphite'),part('Upper armor',(0,2.6,-.3),(3.65,1.45,.16),'Armor'),
    part('Lower armor',(0,.95,-.3),(3.65,1.45,.16),'Armor'),part('Bronze separator',(0,1.8,-.43),(3.75,.18,.16),'Bronze'),
    part('Security light',(0,2.6,-.41),(.075,1,.04),'Crimson'),part('Door rail',(0,.16,-.39),(3.75,.13,.12),'Ivory')],((3.98,3.6,.55),(0,1.8,0)))
prefab('Cover',[part('Footing',(0,.12,0),(2.8,.24,1.4),'Graphite'),part('Armor',(0,.65,0),(2.6,1.08,1.18),'Armor'),
    part('Lid',(0,1.22,0),(2.8,.18,1.4),'Bronze'),part('Front inset',(0,.65,-.62),(2.15,.7,.1),'Graphite'),
    part('ID stripe',(0,.75,-.68),(1.3,.07,.04),'Ivory')],((2.8,1.32,1.4),(0,.66,0)))
prefab('Terminal',[part('Foot',(0,.12,0),(1.35,.24,.9),'Graphite'),part('Housing',(0,.85,0),(1.05,1.5,.65),'Armor'),
    part('Display bezel',(0,1.43,-.4),(1.25,.8,.25),'Graphite'),part('Display',(0,1.45,-.54),(1,.54,.025),'Guide'),
    part('Keypad',(0,1.03,-.5),(.85,.12,.23),'Bronze')],((1.35,1.85,1.1),(0,.925,-.12)))
prefab('FloorGuide',[part('Recess',(0,.018,0),(.16,.02,3.75),'Graphite'),part('Guide strip',(0,.034,0),(.075,.012,3.3),'Guide')])

# Deterministic IDs make regeneration local to this generator's own additions.
docs = {i:b for i,b in docs.items() if not BASE <= i < BASE+1000000}
main, static = BASE, BASE+10
created, static_children, root_children = [], [], [static+1]
counter = BASE+100
instances = []
def place(module, at, yaw=0, scale=(1,1,1), dynamic=False):
    global counter
    instance, transform = counter,counter+1
    counter += 10
    g = guid(f'{PREFIX}/{module}.prefab')
    parent = main+1 if dynamic else static+1
    a=math.radians(yaw)/2
    changes={'m_LocalPosition.'+k:v for k,v in zip('xyz',at)}
    changes.update({'m_LocalRotation.'+k:v for k,v in zip('xyzw',(0,math.sin(a),0,math.cos(a)))})
    changes.update({'m_LocalScale.'+k:v for k,v in zip('xyz',scale)})
    body=f'--- !u!1001 &{instance}\nPrefabInstance:\n  m_ObjectHideFlags: 0\n  serializedVersion: 2\n  m_Modification:\n    serializedVersion: 3\n    m_TransformParent: {{fileID: {parent}}}\n    m_Modifications:\n'
    for key,val in changes.items(): body+=f'    - target: {{fileID: 100001, guid: {g}, type: 3}}\n      propertyPath: {key}\n      value: {val:.7g}\n      objectReference: {{fileID: 0}}\n'
    body+='    m_RemovedComponents: []\n    m_RemovedGameObjects: []\n    m_AddedGameObjects: []\n    m_AddedComponents: []\n'
    body+=f'  m_SourcePrefab: {{fileID: 100100000, guid: {g}, type: 3}}\n--- !u!4 &{transform} stripped\nTransform:\n  m_CorrespondingSourceObject: {{fileID: 100001, guid: {g}, type: 3}}\n  m_PrefabInstance: {{fileID: {instance}}}\n  m_PrefabAsset: {{fileID: 0}}\n'
    created.append(body)
    (root_children if dynamic else static_children).append(transform)
    instances.append(dict(module=module,position=at,yaw=yaw,scale=scale))
    return transform

for ix,x in enumerate(range(-18,20,4)):
    for iz,z in enumerate(range(-16,22,4)): place('FloorTileDark' if (ix+iz)%5==0 else 'FloorTile',(x,0,z))
for x in [-2.5,2.5]:
    for z in range(-44,-18,4): place('FloorTile',(x,0,z),scale=(1.25,1,1))
for x in range(-18,20,4):
    place('WallModule',(x,0,22))
    if abs(x)>4: place('LowWall',(x,0,-18),180)
for z in range(-16,22,4):
    place('WallModule',(-20,0,z),-90);place('WallModule',(20,0,z),90)
for z in range(-44,-18,4):
    place('WallModule',(-5,0,z),-90);place('WallModule',(5,0,z),90)
for x in [-2.5,2.5]: place('WallModule',(x,0,-46),180,(1.25,1,1))
for x in [-18.8,18.8]:
    for z in [-15,2,19]: place('Pillar',(x,0,z),-90 if x<0 else 90)
for x,z in [(-12,-6),(12,-4),(-12,12),(12,14)]: place('Cover',(x,0,z))
for x in [-3.9,3.9]: place('Terminal',(x,0,-37),-90 if x<0 else 90)
for x in [-3.9,3.9]:
    for z in range(-44,-18,4): place('FloorGuide',(x,0,z))
for x in [-18.5,18.5]:
    for z in range(-14,20,4): place('FloorGuide',(x,0,z))
place('DoorFrame',(0,0,-18))
left=place('DoorLeaf',(-2,0,-18),dynamic=True)
right=place('DoorLeaf',(2,0,-18),dynamic=True)
created.insert(0,node(static,'Aegis / Static architecture',main+1,static_children))
created.insert(0,node(main,'AEGIS / Warden approach',children=root_children))

# Preserve the old environment as an inactive hierarchy rather than deleting it.
for i,b in list(docs.items()):
    if b.startswith('--- !u!1 ') and any(f'  m_Name: {n}\n' in b for n in ['ARENA / SECTOR 07','Training unit','Reactor glow']):
        docs[i]=field(b,'m_IsActive','0')
docs[1499894655]=field(docs[1499894655],'m_LocalPosition',vec((0,.12,-43)))
docs[855557968]=field(docs[855557968],'m_LocalPosition',vec((0,0,9)))
for i,b in list(docs.items()):
    if 'AsterionGame.FollowCamera\n' in b:
        b=field(b,'viewSize','8.2')
        if '  perspectiveVersion:' in b: b=field(b,'perspectiveVersion','3')
        else: b+='  perspectiveVersion: 3\n'
        docs[i]=b
        camera_go=int(re.search(r'm_GameObject: \{fileID: (\d+)\}',b)[1])
        for transform_id,transform_block in list(docs.items()):
            if transform_block.startswith('--- !u!4 ') and f'  m_GameObject: {{fileID: {camera_go}}}\n' in transform_block:
                camera_position=(0,.65+20*math.sin(math.radians(55)),-43+1.1-20*math.cos(math.radians(55)))
                docs[transform_id]=field(transform_block,'m_LocalPosition',vec(camera_position))
    if b.startswith('--- !u!20 '): docs[i]=field(b,'orthographic size','8.2')
    if b.startswith('--- !u!108 ') and '  m_Type: 1\n' in b:
        is_key='  m_GameObject: {fileID: 1078857316}' in b
        b=field(b,'m_Intensity','2.25' if is_key else '.9')
        b=field(b,'m_Color','{r: 0.93, g: 0.97, b: 1, a: 1}' if is_key else '{r: 1, g: 0.88, b: 0.73, a: 1}')
        docs[i]=b
docs[2]=field(field(field(docs[2],'m_AmbientSkyColor','{r: 0.52, g: 0.61, b: 0.7, a: 1}'),'m_AmbientEquatorColor','{r: 0.36, g: 0.43, b: 0.5, a: 1}'),'m_AmbientGroundColor','{r: 0.23, g: 0.27, b: 0.32, a: 1}')
level_component=BASE+80
control=docs[1835649644]
control=re.sub(r'  - component: \{fileID: '+str(level_component)+r'\}\n','',control)
control=control.replace('  m_Layer: 0',f'  - component: {{fileID: {level_component}}}\n  m_Layer: 0')
docs[1835649644]=control
created.append(f'--- !u!114 &{level_component}\nMonoBehaviour:\n'+common(1835649644)+f'  m_Enabled: 1\n  m_EditorHideFlags: 0\n  m_Script: {{fileID: 11500000, guid: {guid("Assets/Scripts/Level/AegisLevel.cs")}, type: 3}}\n  m_Name: \n  m_EditorClassIdentifier: Assembly-CSharp::AsterionGame.AegisLevel\n  staticArchitecture: {{fileID: {static+1}}}\n  gateLeft: {{fileID: {left}}}\n  gateRight: {{fileID: {right}}}\n')
root_id=9223372036854775807
roots=re.sub(r'  - \{fileID: '+str(main+1)+r'\}\n','',docs.pop(root_id))
roots+=f'  - {{fileID: {main+1}}}\n'
generated=''.join(created).replace('  m_Name: \n','  m_Name:\n')
SCENE.write_text(HEADER+''.join(docs.values())+generated+roots,encoding='utf-8',newline='\n')

# Lower bloom/contrast/vignette without changing character materials.
volume=ROOT/'Assets/Settings/AsterionVolume.asset'
text=volume.read_text()
for component, values in [('ColorAdjustments',{'postExposure':'.3','contrast':'6'}),('Vignette',{'intensity':'.1'}),('Bloom',{'intensity':'.4'})]:
    def update(match):
        block=match[0]
        if f'  m_Name: {component}\n' not in block: return block
        for key,value in values.items(): block=re.sub(r'(  '+key+r':\n    m_OverrideState: 1\n    m_Value: )[^\n]*',lambda m:m[1]+value,block)
        return block
    text=re.sub(r'--- !u!.*?(?=--- !u!|\Z)',update,text,flags=re.S)
volume.write_text(text,encoding='utf-8',newline='\n')
cover_collider=re.search(r'  m_Size: \{x: ([\d.e+-]+), y: ([\d.e+-]+), z: ([\d.e+-]+)\}',(ROOT/f'{PREFIX}/Cover.prefab').read_text())
cover_width,_,cover_depth=map(float,cover_collider.groups())
layout={'rooms':[{'x':-20,'z':-18,'width':40,'depth':40},{'x':-5,'z':-46,'width':10,'depth':28}],
        'covers':[{'x':x-cover_width/2,'z':z-cover_depth/2,'width':cover_width,'depth':cover_depth} for x,z in [(-12,-6),(12,-4),(-12,12),(12,14)]],
        'guards':[{'x':-1.5,'y':0,'z':-26},{'x':1.5,'y':0,'z':-26},{'x':0,'y':0,'z':-23.5}],
        'gateZ':-18,'playerSpawn':{'x':0,'y':.12,'z':-43},'bossSpawn':{'x':0,'y':0,'z':9}}
write(f'{PREFIX}/LevelLayout.json',json.dumps(layout,indent=2)+'\n','TextScriptImporter')
write('Tools/AegisLevelInstances.json',json.dumps(instances,indent=2)+'\n','DefaultImporter')
# Meta files for scripts and folders are authored now, keeping scene references stable.
for path in ['Assets/Scripts/Level/AegisLevel.cs','Assets/Scripts/Presentation/AegisMinimap.cs']:
    meta=ROOT/(path+'.meta')
    if not meta.exists(): meta.parent.mkdir(parents=True,exist_ok=True);meta.write_text(f'fileFormatVersion: 2\nguid: {guid(path)}\n')
for base in [ROOT/PREFIX,ROOT/'Assets/Scripts/Level']:
    for folder in [base,*base.rglob('*'),*base.parents]:
        if not folder.is_dir() or folder==ROOT or folder==ROOT/'Assets' or ROOT not in folder.parents: continue
        if not str(folder.relative_to(ROOT)).startswith('Assets'): continue
        rel=folder.relative_to(ROOT).as_posix();meta=ROOT/(rel+'.meta')
        if not meta.exists(): meta.write_text(f'fileFormatVersion: 2\nguid: {guid(rel)}\nfolderAsset: yes\nDefaultImporter:\n  externalObjects: {{}}\n  userData: \n  assetBundleName: \n  assetBundleVariant: \n')
print(f'Authored 10 modular prefabs, {len(palette)} materials, {len(instances)} scene instances and connected level layout.')
