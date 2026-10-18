const fs = require('fs');
const path = require('path');
const {spawnSync} = require('child_process');
const root = path.resolve(__dirname, '..');
const server = process.env.ASEPRITE_MCP_DIR || path.join(process.env.USERPROFILE || process.env.HOME, 'Repos', 'aseprite-mcp');
if (!fs.existsSync(path.join(server,'pyproject.toml'))) throw Error('Set ASEPRITE_MCP_DIR to the installed Aseprite MCP server.');
for (const script of ['pack.js','render.js']) {
 const result=spawnSync(process.execPath,[path.join(root,'art','.art',script)],{cwd:root,stdio:'inherit'});
 if(result.status!==0)process.exit(result.status || 1);
}
const result=spawnSync('uv',['run','--project',server,path.join(__dirname,'export-art.py'),server],{cwd:root,stdio:'inherit',env:process.env});
if(result.status!==0)process.exit(result.status || 1);
const audit=spawnSync(process.execPath,[path.join(root,'art','.art','audit.js')],{cwd:root,stdio:'inherit'});
process.exit(audit.status === 0 ? 0 : 1);
