# Dragon Dominion — Art Generation Guide

Complete asset pipeline for generating all game art, sprites, tiles, and audio.
All tools are free-tier or low-cost.

---

## PRIORITY 1: Fix Existing 7 Dragon Portraits (30 min)

**Problem:** Portraits have white backgrounds — they render as white squares on dark UI.

**Tool:** [remove.bg](https://remove.bg) — free, instant, no account needed.

**Steps:**
1. Go to remove.bg
2. Upload each portrait from `Unity/Assets/Art/Dragons/{id}/portrait.png`
3. Click Download → **Download PNG** (free, 0.25x resolution) OR create a free account → full resolution
4. Rename downloaded file to `portrait.png`
5. Overwrite the existing file at the same path
6. Repeat for all 7: voltaris_001, frostfang_002, magmaclaw_003, tempest_glacion_004, stonehide_005, celestara_006, shadowfang_007

**After:** Run `Dragon Dominion > ★ Build Battle Scene` in Unity.

---

## PRIORITY 2: Generate 3 New Dragon Portraits (45 min)

**Match the existing style:** AI-generated, white background, full-body dragon illustration with elemental effects, painterly/realistic.

**Best tool:** [Bing Image Creator](https://bing.com/images/create) — free, no subscription, DALL-E 3 quality.
**Alternative:** Midjourney (better quality, ~$10/mo).

**Base style suffix — add to ALL dragon prompts:**
```
fantasy dragon full body illustration, white background, dynamic pose, detailed scales,
elemental magical effects, painterly style, game art quality, high detail,
professional concept art, clean transparent-friendly background
```

---

### Emberveil_008 (Fire/Celestial, Epic)

```
A majestic dragon covered in ember-like golden scales with celestial flame patterns,
fiery orange and gold wings, small elegant build, surrounded by swirling celestial
fire energy and embers, glowing amber eyes, fantasy dragon full body illustration,
white background, dynamic pose, detailed scales, painterly style, game art quality,
high detail, professional concept art
```

Save as: `Unity/Assets/Art/Dragons/emberveil_008/portrait.png`

---

### Tideclaw_009 (Water/Frost, Rare)

```
A sleek water dragon with blue-green shimmering scales, sharp fins along its back
and tail, webbed claws, surrounded by swirling water and tidal energy, aqua and
teal color palette, elegant aquatic pose, fantasy dragon full body illustration,
white background, dynamic pose, detailed scales, painterly style, game art quality,
high detail, professional concept art
```

Save as: `Unity/Assets/Art/Dragons/tideclaw_009/portrait.png`

---

### Zephyrwing_010 (Wind/Storm, Uncommon)

```
A lightweight nimble dragon with translucent wind-swept wings, pale cyan and white
scales, long flowing tail, surrounded by swirling wind currents and small storm
clouds, emerald green eyes, fast agile pose, fantasy dragon full body illustration,
white background, dynamic pose, detailed scales, painterly style, game art quality,
high detail, professional concept art
```

Save as: `Unity/Assets/Art/Dragons/zephyrwing_010/portrait.png`

**After generating all 3:** Run through remove.bg immediately, then run Build Battle Scene.

---

## PRIORITY 3: Map Tile Sets (90 min)

All tiles must be:
- **512×512 pixels**, PNG
- **Square**, seamless (edges must match neighboring tiles cleanly)
- Style: **stylized painterly, top-down 3/4 view, no pixel art**

Use **Bing Image Creator** for each tile.

---

### Chapter 1 — Forest/Meadow Tiles

**Grass (buildable) tile:**
```
top-down 3/4 view game tile, lush green grass with small wildflowers and pebbles,
seamless tileable texture, stylized painterly, fantasy game art, 512x512,
solid fill edge to edge, no border, clean game asset
```
Save as: `Unity/Assets/Art/UI/ch1_grass.png`

**Dirt path tile (straight horizontal):**
```
top-down 3/4 view game tile, worn dirt path with small stones and grass edges,
brown earthy texture, seamless tileable, stylized painterly, fantasy game art,
512x512, solid fill edge to edge, no border, clean game asset
```
Save as: `Unity/Assets/Art/UI/ch1_path_h.png`

**Dirt path tile (straight vertical):**
```
top-down 3/4 view game tile, dirt path running vertically top to bottom,
worn brown earth with stone edges, seamless tileable, stylized painterly,
fantasy game art, 512x512, solid fill edge to edge
```
Save as: `Unity/Assets/Art/UI/ch1_path_v.png`

**Corner tile top-left (┌ — path enters from top, exits right):**
```
top-down 3/4 view game tile, dirt path turning corner from top to right (L-shape),
grass fills the other areas, seamless tileable, stylized painterly,
fantasy game art, 512x512, solid fill edge to edge
```
Save as: `Unity/Assets/Art/UI/ch1_corner_tl.png`

Repeat for the 3 remaining corners (adapt prompt direction):
- `ch1_corner_tr.png` — path from top, exits left (┐)
- `ch1_corner_bl.png` — path from bottom, exits right (└)
- `ch1_corner_br.png` — path from bottom, exits left (┘)

---

### Chapter 2 — Ice/Snow Tiles

**Snow ground tile:**
```
top-down 3/4 view game tile, snowy ground with ice crystals and frost patterns,
cold blue-white palette, seamless tileable texture, stylized painterly,
fantasy game art, 512x512, solid fill edge to edge, no border
```
Save as: `Unity/Assets/Art/UI/ch2_snow.png`

**Frozen path tile (horizontal):**
```
top-down 3/4 view game tile, frozen ice path with snow edges and frost cracks,
light blue icy surface, seamless tileable, stylized painterly,
fantasy game art, 512x512, solid fill edge to edge
```
Save as: `Unity/Assets/Art/UI/ch2_path_h.png`

**Frozen path tile (vertical):**
Same prompt adapted for vertical orientation.
Save as: `Unity/Assets/Art/UI/ch2_path_v.png`

**4 corner tiles:** Same corner prompts as Ch1 but with ice/frost theme.
Save as: `ch2_corner_tl.png`, `ch2_corner_tr.png`, `ch2_corner_bl.png`, `ch2_corner_br.png`

---

### Chapter 3 — Volcano Tiles (augment existing)

Already have: `volcano_ground.png` (buildable) and `volcano_path.png` (straight path).
Still need: vertical path + 4 corners.

**Lava path vertical:**
```
top-down 3/4 view game tile, molten lava path running vertically, dark volcanic
rock on sides, orange-red glowing lava texture, seamless tileable,
stylized painterly, fantasy game art, 512x512, solid fill edge to edge
```
Save as: `Unity/Assets/Art/UI/vol_path_v.png`

**Lava corner top-left (┌):**
```
top-down 3/4 view game tile, molten lava path turning corner from top to right,
dark volcanic basalt fills corner areas, orange-red glowing lava,
seamless tileable, stylized painterly, 512x512, solid fill
```
Save as: `Unity/Assets/Art/UI/vol_corner_tl.png`

Repeat for 3 remaining corners:
- `vol_corner_tr.png`, `vol_corner_bl.png`, `vol_corner_br.png`

---

**Wiring tiles into the game after saving:**
1. Open Chapter1Map / Chapter2Map / Chapter3Map in Unity Inspector
2. Assign sprites to Buildable Sprite, Path Sprite, and auto-tiling corner slots
3. Click **Save & Build Battle Scene**

---

## PRIORITY 4: Enemy Sprites (60 min)

Currently enemies are colored squares. Replacing with art makes a massive visual difference.

**Tool:** [Bing Image Creator](https://bing.com/images/create) or [Leonardo.ai](https://leonardo.ai)

**Format requirements:** PNG, transparent background, top-down 3/4 view, ~256×256

**Base suffix for ALL enemy prompts:**
```
top-down 3/4 view game enemy sprite, transparent background,
full body, stylized painterly, fantasy tower defense game art,
256x256, clean edges, no shadows on background
```

---

### Chapter 1 — Orc Enemies

**Orc Scout (basic):**
```
small green orc warrior with crude leather armor and short sword, running pose,
top-down 3/4 view, transparent background, full body, stylized painterly, fantasy
```
Save as: `Unity/Assets/Art/Enemies/OrcEnemy.png`

**Orc Runner (fast, runner trait):**
```
lean fast green orc with light leather gear, mid-sprint pose, bright lime green,
top-down 3/4 view, transparent background, full body, stylized painterly, fantasy
```
Save as: `Unity/Assets/Art/Enemies/OrcRunner.png`

**Orc Brute (slow, heavy armor):**
```
massive orc with heavy plate armor and giant war club, brown-orange skin,
wide intimidating stance, top-down 3/4 view, transparent background, stylized painterly
```
Save as: `Unity/Assets/Art/Enemies/OrcBrute.png`

**Shielded Orc (shielded trait):**
```
orc warrior with a large glowing magical shield and sword, cyan magical shield glow,
defensive stance, top-down 3/4 view, transparent background, stylized painterly
```
Save as: `Unity/Assets/Art/Enemies/OrcShielded.png`

**Regenerating Orc (regen trait):**
```
undead orc with glowing green regeneration aura, eerie healing energy swirling around,
top-down 3/4 view, transparent background, stylized painterly
```
Save as: `Unity/Assets/Art/Enemies/OrcRegenerator.png`

**Flying Orc (flying trait):**
```
orc with dark purple bat-like wings, airborne pose viewed from slight above,
top-down 3/4 view, transparent background, stylized painterly, fantasy
```
Save as: `Unity/Assets/Art/Enemies/OrcFlying.png`

---

### Chapter 2 — Ice Enemies

**Ice Shard (fast runner):**
```
small fast creature made of jagged blue ice crystals, running pose,
cold blue-white palette, top-down 3/4 view, transparent background,
stylized painterly, fantasy game art
```
Save as: `Unity/Assets/Art/Enemies/IceShard.png`

**Frost Brute (heavy, high armor):**
```
giant frost troll with thick ice armor and massive frozen fists, slow heavy stance,
icy blue-white, top-down 3/4 view, transparent background, stylized painterly
```
Save as: `Unity/Assets/Art/Enemies/FrostBrute.png`

**Glacial Shield (shielded with regen shield):**
```
ice golem with a large translucent ice shield, cyan magical glow,
defensive pose, top-down 3/4 view, transparent background, stylized painterly
```
Save as: `Unity/Assets/Art/Enemies/GlacialShield.png`

---

### Chapter 3 — Fire Enemies

**Lava Hound (fast runner):**
```
wolf-like creature made of molten rock and fire, running pose, glowing lava cracks,
orange-red glow, top-down 3/4 view, transparent background, stylized painterly
```
Save as: `Unity/Assets/Art/Enemies/LavaHound.png`

**Magma Golem (slow brute):**
```
massive slow golem of hardened lava with glowing orange cracks, enormous fists,
top-down 3/4 view, transparent background, stylized painterly, fantasy
```
Save as: `Unity/Assets/Art/Enemies/MagmaGolem.png`

**Ember Wraith (flying):**
```
ghostly fire spirit with wings made of flame, floating in air, ember particles,
orange-red spectral glow, top-down 3/4 view from above, transparent background,
stylized painterly
```
Save as: `Unity/Assets/Art/Enemies/EmberWraith.png`

---

## PRIORITY 5: Audio (60 min)

**Tool:** [Freesound.org](https://freesound.org) — free, Creative Commons licensed. Create a free account to download.

Search for these terms and download the best match (prefer WAV or OGG format):

| Sound | Search term | Save as |
|-------|------------|---------|
| Projectile attack | `whoosh projectile fantasy` | `Unity/Assets/Audio/sfx_projectile.wav` |
| Dragon attack | `dragon roar short` | `Unity/Assets/Audio/sfx_attack.wav` |
| Enemy death | `monster death short fantasy` | `Unity/Assets/Audio/sfx_enemy_death.wav` |
| Enemy reaches base | `explosion impact damage` | `Unity/Assets/Audio/sfx_enemy_base.wav` |
| Wave start | `battle horn medieval fanfare` | `Unity/Assets/Audio/sfx_wave_start.wav` |
| Victory | `fanfare victory short` | `Unity/Assets/Audio/sfx_victory.wav` |
| Defeat | `dark sting defeat game over` | `Unity/Assets/Audio/sfx_defeat.wav` |
| Button click | `click button UI game` | `Unity/Assets/Audio/sfx_button.wav` |
| Tower upgrade | `upgrade level up chime` | `Unity/Assets/Audio/sfx_upgrade.wav` |
| Ultimate skill | `thunder magic explosion impact` | `Unity/Assets/Audio/sfx_ultimate.wav` |
| Tower placement | `click place object game` | `Unity/Assets/Audio/sfx_place.wav` |

**Background music:**
- Go to [Incompetech.com](https://incompetech.com/music/) → Music → Filter by "Fantasy" or "Medieval"
- Free to use with attribution in credits
- Download 3 tracks:
  - Main menu theme → `Unity/Assets/Audio/music_menu.mp3`
  - Battle theme (loopable) → `Unity/Assets/Audio/music_battle.mp3`
  - Victory sting → `Unity/Assets/Audio/music_victory.mp3`

---

## Tool Summary

| Asset | Best free tool | Est. time |
|-------|---------------|-----------|
| Remove portrait white backgrounds | [remove.bg](https://remove.bg) | 5 min |
| New dragon portraits | [Bing Image Creator](https://bing.com/images/create) | 20 min |
| Map tiles (all chapters) | [Bing Image Creator](https://bing.com/images/create) | 60 min |
| Enemy sprites | [Leonardo.ai](https://leonardo.ai) or Bing | 45 min |
| SFX | [Freesound.org](https://freesound.org) | 30 min |
| Music | [Incompetech.com](https://incompetech.com) | 10 min |

---

## After All Assets Are Ready

Tell Claude Code and it will wire everything into the game in one pass:
- Sprites assigned to enemy prefabs via SceneBootstrapper
- Tiles assigned to MapDefinitions
- Audio wired into an AudioManager
- Portraits set on DragonDefinitions

All Unity work is already scaffolded — just drop the art files into the correct paths and rebuild.
