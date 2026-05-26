# Dragon Portrait Art Mapping

Place portrait PNG files here named exactly as listed. The Editor factory
auto-binds them when you run `DragonTD > Create Phase 1 Dragons`.

| Dragon ID            | Portrait file path                           | Description | Status |
|----------------------|----------------------------------------------|-------------|--------|
| voltaris_001         | Art/Dragons/voltaris_001/portrait.png        | Dark purple/gold lightning dragon — electric crackling wings | Pending |
| frostfang_002        | Art/Dragons/frostfang_002/portrait.png       | Blue ice crystal dragon — frost breath | Pending |
| magmaclaw_003        | Art/Dragons/magmaclaw_003/portrait.png       | Red/orange fire-breathing dragon | Pending |
| tempest_glacion_004  | Art/Dragons/tempest_glacion_004/portrait.png | Blue/gold dragon — one ice wing, one lightning wing (fusion) | Pending |
| stonehide_005        | Art/Dragons/stonehide_005/portrait.png       | Stocky mossy stone dragon, golden eyes | **Ready** |
| celestara_006        | Art/Dragons/celestara_006/portrait.png       | White/gold feathered celestial dragon with halo | Pending |
| shadowfang_007       | Art/Dragons/shadowfang_007/portrait.png      | Black/dark purple dragon with glowing purple void flames | **Ready** |

## Animation Videos

| Dragon ID            | idle_anim.mp4 | attack_anim.mp4 |
|----------------------|--------------|-----------------|
| voltaris_001         | ✓            | ✗ pending       |
| frostfang_002        | ✓            | ✓               |
| magmaclaw_003        | ✓            | ✓               |
| tempest_glacion_004  | ✓            | ✗ pending       |
| stonehide_005        | ✓            | ✓               |
| celestara_006        | ✓            | ✓               |
| shadowfang_007       | ✓            | ✓               |

## How to import in Unity

1. Save portrait PNGs to the correct `Art/Dragons/{id}/portrait.png` path
2. In Import Settings set **Texture Type = Sprite (2D and UI)**
3. Run `DragonTD > Create Phase 1 Dragons` — portraits and videos auto-bind
4. Run `DragonTD > Create DragonRegistry` and `DragonTD > Create Default Summon Pool`
