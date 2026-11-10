import { build } from 'esbuild'; import fs from 'node:fs';
const r = await build({ entryPoints: ['src/main.js'], bundle: true, minify: true, format: 'iife', write: false, target: 'es2020' });
const js = r.outputFiles[0].text.replace(/<\/script/g, '<\\/script');
const fdir = 'node_modules/@fontsource/fredoka/files/';
const face = w => `@font-face{font-family:'Fredoka';font-weight:${w};font-display:swap;src:url(data:font/woff2;base64,${fs.readFileSync(fdir + `fredoka-latin-${w}-normal.woff2`).toString('base64')}) format('woff2')}`;
const html = fs.readFileSync('src/index.tpl.html', 'utf8').replace('/*FONTS*/', () => [500, 700].map(face).join('')).replace('/*APP*/', () => js);
fs.mkdirSync('web', { recursive: true }); fs.writeFileSync('web/index.html', html);
console.log((html.length / 1024).toFixed(0) + ' KB');
