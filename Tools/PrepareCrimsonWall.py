"""Prepare Unity texture channels and stable metadata for the supplied wall FBX.
The original FBX, albedo, normal, metallic and roughness files are kept unchanged.
"""
from pathlib import Path
import hashlib,re
from PIL import Image,ImageChops,ImageOps

ROOT=Path(__file__).resolve().parents[1]
ASSET=ROOT/'Assets/Art/MeshyEnvironment/CrimsonWall'
TEXTURES=ASSET/'Textures'

metallic=Image.open(TEXTURES/'Metallic.png').convert('L')
roughness=Image.open(TEXTURES/'Roughness.png').convert('L')
albedo=Image.open(TEXTURES/'BaseColor.png').convert('RGB')
assert metallic.size==roughness.size==albedo.size,'Wall texture dimensions disagree'
# URP Lit reads metallic from red and smoothness (1 - roughness) from alpha.
Image.merge('RGBA',(metallic,metallic,metallic,ImageOps.invert(roughness))).save(TEXTURES/'MetallicSmoothness.png')
# Only the saturated crimson conduits emit; bronze armor remains unlit.
r,g,b=albedo.split()
red_dominance=ImageChops.subtract(r,ImageChops.lighter(g,b)).point(lambda value:255 if value>80 else 0)
bright=r.point(lambda value:255 if value>110 else 0)
mask=ImageChops.multiply(red_dominance,bright)
Image.composite(albedo,Image.new('RGB',albedo.size),mask).save(TEXTURES/'Emission.png')

def guid(path):
    meta=Path(str(path)+'.meta')
    if meta.exists():return re.search(r'^guid: (\w+)',meta.read_text(),re.M)[1]
    return hashlib.md5(('aegis-crimson-wall:'+path.relative_to(ROOT).as_posix()).encode()).hexdigest()

templates=ROOT/'Assets/Art/MeshyEnemies/Warden/Textures'
for path in TEXTURES.glob('*.png'):
    template='Normal.png.meta' if path.name=='Normal.png' else 'BaseColor.png.meta' if path.name in ['BaseColor.png','Emission.png'] else 'MetallicSmoothness.png.meta'
    text=(templates/template).read_text()
    text=re.sub(r'^guid: \w+','guid: '+guid(path),text,flags=re.M)
    text=re.sub(r'^  userData:.*','  userData: aegis-crimson-wall-1',text,flags=re.M)
    text=re.sub(r'^    aniso:.*','    aniso: 4',text,flags=re.M)
    Path(str(path)+'.meta').write_text(text,encoding='utf-8',newline='\n')

model=ASSET/'Model.fbx'
if not Path(str(model)+'.meta').exists():
    # The scoped AssetPostprocessor supplies model import settings on first import.
    Path(str(model)+'.meta').write_text(f'fileFormatVersion: 2\nguid: {guid(model)}\nModelImporter:\n  serializedVersion: 24600\n  externalObjects: {{}}\n  userData:\n',newline='\n')
for folder in [ASSET.parent,ASSET,TEXTURES]:
    meta=Path(str(folder)+'.meta')
    if not meta.exists():meta.write_text(f'fileFormatVersion: 2\nguid: {guid(folder)}\nfolderAsset: yes\nDefaultImporter:\n  externalObjects: {{}}\n  userData:\n  assetBundleName:\n  assetBundleVariant:\n',newline='\n')
for name in ['AegisWallImportSettings','AegisWallSetup']:
    script=ROOT/('Assets/Editor/'+name+'.cs');meta=Path(str(script)+'.meta')
    if not meta.exists():meta.write_text(f'fileFormatVersion: 2\nguid: {guid(script)}\n',newline='\n')
print(f'Prepared {albedo.width}x{albedo.height} wall textures; {mask.histogram()[255]} emissive pixels; stable Unity metadata.')
