# Rebuilding the pixel art

Run `npm run art` to generate the sheet, send it through the installed Aseprite MCP server, save the
editable `.aseprite` with named slices, export its PNG, and audit palette/slice bounds. Pixel rules are
in `.art/generate.js`; game assets are embedded from `export/`, so players and clean clones never
need Aseprite or the MCP server installed.

The same command exports the hero, logo and cloud crops for the WebGL loading screen. Its CSS palette
and SVG pixel lettering are generated from `Theme.cs` and `PixelFont.cs` by `tools/build-boot.cjs`,
so the page before WASM loads follows the same art and type rules as the game.

World sign and recipient-tag slices also use the shared glyph masks. Their board, lettering and post
are exported together, so camera travel moves one sprite rather than independently rounded text.

Art-authoring prerequisites: Node, uv, Aseprite, and the Python Aseprite MCP server. Set
`ASEPRITE_MCP_DIR` to the server checkout and `ASEPRITE_PATH` to the executable if they are outside
the usual `~/Repos/aseprite-mcp` and Steam locations. The build reuses the same stdio entry point as
the user's OpenCode MCP configuration; it does not modify that configuration or other MCP entries.
