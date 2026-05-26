# Dragon Portrait Art Mapping

Place portrait PNG files here named exactly as listed. The Editor factory
auto-binds them when you run `DragonTD > Create Phase 1 Dragons`.

| Dragon ID            | Portrait file                                | Description | Portrait | Video |
|----------------------|----------------------------------------------|-------------|----------|-------|
| voltaris_001         | Art/Dragons/voltaris_001/portrait.png        | Dark purple/gold lightning dragon | Pending | ✓ |
| frostfang_002        | Art/Dragons/frostfang_002/portrait.png       | Blue ice crystal dragon | Pending | ✓ |
| magmaclaw_003        | Art/Dragons/magmaclaw_003/portrait.png       | Red/orange fire-breathing dragon | Pending | ✓ |
| tempest_glacion_004  | Art/Dragons/tempest_glacion_004/portrait.png | Blue/gold ice+lightning fusion dragon | Pending | ✓ |
| stonehide_005        | Art/Dragons/stonehide_005/portrait.png       | Stocky mossy stone dragon, golden eyes | **Ready** | ✓ |
| celestara_006        | Art/Dragons/celestara_006/portrait.png       | White/gold feathered celestial dragon | Pending | ✓ |
| shadowfang_007       | Art/Dragons/shadowfang_007/portrait.png      | Black/purple void flame dragon | **Ready** | ✓ |

Videos: **7/7 complete** — one `idle_anim.mp4` per dragon folder.
Portraits: **2/7 ready** — stonehide_005, shadowfang_007.

## How to import in Unity

1. Save portrait PNGs to `Art/Dragons/{id}/portrait.png`
2. Set **Texture Type = Sprite (2D and UI)** in Import Settings
3. Run `DragonTD > Create Phase 1 Dragons` — portraits and videos auto-bind to `visualData`
4. Run `DragonTD > Create DragonRegistry` and `DragonTD > Create Default Summon Pool`
