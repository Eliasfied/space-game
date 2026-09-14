"""Check authored references and collision-space routes without starting Unity."""
from pathlib import Path
from collections import deque
import json, math, re

ROOT=Path(__file__).resolve().parents[1]
ENV=ROOT/'Assets/Resources/Environment/Aegis'
layout=json.loads((ENV/'LevelLayout.json').read_text())
instances=json.loads((ROOT/'Tools/AegisLevelInstances.json').read_text())

def references(path):
    text=path.read_text(encoding='utf-8')
    ids=re.findall(r'^--- !u!\d+ &(-?\d+)',text,re.M)
    assert len(ids)==len(set(ids)),f'Duplicate object ID: {path}'
    local=set(re.findall(r'\{fileID: (-?\d+)\}',text)) - {'0'}
    missing=local-set(ids)
    assert not missing,f'Unresolved local references: {path}: {missing}'
    return text

scene=references(ROOT/'Assets/Scenes/Asterion.unity')
for prefab in ENV.glob('*.prefab'): references(prefab)
assert scene.count('AsterionGame.AegisLevel\n')==1
assert scene.count('  m_Name: AEGIS / Warden approach\n')==1
assert re.search(r'm_Name: ARENA / SECTOR 07\n(?:(?!---).)*m_IsActive: 0',scene,re.S)
assert re.search(r'm_Name: Training unit\n(?:(?!---).)*m_IsActive: 0',scene,re.S)
guids={re.search(r'^guid: (\w+)',p.read_text(),re.M)[1] for p in ENV.rglob('*.meta')}
environment_assets=ROOT/'Assets/Art/MeshyEnvironment'
guids.update(re.search(r'^guid: (\w+)',p.read_text(),re.M)[1] for p in environment_assets.rglob('*.meta'))
guids.add(re.search(r'^guid: (\w+)',(ROOT/'Assets/Scripts/Level/AegisLevel.cs.meta').read_text(),re.M)[1])
for prefab in ENV.glob('*.prefab'):
    for g in re.findall(r'guid: (\w+)',prefab.read_text()):
        assert g in guids or g=='0000000000000000e000000000000000',(prefab,g)

def vector(block,key):
    m=re.search(r'^  '+key+r': \{x: ([\d.e+-]+), y: ([\d.e+-]+), z: ([\d.e+-]+)\}',block,re.M)
    assert m,key
    return tuple(map(float,m.groups()))

obstacles=[]
for item in instances:
    text=(ENV/(item['module']+'.prefab')).read_text()
    for collider in re.findall(r'--- !u!65 .*?(?=--- !u!|\Z)',text,re.S):
        size=vector(collider,'m_Size');center=vector(collider,'m_Center')
        px,py,pz=item['position'];sx,sy,sz=item['scale'];a=math.radians(item['yaw'])
        bottom=py+(center[1]-size[1]/2)*sy;top=py+(center[1]+size[1]/2)*sy
        if top<=.05: continue
        cosine,sine=math.cos(a),math.sin(a)
        cx=px+center[0]*sx*cosine+center[2]*sz*sine
        cz=pz-center[0]*sx*sine+center[2]*sz*cosine
        ex=(abs(size[0]*sx*cosine)+abs(size[2]*sz*sine))/2
        ez=(abs(size[0]*sx*sine)+abs(size[2]*sz*cosine))/2
        obstacles.append((cx,cz,ex,ez,bottom,item['module']=='DoorLeaf'))

def on_floor(x,z,arena_only=False):
    rooms=layout['rooms'][:1] if arena_only else layout['rooms']
    return any(r['x']<=x<=r['x']+r['width'] and r['z']<=z<=r['z']+r['depth'] for r in rooms)

def free(x,z,gate_open,radius=.36,height=2.05,arena_only=False):
    if not all(on_floor(x+radius*math.cos(a*math.pi/4),z+radius*math.sin(a*math.pi/4),arena_only) for a in range(8)):return False
    for cx,cz,ex,ez,bottom,door in obstacles:
        if bottom>height:continue
        if door and gate_open:cx+=4.4 if cx>0 else -4.4
        dx=max(abs(x-cx)-ex,0);dz=max(abs(z-cz)-ez,0)
        if dx*dx+dz*dz<radius*radius:return False
    return True

def reachable(gate_open,spawn=None,radius=.36,height=2.05,arena_only=False):
    spawn=spawn or layout['playerSpawn'];start=(round(spawn['x']*2),round(spawn['z']*2))
    assert free(start[0]/2,start[1]/2,gate_open,radius,height,arena_only),'Spawn overlaps level geometry'
    queue=deque([start]);seen={start}
    while queue:
        x,z=queue.popleft()
        for dx,dz in [(0,1),(0,-1),(1,0),(-1,0)]:
            n=(x+dx,z+dz)
            if n in seen or not free(n[0]/2,n[1]/2,gate_open,radius,height,arena_only) or not free(x/2+dx/4,z/2+dz/4,gate_open,radius,height,arena_only):continue
            seen.add(n);queue.append(n)
    return seen

assert len(layout['guards'])==3
def distance(a,b):return math.hypot(a['x']-b['x'],a['z']-b['z'])
assert min(distance(g,layout['playerSpawn']) for g in layout['guards'])>12,'Guard pack too close to entry'
assert max(distance(a,b) for a in layout['guards'] for b in layout['guards'])<=4,'Guards are scattered instead of grouped'
closed=reachable(False);opened=reachable(True)
for guard in layout['guards']:
    assert (round(guard['x']*2),round(guard['z']*2)) in closed,'Guard inaccessible before opening gate'
boss=(round(layout['bossSpawn']['x']*2),round(layout['bossSpawn']['z']*2))
assert boss not in closed,'Closed gate can be bypassed'
assert boss in opened,'Open gate does not connect to boss'
for x,z in [(-9,-9),(9,-9),(-9,16),(9,16)]: assert (x*2,z*2) in opened,'Boss arena combat lane blocked'
assert len(opened)>len(closed)*3,'Arena unexpectedly restricted'
assert layout['rooms'][0]['width']==40 and layout['rooms'][0]['depth']==40
boss_routes=reachable(True,layout['bossSpawn'],radius=1.17,height=4.2,arena_only=True)
for cover in layout['covers']:
    cx=cover['x']+cover['width']/2;cz=cover['z']+cover['depth']/2
    for dx,dz in [(-3.5,0),(3.5,0),(0,-2.5),(0,2.5)]:
        assert (round((cx+dx)*2),round((cz+dz)*2)) in boss_routes,'Boss cannot reach a side of cover'
print(f'PASS: native references; distant 3-guard pack; closed/open gate routes ({len(opened)} player grid points); boss-sized routes around all four covers ({len(boss_routes)} grid points).')
