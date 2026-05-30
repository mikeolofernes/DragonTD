# Audio System Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Build an `AudioManager` singleton with SFX + music playback, volume/mute persistence, and wire calls into all game events.

**Architecture:** Single `AudioManager` MonoBehaviour (`DontDestroyOnLoad`) with two `AudioSource` components (SFX + music), `Resources.Load` clip cache (null = silent), `PlayerPrefs` volume persistence. Callers use `AudioManager.Instance.PlaySfx(SfxKey.X)` — no null checks needed at call sites.

**Tech Stack:** Unity 6 C#, `AudioSource`, `Resources.Load<AudioClip>`, `PlayerPrefs`.

---

## File Structure

| Action | Path |
|--------|------|
| Create | `Unity/Assets/Scripts/Core/AudioManager.cs` |
| Create | `Unity/Assets/Resources/Audio/.gitkeep` |
| Modify | `Unity/Assets/Scripts/TowerDefense/Combat/DragonTower.cs` |
| Modify | `Unity/Assets/Scripts/TowerDefense/Enemies/EnemyBase.cs` |
| Modify | `Unity/Assets/Scripts/TowerDefense/Placement/PlacementManager.cs` |
| Modify | `Unity/Assets/Scripts/Core/GameManager.cs` |
| Modify | `Unity/Assets/Scripts/Core/BattleStatsTracker.cs` |
| Modify | `Unity/Assets/Scripts/UI/MainMenuController.cs` |

---

## Task 1: AudioManager Core

**Files:**
- Create: `Unity/Assets/Scripts/Core/AudioManager.cs`
- Create: `Unity/Assets/Resources/Audio/.gitkeep`

- [ ] **Step 1: Create Resources/Audio folder**

```bash
mkdir -p "D:/DragonTD/DragonTD/Unity/Assets/Resources/Audio"
echo "" > "D:/DragonTD/DragonTD/Unity/Assets/Resources/Audio/.gitkeep"
```

- [ ] **Step 2: Create AudioManager.cs**

Create `Unity/Assets/Scripts/Core/AudioManager.cs`:

```csharp
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace DragonTD.Core
{
    public enum SfxKey
    {
        Attack, EnemyDeath, EnemyReachBase,
        WaveStart, WaveEnd,
        Upgrade, SkillCast, Ultimate,
        TowerPlace, TowerSell,
        ButtonClick, Victory, Defeat
    }

    public enum MusicKey { Menu, Battle, Victory }

    public class AudioManager : MonoBehaviour
    {
        public static AudioManager Instance { get; private set; }

        private AudioSource _sfxSource;
        private AudioSource _musicSource;

        private readonly Dictionary<SfxKey, AudioClip> _sfxCache = new Dictionary<SfxKey, AudioClip>();
        private readonly Dictionary<MusicKey, AudioClip> _musicCache = new Dictionary<MusicKey, AudioClip>();

        private static readonly Dictionary<SfxKey, string> SfxFilenames = new Dictionary<SfxKey, string>
        {
            { SfxKey.Attack,         "sfx_attack"      },
            { SfxKey.EnemyDeath,     "sfx_enemy_death" },
            { SfxKey.EnemyReachBase, "sfx_enemy_base"  },
            { SfxKey.WaveStart,      "sfx_wave_start"  },
            { SfxKey.WaveEnd,        "sfx_wave_end"    },
            { SfxKey.Upgrade,        "sfx_upgrade"     },
            { SfxKey.SkillCast,      "sfx_skill"       },
            { SfxKey.Ultimate,       "sfx_ultimate"    },
            { SfxKey.TowerPlace,     "sfx_place"       },
            { SfxKey.TowerSell,      "sfx_sell"        },
            { SfxKey.ButtonClick,    "sfx_button"      },
            { SfxKey.Victory,        "sfx_victory"     },
            { SfxKey.Defeat,         "sfx_defeat"      },
        };

        private static readonly Dictionary<MusicKey, string> MusicFilenames = new Dictionary<MusicKey, string>
        {
            { MusicKey.Menu,    "music_menu"    },
            { MusicKey.Battle,  "music_battle"  },
            { MusicKey.Victory, "music_victory" },
        };

        private const string PrefSfxVolume   = "audio_sfx_volume";
        private const string PrefMusicVolume  = "audio_music_volume";
        private const string PrefMuted        = "audio_muted";

        public float SfxVolume   { get; private set; }
        public float MusicVolume { get; private set; }
        public bool  IsMuted     { get; private set; }

        private MusicKey? _currentMusic;

        private void Awake()
        {
            if (Instance != null && Instance != this) { Destroy(gameObject); return; }
            Instance = this;
            DontDestroyOnLoad(gameObject);

            _sfxSource   = gameObject.AddComponent<AudioSource>();
            _musicSource = gameObject.AddComponent<AudioSource>();
            _musicSource.loop = true;

            SfxVolume   = PlayerPrefs.GetFloat(PrefSfxVolume,  1.0f);
            MusicVolume = PlayerPrefs.GetFloat(PrefMusicVolume, 0.7f);
            IsMuted     = PlayerPrefs.GetInt(PrefMuted, 0) == 1;

            ApplyVolumes();
        }

        public void PlaySfx(SfxKey key)
        {
            if (IsMuted) return;
            AudioClip clip = GetSfx(key);
            if (clip == null) return;
            _sfxSource.PlayOneShot(clip, SfxVolume);
        }

        public void PlayMusic(MusicKey key)
        {
            if (_currentMusic == key) return;
            AudioClip clip = GetMusic(key);
            _currentMusic = key;
            StopAllCoroutines();
            StartCoroutine(CrossfadeMusic(clip, key == MusicKey.Victory ? false : true));
        }

        public void StopMusic()
        {
            _currentMusic = null;
            StopAllCoroutines();
            StartCoroutine(FadeOutMusic());
        }

        public void SetSfxVolume(float volume)
        {
            SfxVolume = Mathf.Clamp01(volume);
            PlayerPrefs.SetFloat(PrefSfxVolume, SfxVolume);
            ApplyVolumes();
        }

        public void SetMusicVolume(float volume)
        {
            MusicVolume = Mathf.Clamp01(volume);
            PlayerPrefs.SetFloat(PrefMusicVolume, MusicVolume);
            ApplyVolumes();
        }

        public void ToggleMute()
        {
            IsMuted = !IsMuted;
            PlayerPrefs.SetInt(PrefMuted, IsMuted ? 1 : 0);
            ApplyVolumes();
        }

        private void ApplyVolumes()
        {
            _sfxSource.volume   = IsMuted ? 0f : SfxVolume;
            _musicSource.volume = IsMuted ? 0f : MusicVolume;
        }

        private IEnumerator CrossfadeMusic(AudioClip clip, bool loop)
        {
            float duration = 0.5f;
            float start = _musicSource.volume;
            for (float t = 0f; t < duration; t += Time.unscaledDeltaTime)
            {
                _musicSource.volume = Mathf.Lerp(start, 0f, t / duration);
                yield return null;
            }
            _musicSource.Stop();
            _musicSource.loop = loop;
            if (clip != null)
            {
                _musicSource.clip = clip;
                _musicSource.Play();
            }
            for (float t = 0f; t < duration; t += Time.unscaledDeltaTime)
            {
                _musicSource.volume = Mathf.Lerp(0f, IsMuted ? 0f : MusicVolume, t / duration);
                yield return null;
            }
            _musicSource.volume = IsMuted ? 0f : MusicVolume;
        }

        private IEnumerator FadeOutMusic()
        {
            float duration = 0.5f;
            float start = _musicSource.volume;
            for (float t = 0f; t < duration; t += Time.unscaledDeltaTime)
            {
                _musicSource.volume = Mathf.Lerp(start, 0f, t / duration);
                yield return null;
            }
            _musicSource.Stop();
        }

        private AudioClip GetSfx(SfxKey key)
        {
            if (!_sfxCache.TryGetValue(key, out AudioClip clip))
            {
                clip = SfxFilenames.TryGetValue(key, out string name)
                    ? Resources.Load<AudioClip>("Audio/" + name)
                    : null;
                _sfxCache[key] = clip; // cache null so we don't try again
            }
            return clip;
        }

        private AudioClip GetMusic(MusicKey key)
        {
            if (!_musicCache.TryGetValue(key, out AudioClip clip))
            {
                clip = MusicFilenames.TryGetValue(key, out string name)
                    ? Resources.Load<AudioClip>("Audio/" + name)
                    : null;
                _musicCache[key] = clip;
            }
            return clip;
        }
    }
}
```

- [ ] **Step 3: Add AudioManager to SceneBootstrapper**

In `Unity/Assets/Editor/SceneBootstrapper.cs`, inside `CreateManagerRoot`, find where `Root<GameManager>` etc. are called and add:

```csharp
            Root<AudioManager>("AudioManager");
```

Add it alongside the other singleton manager roots. This ensures the AudioManager is in every generated scene.

- [ ] **Step 4: Commit**

```bash
git -C "D:/DragonTD/DragonTD" add Unity/Assets/Scripts/Core/AudioManager.cs Unity/Assets/Resources/Audio/.gitkeep Unity/Assets/Editor/SceneBootstrapper.cs
git -C "D:/DragonTD/DragonTD" commit -m "feat: AudioManager singleton with SFX/music playback, volume/mute, PlayerPrefs persistence"
```

---

## Task 2: Wire SFX — DragonTower

**Files:**
- Modify: `Unity/Assets/Scripts/TowerDefense/Combat/DragonTower.cs`

- [ ] **Step 1: Add SFX in FireAt**

In `DragonTower.FireAt`, after the line `_lastAttackTime = Time.time;` (in `Update`) — actually, add to `FireAt` at the very end, after `go.GetComponent<ProjectileBase>()?.Initialize(...)`:

```csharp
            AudioManager.Instance?.PlaySfx(SfxKey.Attack);
```

- [ ] **Step 2: Add SFX in TryUpgrade success path**

In `TryUpgrade`, after `DamageIndicator.SpawnText(...)`, add:

```csharp
            AudioManager.Instance?.PlaySfx(SfxKey.Upgrade);
```

- [ ] **Step 3: Add SFX in Sell**

In `Sell`, after `ResourceManager.Instance?.AddMana(_manaRefund);`, add:

```csharp
            AudioManager.Instance?.PlaySfx(SfxKey.TowerSell);
```

- [ ] **Step 4: Add SFX in TryCastUltimate**

In `TryCastUltimate`, after `_lastUltimateCastTime = Time.time;`, add:

```csharp
            AudioManager.Instance?.PlaySfx(SfxKey.Ultimate);
```

- [ ] **Step 5: Commit**

```bash
git -C "D:/DragonTD/DragonTD" add Unity/Assets/Scripts/TowerDefense/Combat/DragonTower.cs
git -C "D:/DragonTD/DragonTD" commit -m "feat: wire attack/upgrade/sell/ultimate SFX into DragonTower"
```

---

## Task 3: Wire SFX — EnemyBase

**Files:**
- Modify: `Unity/Assets/Scripts/TowerDefense/Enemies/EnemyBase.cs`

- [ ] **Step 1: Add SFX in Die**

In `EnemyBase.Die()`, after `if (IsDead) return; IsDead = true;`, add:

```csharp
            if (!_reachedBase) AudioManager.Instance?.PlaySfx(SfxKey.EnemyDeath);
```

(Guard with `!_reachedBase` — enemies that reach the base play a different sound below.)

- [ ] **Step 2: Add SFX in ReachBase**

In `EnemyBase.ReachBase()`, after `_reachedBase = true;`, add:

```csharp
            AudioManager.Instance?.PlaySfx(SfxKey.EnemyReachBase);
```

- [ ] **Step 3: Commit**

```bash
git -C "D:/DragonTD/DragonTD" add Unity/Assets/Scripts/TowerDefense/Enemies/EnemyBase.cs
git -C "D:/DragonTD/DragonTD" commit -m "feat: wire enemy death / reach-base SFX into EnemyBase"
```

---

## Task 4: Wire SFX — PlacementManager

**Files:**
- Modify: `Unity/Assets/Scripts/TowerDefense/Placement/PlacementManager.cs`

- [ ] **Step 1: Add SFX in PlaceDragon**

In `PlaceDragon(GridTile tile)`, after `BattleStatsTracker.Instance?.RecordTowerPlaced(...)`, add:

```csharp
            AudioManager.Instance?.PlaySfx(SfxKey.TowerPlace);
```

- [ ] **Step 2: Commit**

```bash
git -C "D:/DragonTD/DragonTD" add Unity/Assets/Scripts/TowerDefense/Placement/PlacementManager.cs
git -C "D:/DragonTD/DragonTD" commit -m "feat: wire tower-place SFX into PlacementManager"
```

---

## Task 5: Wire SFX + Music — GameManager

**Files:**
- Modify: `Unity/Assets/Scripts/Core/GameManager.cs`

- [ ] **Step 1: Wire WaveStart SFX and Battle music**

In `GameManager.SetState(GameState newState)`, after `OnStateChanged?.Invoke(State);`, add:

```csharp
            if (newState == GameState.Wave)
            {
                AudioManager.Instance?.PlaySfx(SfxKey.WaveStart);
                if (AudioManager.Instance?._currentMusicKey != (int)MusicKey.Battle)
                    AudioManager.Instance?.PlayMusic(MusicKey.Battle);
            }
            else if (newState == GameState.Victory)
            {
                AudioManager.Instance?.PlaySfx(SfxKey.Victory);
                AudioManager.Instance?.PlayMusic(MusicKey.Victory);
            }
            else if (newState == GameState.Defeat)
            {
                AudioManager.Instance?.PlaySfx(SfxKey.Defeat);
                AudioManager.Instance?.StopMusic();
            }
```

**Note:** `_currentMusicKey` is private on AudioManager. To avoid the double-play guard being fragile, simplify: just call `PlayMusic(MusicKey.Battle)` on every Wave state — `PlayMusic` already checks `_currentMusic == key` and no-ops if same. Remove the `if` check:

```csharp
            if (newState == GameState.Wave)
            {
                AudioManager.Instance?.PlaySfx(SfxKey.WaveStart);
                AudioManager.Instance?.PlayMusic(MusicKey.Battle);
            }
            else if (newState == GameState.Victory)
            {
                AudioManager.Instance?.PlaySfx(SfxKey.Victory);
                AudioManager.Instance?.PlayMusic(MusicKey.Victory);
            }
            else if (newState == GameState.Defeat)
            {
                AudioManager.Instance?.PlaySfx(SfxKey.Defeat);
                AudioManager.Instance?.StopMusic();
            }
```

- [ ] **Step 2: Commit**

```bash
git -C "D:/DragonTD/DragonTD" add Unity/Assets/Scripts/Core/GameManager.cs
git -C "D:/DragonTD/DragonTD" commit -m "feat: wire wave-start SFX and battle/victory/defeat music into GameManager"
```

---

## Task 6: Wire SFX — BattleStatsTracker + WaveManager

**Files:**
- Modify: `Unity/Assets/Scripts/Core/BattleStatsTracker.cs`
- Modify: `Unity/Assets/Scripts/TowerDefense/Waves/WaveManager.cs`

- [ ] **Step 1: Wire SkillCast SFX in BattleStatsTracker**

Read `BattleStatsTracker.cs`. Find `RecordSkillCast()`. After `BattleSkillsCast++;`, add:

```csharp
            AudioManager.Instance?.PlaySfx(SfxKey.SkillCast);
```

- [ ] **Step 2: Wire WaveEnd SFX in WaveManager**

In `WaveManager`, find where `OnWaveComplete?.Invoke()` is called (the wave completion event). After that line, add:

```csharp
            AudioManager.Instance?.PlaySfx(SfxKey.WaveEnd);
```

- [ ] **Step 3: Commit**

```bash
git -C "D:/DragonTD/DragonTD" add Unity/Assets/Scripts/Core/BattleStatsTracker.cs Unity/Assets/Scripts/TowerDefense/Waves/WaveManager.cs
git -C "D:/DragonTD/DragonTD" commit -m "feat: wire skill-cast SFX and wave-end SFX"
```

---

## Task 7: Wire SFX + Music — MainMenuController

**Files:**
- Modify: `Unity/Assets/Scripts/UI/MainMenuController.cs`

- [ ] **Step 1: Start menu music**

In `MainMenuController`, find `Start()` or `Awake()` or `OnEnable()` (whichever initializes the menu). Add at the end:

```csharp
            AudioManager.Instance?.PlayMusic(MusicKey.Menu);
```

- [ ] **Step 2: Wire button-click SFX**

Find where nav buttons are wired up in `MainMenuController` (look for `onClick.AddListener` calls). Add to each button's listener:

```csharp
AudioManager.Instance?.PlaySfx(SfxKey.ButtonClick);
```

Do this for the main nav buttons only (Battle, Dragons, Profile, Clan, Events, Store). Do NOT add it to every minor button — just the primary navigation ones that already have listeners.

**Pattern:** Find each `_someButton.onClick.AddListener(() => { ... });` and prepend the PlaySfx call inside the lambda:

```csharp
_battleButton.onClick.AddListener(() =>
{
    AudioManager.Instance?.PlaySfx(SfxKey.ButtonClick);
    // ... existing code
});
```

Read the file first to see the actual button field names.

- [ ] **Step 3: Commit**

```bash
git -C "D:/DragonTD/DragonTD" add Unity/Assets/Scripts/UI/MainMenuController.cs
git -C "D:/DragonTD/DragonTD" commit -m "feat: wire menu music and button-click SFX into MainMenuController"
```

---

## Task 8: Update Handoff Doc

**Files:**
- Modify: `docs/2026-05-27-dragon-dominion-prototype-handoff.md`

- [ ] **Step 1: Append audio system section**

Add before `## Main Files To Read First`:

```markdown
## Audio System — 2026-05-30

- `AudioManager` DontDestroyOnLoad singleton in `Unity/Assets/Scripts/Core/AudioManager.cs`.
- Two `AudioSource` components: one for SFX (`PlayOneShot`), one for music (looping, crossfade 0.5s).
- Clips loaded from `Resources/Audio/` by filename. Missing clip = silent (no crash, no log).
- Volume + mute persisted in `PlayerPrefs` (`audio_sfx_volume`, `audio_music_volume`, `audio_muted`).
- SFX wired: attack, enemy death, enemy-reach-base, wave start/end, upgrade, skill cast, ultimate, place, sell, button click, victory, defeat.
- Music: menu (`music_menu`), battle (`music_battle`), victory sting (`music_victory`).
- Place audio files at `Unity/Assets/Resources/Audio/` with the filename keys above. See `docs/art-generation-guide.md` for generation instructions.
```

- [ ] **Step 2: Commit**

```bash
git -C "D:/DragonTD/DragonTD" add docs/2026-05-27-dragon-dominion-prototype-handoff.md
git -C "D:/DragonTD/DragonTD" commit -m "docs: audio system handoff entry"
git -C "D:/DragonTD/DragonTD" fetch . main:claude/dragon-tower-defense-rpg-okxJw
```

---

## Self-Review

**Spec coverage:**
- AudioManager singleton ✓ Task 1
- SfxKey + MusicKey enums ✓ Task 1
- Two AudioSources ✓ Task 1
- Resources.Load clip cache (null = silent) ✓ Task 1
- PlaySfx / PlayMusic / StopMusic / SetSfxVolume / SetMusicVolume / ToggleMute ✓ Task 1
- PlayerPrefs persistence ✓ Task 1
- Crossfade 0.5s ✓ Task 1
- All 13 SFX trigger wires ✓ Tasks 2-7
- Music start/stop wires ✓ Tasks 5 + 7
- Resources/Audio folder ✓ Task 1
- No placeholders ✓

**Type consistency:**
- `SfxKey` defined Task 1, used Tasks 2-7. ✓
- `MusicKey` defined Task 1, used Tasks 5+7. ✓
- `AudioManager.Instance?.PlaySfx(SfxKey.X)` pattern consistent across all callers. ✓
- `AudioManager.Instance?.PlayMusic(MusicKey.X)` consistent. ✓
