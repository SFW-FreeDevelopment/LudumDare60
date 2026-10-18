"""Import reproducible storefront compositions and export sources/PNGs through Aseprite MCP."""
import asyncio, json, os, sys
from pathlib import Path
from mcp import ClientSession, StdioServerParameters
from mcp.client.stdio import stdio_client
ROOT=Path(__file__).resolve().parent.parent
def lua(p): return json.dumps(str(p).replace('\\','/'))
async def main():
    env=dict(os.environ)
    env.setdefault('ASEPRITE_PATH',r'C:\Program Files (x86)\Steam\steamapps\common\Aseprite\aseprite.exe')
    script=''
    for name in ['cover','banner','background','embed']:
        script+=f'local spr=app.open({lua(ROOT/"_scratch/itch-art"/(name+".png"))}); assert(spr)\n'
        script+=f'spr.layers[1].name="Storefront composition"\n'
        script+=f'spr:saveAs({lua(ROOT/"art/itch/src"/(name+".aseprite"))})\n'
        script+=f'spr:saveAs({lua(ROOT/"art/itch"/(name+".png"))})\n'
    script+='print("ITCH_EXPORTED")'
    params=StdioServerParameters(command=sys.executable,args=['-m','aseprite_mcp'],cwd=sys.argv[1],env=env)
    async with stdio_client(params) as (read,write):
        async with ClientSession(read,write) as session:
            await session.initialize()
            result=await session.call_tool('run_lua_script',{'script':script})
            output='\n'.join(c.text for c in result.content if c.type=='text');print(output)
            if result.isError or 'ITCH_EXPORTED' not in output:raise RuntimeError('Storefront export failed')
asyncio.run(main())
