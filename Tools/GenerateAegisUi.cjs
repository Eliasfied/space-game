// Run with Node.js and sharp installed (or NODE_PATH pointing to its package directory).
// The SVG files are editable sources; Unity uses the exported transparent PNG textures.
const fs = require('fs');
const path = require('path');
const crypto = require('crypto');
const sharp = require('sharp');
const root = path.resolve(__dirname, '..');
const source = path.join(root, 'Assets/Art/UI/Aegis/Source');
const output = path.join(root, 'Assets/Resources/UI/Aegis');
fs.mkdirSync(source, { recursive: true });
fs.mkdirSync(output, { recursive: true });
const svg = (w, h, body) => `<svg xmlns="http://www.w3.org/2000/svg" width="${w}" height="${h}" viewBox="0 0 ${w} ${h}">${body}</svg>`;
const defs = `<defs>
 <linearGradient id="metal" x2=".3" y2="1"><stop stop-color="#a2b7c0"/><stop offset=".18" stop-color="#617681"/><stop offset=".5" stop-color="#34434d"/><stop offset="1" stop-color="#657e88"/></linearGradient>
 <linearGradient id="panel" x2="0" y2="1"><stop stop-color="#24343e"/><stop offset="1" stop-color="#101c24"/></linearGradient>
 <linearGradient id="glass" x2=".5" y2="1"><stop stop-color="#294652"/><stop offset="1" stop-color="#111f29"/></linearGradient>
</defs>`;
const frame = (hex, mask = false, glow = false) => {
 const outer = hex ? 'M64 2L121 34V106L64 138L7 106V34Z' : 'M10 2H84L93 11V99L84 108H10L1 99V11Z';
 const middle = hex ? 'M64 7L117 37V103L64 133L11 103V37Z' : 'M12 6H82L89 13V97L82 104H12L5 97V13Z';
 const inner = hex ? 'M64 14L111 41V99L64 126L17 99V41Z' : 'M14 12H80L83 16V94L79 98H15L11 94V16Z';
 const border = hex ? 'M64 18L107 43V97L64 122L21 97V43Z' : 'M16 16H78L79 18V92L77 94H17L15 92V18Z';
 if(mask)return svg(hex?128:94,hex?140:110,`<path d="${inner}" fill="white"/>`);
 if(glow)return svg(hex?128:94,hex?140:110,`<path d="${border}" fill="none" stroke="white" stroke-width="1.5"/><path d="${hex?'M48 126L64 135L80 126':'M35 103H59'}" fill="none" stroke="white" stroke-width="3"/>`);
 return svg(hex?128:94,hex?140:110,defs+`<path d="${outer}" fill="#050c12" stroke="#040a0f" stroke-width="2"/>
 <path d="${middle}" fill="url(#metal)" stroke="#acc3cc" stroke-opacity=".4"/>
 <path d="${inner}" fill="url(#glass)" stroke="#060e15" stroke-width="3"/>
 <path d="${border}" fill="none" stroke="#7e9da9" stroke-opacity=".3"/>
 ${hex?'<path d="M14 42V65M114 75V98" stroke="#8da7b0" stroke-width="2"/>':'<path d="M4 26V39M90 72V85" stroke="#8da7b0" stroke-width="2"/><path d="M22 7H42M52 103H72" stroke="#c4d7db" stroke-opacity=".3"/>'}`);
};
const art = {
 'minimap-frame': svg(228,244,defs+`<path d="M8 1H214L227 14V231L215 243H8L1 236V8Z" fill="#070f15" stroke="#03090d"/>
 <path d="M9 3H213L224 15V230L213 240H9L4 235V9Z" fill="url(#metal)"/>
 <path d="M10 6H211L221 17V228L211 237H10L7 234V10Z" fill="url(#panel)"/>
 <path d="M13 4H57M171 240H210" stroke="#9cb7c1" stroke-opacity=".55"/>
 <path d="M15 24H213M15 218H213" stroke="#4e6671" stroke-opacity=".55"/>
 <path d="M14 3H39" stroke="#2be1d7"/>`),
 'minimap-dot': svg(16,16,'<circle cx="8" cy="8" r="7" fill="white" stroke="#08151b" stroke-width="2"/>'),
 'minimap-player': svg(20,20,'<path d="M10 1L18 18L10 14L2 18Z" fill="white" stroke="#07131b" stroke-width="1.5"/>'),
 'minimap-boss': svg(20,20,'<path d="M10 1L19 10L10 19L1 10Z" fill="white" stroke="#07131b" stroke-width="2"/>'),
 'proc-glow': svg(80,80,`<defs><filter id="halo" x="-40%" y="-40%" width="180%" height="180%"><feGaussianBlur stdDeviation="2.4"/></filter></defs>
 <rect x="10" y="10" width="60" height="60" rx="3" fill="none" stroke="white" stroke-width="4" opacity=".8" filter="url(#halo)"/>
 <rect x="10" y="10" width="60" height="60" rx="3" fill="none" stroke="white" stroke-width="2"/>`),
 'unit-frame': svg(320,100,defs+`<path d="M9 1H306L319 14V85L305 99H9L1 91V9Z" fill="#050c12" stroke="#050b10"/>
 <path d="M10 3H305L316 15V84L304 96H10L4 90V10Z" fill="url(#metal)"/>
 <path d="M12 7H302L312 17V82L301 92H12L8 88V12Z" fill="url(#panel)" stroke="#0b1720"/>
 <path d="M16 9H71M249 94H300M314 24V42" stroke="#a2bac4" stroke-opacity=".5"/>
 <path d="M16 16H62V62H16Z" fill="#0e1c26" stroke="#58717c" stroke-width="1"/>
 <path d="M13 21V15H25M53 63H63V53" fill="none" stroke="#94b2bd" stroke-opacity=".65"/>
 <path d="M73 43H299" stroke="#506570" stroke-opacity=".55"/>`),
 'resource-stack-frame': svg(248,60,defs+`<path d="M7 1H241L247 7V53L241 59H7L1 53V7Z" fill="#071019" stroke="#030b10"/>
 <path d="M8 3H240L245 8V52L240 57H8L3 52V8Z" fill="url(#metal)"/>
 <path d="M10 6H238L242 10V50L238 54H10L6 50V10Z" fill="url(#panel)"/>
 <path d="M10 4H77M171 56H238" fill="none" stroke="#a2bbc5" stroke-opacity=".65"/>
 <path d="M10 30H238" stroke="#52717d" stroke-opacity=".3"/>`),
 'boss-frame': svg(560,22,defs+`<path d="M8 1H552L559 8V14L552 21H8L1 14V8Z" fill="#070f15" stroke="#080d12"/>
 <path d="M9 3H551L557 9V13L551 19H9L3 13V9Z" fill="url(#metal)"/>
 <path d="M11 5H549L553 9V13L549 17H11L7 13V9Z" fill="#171219" stroke="#7f756b" stroke-opacity=".6"/>
 <path d="M12 3H62M498 3H548M12 19H40M520 19H548" stroke="#a2bbc5" stroke-opacity=".5"/>
 <path d="M4 10V12M556 10V12" stroke="#5bb1b9" stroke-opacity=".65"/>`),
 'resource-frame': svg(248,36,defs+`<path d="M7 1H241L247 7V29L241 35H7L1 29V7Z" fill="#071019" stroke="#030b10"/>
 <path d="M8 3H240L245 8V28L240 33H8L3 28V8Z" fill="url(#metal)"/>
 <path d="M10 6H238L242 10V26L238 30H10L6 26V10Z" fill="url(#panel)"/>
 <path d="M10 4H77M171 32H238" fill="none" stroke="#a2bbc5" stroke-opacity=".65" stroke-width="1"/>
 <path d="M8 21H240" stroke="#52717d" stroke-opacity=".5"/>`),
 'compact-frame': svg(64,64,defs+`<rect x=".5" y=".5" width="63" height="63" fill="#071019" stroke="#050b11"/>
 <rect x="2" y="2" width="60" height="60" fill="url(#metal)"/>
 <rect x="4" y="4" width="56" height="56" fill="url(#panel)" stroke="#080f17"/>
 <path d="M3 16V3H25M61 48V61H39" fill="none" stroke="#a4bdc7" stroke-opacity=".45" stroke-width=".8"/>
 <path d="M8 59H56" stroke="#050d14"/>`),
 'compact-mask': svg(64,64,'<rect x="5" y="5" width="54" height="54" fill="white"/>'),
 'compact-glow': svg(64,64,'<rect x="5.5" y="5.5" width="53" height="53" fill="none" stroke="white" stroke-width=".65"/><path d="M25 61H39" stroke="white" stroke-width="1.5"/>'),
 'slot-frame': frame(false), 'slot-mask': frame(false,true), 'slot-glow': frame(false,false,true),
 'ultimate-frame': frame(true), 'ultimate-mask': frame(true,true), 'ultimate-glow': frame(true,false,true),
 'chassis': svg(980,158,defs+`<path d="M18 2H176L188 11H788L802 2H954L978 26V126L951 154H810L798 145H189L177 154H27L2 129V19Z" fill="#060d13" stroke="#050a0e" stroke-width="3"/>
 <path d="M20 7H173L185 17H791L805 7H951L972 29V123L948 148H813L800 139H185L174 148H30L8 126V22Z" fill="url(#metal)"/>
 <path d="M26 14H168L181 24H796L809 14H946L965 33V120L944 140H817L804 132H181L170 140H34L15 122V27Z" fill="url(#panel)" stroke="#0b141c" stroke-width="2"/>
 <path d="M32 17H166L176 25M817 16H944L960 33M38 136H167M818 136H942" fill="none" stroke="#8cabb6" stroke-opacity=".45" stroke-width="2"/>
 <path d="M16 47V72M965 83V110M190 144H254M720 144H788" fill="none" stroke="#26e1de" stroke-width="3"/>
 <path d="M189 20H790" stroke="#7a929c" stroke-opacity=".3"/>
 <path d="M800 37V119" stroke="#627a85" stroke-opacity=".5"/>
 <path d="M21 87V107M25 87V107M955 48V68M959 48V68" stroke="#060d14" stroke-width="2"/>
 <g fill="#14252d" stroke="#79909a" stroke-width=".8"><circle cx="24" cy="31" r="2"/><circle cx="950" cy="33" r="2"/><circle cx="33" cy="127" r="2"/><circle cx="944" cy="126" r="2"/></g>`),
 'gauge-frame': svg(440,32,defs+`<path d="M10 1H429L439 11V21L429 31H10L1 21V11Z" fill="url(#metal)" stroke="#071119" stroke-width="2"/><path d="M12 5H427L434 13V19L427 27H12L6 19V13Z" fill="#0c1921" stroke="#809ba6" stroke-opacity=".5"/><path d="M19 2H69M361 30H415" stroke="#30d8d7" stroke-width="2"/>`),
};
const icons = {
 'explosive-shot': `<path d="M7 13L27 27M5 23L19 32M17 5L31 20"/><path d="M34 17L38 29L53 23L46 37L58 44L43 46L39 59L31 47L19 52L24 39L16 32L30 31Z"/><circle cx="36" cy="38" r="5" fill="white" stroke="none"/>`,
 'twin-pulses': `<path d="M7 17H28V28H21L17 44H8L13 28H7ZM36 17H57V28H50L46 44H37L42 28H36Z" fill="white"/><path d="M8 12H27M37 12H56M24 33L20 48M53 33L49 48"/>`,
 'jet-burst': `<rect x="16" y="12" width="12" height="27" rx="3"/><rect x="36" y="12" width="12" height="27" rx="3"/><path d="M28 21H36M28 33H36M19 45L22 56L26 45M39 45L42 56L46 45M9 21V37M55 21V37"/><path d="M25 7H39"/>`,
 'ion-grenade': `<path d="M26 9H39V17H26Z" fill="white"/><path d="M38 10H48V22M26 18L17 28V46L26 54H39L47 46V28L38 18Z"/><path d="M35 24L25 37H33L29 48L41 33H33Z" fill="white" stroke="none"/>`,
 'pulse-kick': `<path d="M14 9L29 15L25 30L39 38L52 34L58 43L45 50L30 46L13 35L8 28Z" fill="white" stroke="none"/><path d="M4 43L18 49M7 51L20 56M44 21L51 15M50 27H58"/>`,
 'repulsor-grenade': `<circle cx="32" cy="33" r="10"/><path d="M28 18V13H36V18M15 22L7 30L15 38M7 30H18M49 22L57 30L49 38M46 30H57M25 50L32 57L39 50M32 45V57M30 30L34 36"/>`,
 'charged-shot': `<path d="M10 23V11H22M42 11H54V23M54 42V54H42M22 54H10V42"/><path d="M44 16L23 33H33L19 49L43 29H32Z" fill="white" stroke="none"/><path d="M10 32H16M48 32H54"/>`,
 'aegis-shield': `<path d="M32 6L53 14L49 38L42 48L32 56L22 48L15 38L11 14Z"/><path d="M32 13L46 18L43 36L32 47L21 36L18 18Z" stroke-opacity=".45" stroke-width="2"/><path d="M33 19L24 33H32L28 43L41 28H33Z" fill="white" stroke="none"/>`,
 'overdrive': `<path d="M11 7Q32 1 53 7L39 36H25Z" stroke-opacity=".45" stroke-width="2"/><path d="M20 10L28 30M32 8V29M44 10L36 30"/><path d="M12 37H29V46H23L20 58H13L16 46H12ZM35 37H52V46H48L45 58H38L41 46H35Z" fill="white" stroke="none"/>`,
 'pulse-burst': `<path d="M6 22H40L46 17H55V29H43L35 36H22L16 47H8L13 31H6Z" fill="white" stroke="none"/><path d="M18 17H37M31 36L36 44M42 36H53M43 43H59M22 26H37"/><path d="M19 25H37" stroke="#000" stroke-width="2"/>`,
 'overcharge-railgun': `<path d="M8 17H19M4 27H15M8 37H19M25 22L18 45H27L34 30H48L56 22Z"/><path d="M24 15H57M38 35L46 43M41 48H58M48 39L58 48L48 57"/>`,
 'graviton-dash': `<path d="M7 15H24M4 26H19M7 37H21M13 49H26"/><path d="M28 12L47 30L28 49M39 12L58 30L39 49" stroke-width="6"/><path d="M6 55H41" stroke-width="2"/>`,
 'supply-drone': `<path d="M23 17H41L46 25L39 33H25L18 25Z"/><path d="M18 25H8V15M46 25H56V15M4 12H15M49 12H60M8 30V36M56 30V36"/><path d="M32 35L44 40L41 51L32 58L23 51L20 40Z"/><path d="M32 41V51M27 46H37" stroke-width="3"/>`,
 'concussive-blast': `<path d="M6 25H25L34 32L25 39H6Z" fill="white" stroke="none"/><path d="M37 17Q52 32 37 47M47 9Q68 32 47 55M8 17H20M8 47H20"/>`,
 'tractor-beam': `<path d="M13 10V25Q13 40 27 40Q41 40 41 25V10M13 20H22V10M32 10V20H41"/><path d="M20 53L27 46L34 53M27 46V59M48 21H58M48 30H55M45 39H52"/>`,
 'targeting-matrix': `<path d="M8 22V9H22M42 9H56V22M56 42V55H42M22 55H8V42"/><circle cx="32" cy="32" r="12"/><circle cx="32" cy="32" r="3" fill="white"/><path d="M32 14V23M32 41V50M14 32H23M41 32H50" stroke-width="3"/>`,
 'orbital-strike': `<ellipse cx="30" cy="47" rx="23" ry="9"/><path d="M50 7L32 30M40 7L25 27M56 15L39 36"/><path d="M22 30L37 36L30 49L18 41Z" fill="white" stroke="none"/><path d="M9 30L14 34M47 42L55 37"/>`,
};
for(const [name, body] of Object.entries(icons))art[name] = svg(64,64,`<g fill="none" stroke="white" stroke-width="3.5" stroke-linecap="round" stroke-linejoin="round">${body}</g>`);
function meta(file, texture = false) {
 if(fs.existsSync(file+'.meta'))return;
 const guid=crypto.createHash('md5').update(path.relative(root,file).replaceAll('\\','/')).digest('hex');
 fs.writeFileSync(file+'.meta',`fileFormatVersion: 2\nguid: ${guid}\n`+(texture?`TextureImporter:
  serializedVersion: 13
  mipmaps:
    enableMipMap: 0
    sRGBTexture: 1
  isReadable: 0
  textureSettings:
    serializedVersion: 2
    filterMode: 1
    aniso: 0
    wrapU: 1
    wrapV: 1
    wrapW: 1
  nPOTScale: 0
  alphaIsTransparency: 1
  textureType: 2
  textureShape: 1
  maxTextureSize: 2048
  platformSettings:
  - serializedVersion: 4
    buildTarget: DefaultTexturePlatform
    maxTextureSize: 2048
    textureFormat: -1
    textureCompression: 0
    compressionQuality: 100
    overridden: 0
`:'DefaultImporter:\n  externalObjects: {}\n'));
}
(async()=>{
 for(const [name, data] of Object.entries(art)){
  const src=path.join(source,name+'.svg'),dest=path.join(output,name+'.png');
  fs.writeFileSync(src,data);meta(src);
  await sharp(Buffer.from(data),{density:name==='chassis'?144:216}).png().toFile(dest);meta(dest,true);
 }
 for(const dir of ['Assets/Art/UI','Assets/Art/UI/Aegis','Assets/Art/UI/Aegis/Source','Assets/Resources/UI','Assets/Resources/UI/Aegis']){
  const file=path.join(root,dir);if(!fs.existsSync(file+'.meta'))fs.writeFileSync(file+'.meta',`fileFormatVersion: 2\nguid: ${crypto.createHash('md5').update(dir).digest('hex')}\nfolderAsset: yes\nDefaultImporter:\n  externalObjects: {}\n`);
 }
 console.log(`Exported ${Object.keys(art).length} Aegis UI textures and editable SVG sources.`);
})().catch(error=>{console.error(error);process.exitCode=1;});
