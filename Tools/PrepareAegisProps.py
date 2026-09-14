"""Prepare URP texture channels and metadata for the three supplied static props."""
from pathlib import Path
import hashlib,re
from PIL import Image,ImageChops,ImageOps

ROOT=Path(__file__).resolve().parents[1]
ASSETS=ROOT/'Assets/Art/MeshyEnvironment/Props'
TEMPLATES=ROOT/'Assets/Art/MeshyEnemies/Warden/Textures'
REVISION='aegis-props-1'

def guid(path):
    meta=Path(str(path)+'.meta')
    if meta.exists():return re.search(r'^guid: (\w+)',meta.read_text(),re.M)[1]
    return hashlib.md5(('aegis-props:'+path.relative_to(ROOT).as_posix()).encode()).hexdigest()

for name in ['NeonVault','ArcReactor','SideConsole']:
    asset=ASSETS/name;textures=asset/'Textures'
    metallic=Image.open(textures/'Metallic.png').convert('L');roughness=Image.open(textures/'Roughness.png').convert('L')
    albedo=Image.open(textures/'BaseColor.png').convert('RGB')
    assert metallic.size==roughness.size==albedo.size
    Image.merge('RGBA',(metallic,metallic,metallic,ImageOps.invert(roughness))).save(textures/'MetallicSmoothness.png')
    r,g,b=albedo.split()
    if name=='ArcReactor':
        dominance=ImageChops.subtract(ImageChops.darker(g,b),r).point(lambda value:255 if value>45 else 0)
        bright=ImageChops.lighter(g,b).point(lambda value:255 if value>110 else 0)
    else:
        dominance=ImageChops.subtract(r,ImageChops.lighter(g,b)).point(lambda value:255 if value>80 else 0)
        bright=r.point(lambda value:255 if value>110 else 0)
    mask=ImageChops.multiply(dominance,bright)
    Image.composite(albedo,Image.new('RGB',albedo.size),mask).save(textures/'Emission.png')
    for path in textures.glob('*.png'):
        template='Normal.png.meta' if path.name=='Normal.png' else 'BaseColor.png.meta' if path.name in ['BaseColor.png','Emission.png'] else 'MetallicSmoothness.png.meta'
        text=(TEMPLATES/template).read_text()
        text=re.sub(r'^guid: \w+','guid: '+guid(path),text,flags=re.M)
        text=re.sub(r'^  userData:.*','  userData: '+REVISION,text,flags=re.M)
        text=re.sub(r'^    aniso:.*','    aniso: 4',text,flags=re.M)
        Path(str(path)+'.meta').write_text(text,encoding='utf-8',newline='\n')
    model=asset/'Model.fbx';meta=Path(str(model)+'.meta')
    if not meta.exists():meta.write_text(f'fileFormatVersion: 2\nguid: {guid(model)}\nModelImporter:\n  serializedVersion: 24600\n  externalObjects: {{}}\n  userData:\n',newline='\n')
    for folder in [ASSETS,asset,textures]:
        meta=Path(str(folder)+'.meta')
        if not meta.exists():meta.write_text(f'fileFormatVersion: 2\nguid: {guid(folder)}\nfolderAsset: yes\nDefaultImporter:\n  externalObjects: {{}}\n  userData:\n  assetBundleName:\n  assetBundleVariant:\n',newline='\n')
    print(f'{name}: {albedo.width}x{albedo.height}; {mask.histogram()[255]} emissive pixels.')
for name in ['AegisPropImportSettings','AegisPropSetup']:
    script=ROOT/('Assets/Editor/'+name+'.cs');meta=Path(str(script)+'.meta')
    if not meta.exists():meta.write_text(f'fileFormatVersion: 2\nguid: {guid(script)}\n',newline='\n')
