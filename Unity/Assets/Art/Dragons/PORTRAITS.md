# Dragon Portrait Art Mapping

All 7 portraits confirmed. Save each image as a PNG to the path below,
then run `DragonTD > Create Phase 1 Dragons` to auto-bind them.

| Dragon ID            | Save portrait here                           | Description |
|----------------------|----------------------------------------------|-------------|
| voltaris_001         | Art/Dragons/voltaris_001/portrait.png        | Dark purple/black dragon, gold scales, purple+gold lightning crackling through wings |
| frostfang_002        | Art/Dragons/frostfang_002/portrait.png       | Blue ice crystal dragon standing tall, exhaling frost breath |
| magmaclaw_003        | Art/Dragons/magmaclaw_003/portrait.png       | Red/orange dragon, lava-crack scales, breathing a cone of fire |
| tempest_glacion_004  | Art/Dragons/tempest_glacion_004/portrait.png | Blue/gold dragon — left wing ice crystals + snowflakes, right wing lightning (fusion) |
| stonehide_005        | Art/Dragons/stonehide_005/portrait.png       | Stocky mossy stone dragon, golden eyes ✓ already in project |
| celestara_006        | Art/Dragons/celestara_006/portrait.png       | White/gold feathered celestial dragon, golden halo, glowing orbs |
| shadowfang_007       | Art/Dragons/shadowfang_007/portrait.png      | Black/dark purple dragon, purple void flames, glowing violet eyes ✓ already in project |

## Asset status

| Dragon               | Portrait | Video (idle_anim.mp4) |
|----------------------|----------|-----------------------|
| voltaris_001         | Pending  | ✓ |
| frostfang_002        | Pending  | ✓ |
| magmaclaw_003        | Pending  | ✓ |
| tempest_glacion_004  | Pending  | ✓ |
| stonehide_005        | ✓        | ✓ |
| celestara_006        | Pending  | ✓ |
| shadowfang_007       | ✓        | ✓ |

All 7 images have been visually confirmed and mapped above — they just need to be
saved as .png files to the paths listed. The inline images in the chat session
are not automatically written to disk; export them from the chat and place them here.

## Import steps in Unity

1. Save each portrait PNG to `Art/Dragons/{id}/portrait.png`
2. Select each file in the Project window → Inspector → **Texture Type: Sprite (2D and UI)** → Apply
3. Run **DragonTD > Create Phase 1 Dragons** — portraits auto-bind to `visualData.portrait`
4. Run **DragonTD > Create DragonRegistry**
5. Run **DragonTD > Create Default Summon Pool**
