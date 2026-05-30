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

        private const string PrefSfxVolume  = "audio_sfx_volume";
        private const string PrefMusicVolume = "audio_music_volume";
        private const string PrefMuted       = "audio_muted";

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
            StartCoroutine(CrossfadeMusic(clip, key != MusicKey.Victory));
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
            const float duration = 0.5f;
            float start = _musicSource.volume;
            for (float t = 0f; t < duration; t += Time.unscaledDeltaTime)
            {
                _musicSource.volume = Mathf.Lerp(start, 0f, t / duration);
                yield return null;
            }
            _musicSource.Stop();
            _musicSource.loop = loop;
            if (clip != null) { _musicSource.clip = clip; _musicSource.Play(); }
            for (float t = 0f; t < duration; t += Time.unscaledDeltaTime)
            {
                _musicSource.volume = Mathf.Lerp(0f, IsMuted ? 0f : MusicVolume, t / duration);
                yield return null;
            }
            _musicSource.volume = IsMuted ? 0f : MusicVolume;
        }

        private IEnumerator FadeOutMusic()
        {
            const float duration = 0.5f;
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
                    ? Resources.Load<AudioClip>("Audio/" + name) : null;
                _sfxCache[key] = clip;
            }
            return clip;
        }

        private AudioClip GetMusic(MusicKey key)
        {
            if (!_musicCache.TryGetValue(key, out AudioClip clip))
            {
                clip = MusicFilenames.TryGetValue(key, out string name)
                    ? Resources.Load<AudioClip>("Audio/" + name) : null;
                _musicCache[key] = clip;
            }
            return clip;
        }
    }
}
