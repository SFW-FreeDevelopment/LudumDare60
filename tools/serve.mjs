// Static file server for the published WebGL build, matching what an LD-hosting provider does:
// brotli where a .br sidecar exists, gzip next, raw last. Playwright boots against this, so it is the
// same code path the entry page uses (measured payload here is the payload the player downloads).
import { createServer } from 'node:http';
import { createReadStream, existsSync, statSync } from 'node:fs';
import { dirname, extname, join, normalize, resolve, sep } from 'node:path';
import { fileURLToPath } from 'node:url';

// ROOT overrides the served directory (the zip check serves an unzipped archive; a fresh clone uses
// its own export/) so the same brotli-aware host proves every artifact the entry pipeline makes.
const root = process.env.ROOT
  ? resolve(process.env.ROOT)
  : resolve(dirname(fileURLToPath(import.meta.url)), '..', 'export', 'web', 'wwwroot');
const port = Number(process.env.PORT || 4173);
const encoded = process.env.RAW !== undefined ? false : true;

const types = new Map(Object.entries({
  '.html': 'text/html; charset=utf-8',
  '.js': 'text/javascript; charset=utf-8',
  '.mjs': 'text/javascript; charset=utf-8',
  '.css': 'text/css; charset=utf-8',
  '.json': 'application/json',
  '.wasm': 'application/wasm',
  '.dll': 'application/octet-stream',
  '.dat': 'application/octet-stream',
  '.br': 'application/octet-stream',
  '.gz': 'application/octet-stream',
  '.png': 'image/png',
  '.jpg': 'image/jpeg',
  '.gif': 'image/gif',
  '.ico': 'image/x-icon',
  '.svg': 'image/svg+xml',
  '.woff2': 'font/woff2',
  '.map': 'application/json',
  '.xml': 'application/xml',
  '.ogg': 'audio/ogg',
  '.mp3': 'audio/mpeg',
  '.txt': 'text/plain; charset=utf-8',
}));

function dirnameOf(urlPath) {
  return urlPath.slice(0, urlPath.lastIndexOf('/'));
}

function pick(file) {
  if (encoded) {
    if (existsSync(file + '.br')) return { path: file + '.br', encoding: 'br' };
    if (existsSync(file + '.gz')) return { path: file + '.gz', encoding: 'gzip' };
  }
  return { path: file, encoding: null };
}

function resolvePath(url) {
  const clean = normalize(decodeURIComponent(url.split('?')[0])).replace(/^(\.\.[/\\])+/, '');
  let target = join(root, clean);
  if (!target.startsWith(root + sep) && target !== root) return null;
  if (!existsSync(target)) return null;
  if (statSync(target).isDirectory()) target = join(target, 'index.html');
  return existsSync(target) ? target : null;
}

createServer((req, res) => {
  let url = req.url === '/' ? '/index.html' : req.url;
  let file = resolvePath(url);

  // Blazor is a single-page app rooted at ./ ; unknown paths fall back to index.html.
  if (!file && !extname(url)) {
    file = join(root, 'index.html');
    url = '/index.html';
  }

  if (!file) {
    res.writeHead(404, { 'content-type': 'text/plain; charset=utf-8' });
    res.end(`404 ${url}`);
    return;
  }

  const { path, encoding } = pick(file);
  const type = types.get(extname(file)) || 'application/octet-stream';
  res.writeHead(200, {
    'content-type': type,
    'content-length': statSync(path).size,
    'cache-control': 'no-cache',
    ...(encoding ? { 'content-encoding': encoding } : {}),
  });
  createReadStream(path).pipe(res);
}).listen(port, () => {
  console.log(`serving ${root} on http://localhost:${port} (${encoded ? 'brotli/gzip aware' : 'raw'})`);
});
