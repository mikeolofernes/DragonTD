# Audio System Design

Date: 2026-05-30

## Architecture

Single `AudioManager` MonoBehaviour singleton (`DontDestroyOnLoad`). Two `AudioSource` components on the same GameObject: one for SFX (non-looping, `PlayOneShot`), one for music (looping, crossfade). Volume and mute stored in `PlayerPrefs`. Missing clips = silent — no NullRef, no log spam. Clips loaded from `Resources/Audio/` by filename key.

## Files

- Create: `Unity/Assets/Scripts/Core/AudioManager.cs`
- Create: `Unity/Assets/Resources/Audio/.gitkeep` (empty folder placeholder)

## SFX Keys and Triggers

| Enum | Filename | Trigger location |
|------|---------|-----------------|
| `Attack` | `sfx_attack` | `DragonTower.FireAt` |
| `EnemyDeath` | `sfx_enemy_death` | `EnemyBase.Die` |
| `EnemyReachBase` | `sfx_enemy_base` | `EnemyBase.ReachBase` |
| `WaveStart` | `sfx_wave_start` | `GameManager.SetState(Wave)` |
| `WaveEnd` | `sfx_wave_end` | `WaveManager.OnWaveComplete` event |
| `Upgrade` | `sfx_upgrade` | `DragonTower.TryUpgrade` success path |
| `SkillCast` | `sfx_skill` | `BattleStatsTracker.RecordSkillCast` |
| `Ultimate` | `sfx_ultimate` | `DragonTower.TryCastUltimate` success path |
| `TowerPlace` | `sfx_place` | `PlacementManager` place success |
| `TowerSell` | `sfx_sell` | `DragonTower.Sell` |
| `ButtonClick` | `sfx_button` | `MainMenuController` nav buttons |
| `Victory` | `sfx_victory` | `GameManager.SetState(Victory)` |
| `Defeat` | `sfx_defeat` | `GameManager.SetState(Defeat)` |

## Music Keys

| Enum | Filename | When | Loop |
|------|---------|------|------|
| `Menu` | `music_menu` | MainMenu scene | yes |
| `Battle` | `music_battle` | BattleScene | yes |
| `Victory` | `music_victory` | Victory panel shown | no |

Crossfade duration: 0.5s (fade out old, fade in new).

## API

```csharp
AudioManager.Instance.PlaySfx(SfxKey.Attack);
AudioManager.Instance.PlayMusic(MusicKey.Battle);
AudioManager.Instance.StopMusic();
AudioManager.Instance.SetSfxVolume(float 0-1);   // persists to PlayerPrefs
AudioManager.Instance.SetMusicVolume(float 0-1); // persists to PlayerPrefs
AudioManager.Instance.ToggleMute();               // mutes both channels
bool AudioManager.Instance.IsMuted { get; }
float AudioManager.Instance.SfxVolume { get; }
float AudioManager.Instance.MusicVolume { get; }
```

## PlayerPrefs Keys

- `audio_sfx_volume` (float, default 1.0)
- `audio_music_volume` (float, default 0.7)
- `audio_muted` (int 0/1, default 0)

## Missing Clip Behaviour

`Resources.Load<AudioClip>` returns null if file absent. `AudioManager` caches the result (including null). `PlaySfx(key)` with null clip = no-op. No error log. System fully functional with zero audio files.

## Out of Scope

- 3D spatial audio
- Addressables (use Resources for now)
- Per-clip volume control
- Audio mixer groups
