"""Reproducible source/slice/PNG export through the user's installed Aseprite MCP server."""
import asyncio, json, os, sys, re
from pathlib import Path
from mcp import ClientSession, StdioServerParameters
from mcp.client.stdio import stdio_client

ROOT=Path(__file__).resolve().parent.parent
SERVER=Path(sys.argv[1]).resolve()
def lua(value): return json.dumps(str(value).replace('\\','/'))

async def main():
    env=dict(os.environ)
    env.setdefault('ASEPRITE_PATH',r'C:\Program Files (x86)\Steam\steamapps\common\Aseprite\aseprite.exe')
    slices=json.loads((ROOT/'art/export/slices.json').read_text())
    script=f'local spr=app.open({lua(ROOT/"art/src/sheet-draft.png")})\nassert(spr,"draft failed to open")\n'
    script+='spr.layers[1].name="Generated pixel art"\n'
    colors=sorted(set(re.findall(r'"(#[0-9a-fA-F]{6})"',(ROOT/'art/.art/generate.js').read_text())))
    script+=f'local pal=Palette({len(colors)})\n'
    for i,color in enumerate(colors):
        channels=[int(color[n:n+2],16) for n in (1,3,5)]
        script+=f'pal:setColor({i},Color({channels[0]},{channels[1]},{channels[2]}))\n'
    script+='spr:setPalette(pal)\n'
    for rect in slices:
        script+=f'local s=spr:newSlice(Rectangle({rect["x"]},{rect["y"]},{rect["w"]},{rect["h"]})); s.name={lua(rect["name"])}\n'
    script+=f'spr:saveAs({lua(ROOT/"art/src/balloonatic.aseprite")})\n'
    script+=f'spr:saveAs({lua(ROOT/"art/export/sheet.png")})\n'
    script+='print("EXPORTED "..spr.width.."x"..spr.height)\n'
    params=StdioServerParameters(command=sys.executable,args=['-m','aseprite_mcp'],cwd=str(SERVER),env=env)
    async with stdio_client(params) as (read,write):
        async with ClientSession(read,write) as session:
            await session.initialize()
            result=await session.call_tool('run_lua_script',{'script':script})
            text='\n'.join(c.text for c in result.content if c.type=='text')
            print(text)
            if result.isError or 'EXPORTED ' not in text: raise RuntimeError('Aseprite export failed')
    await asyncio.sleep(.2)

asyncio.run(main())
