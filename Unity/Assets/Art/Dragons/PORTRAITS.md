# Dragon Portrait Art Mapping

Place the portrait PNG files here, named exactly as listed. The Editor factory
auto-binds them when you run `DragonTD > Create Phase 1 Dragons`.

| Dragon ID            | Portrait file path                              | Source image |
|----------------------|-------------------------------------------------|--------------|
| voltaris_001         | Art/Dragons/voltaris_001/portrait.png           | Dark purple/gold lightning dragon (image 1 in session) |
| frostfang_002        | Art/Dragons/frostfang_002/portrait.png          | Blue ice crystal dragon (image 4 in session) |
| magmaclaw_003        | Art/Dragons/magmaclaw_003/portrait.png          | Red fire-breathing dragon (image 5 in session) |
| tempest_glacion_004  | Art/Dragons/tempest_glacion_004/portrait.png    | Blue/gold ice+lightning fusion dragon (image 2 in session) |
| stonehide_005        | Art/Dragons/stonehide_005/portrait.png          | (no portrait yet — use placeholder) |
| celestara_006        | Art/Dragons/celestara_006/portrait.png          | White/gold celestial feathered dragon (image 3 in session) |
| shadowfang_007       | Art/Dragons/shadowfang_007/portrait.png         | (no portrait yet — use placeholder) |

## Idle Animation Videos

Already copied to each dragon folder as `idle_anim.mp4`:
- frostfang_002, magmaclaw_003, stonehide_005, celestara_006, shadowfang_007

voltaris_001 and tempest_glacion_004 need idle_anim.mp4 added when available.

## How to import in Unity

1. Drag portrait PNGs into the correct Art/Dragons/{id}/ folder
2. In Import Settings set Texture Type = Sprite (2D and UI)
3. Run `DragonTD > Create Phase 1 Dragons` — portraits and videos auto-bind
