using UnityEngine;
using System.Collections.Generic;
using System.Threading.Tasks;
#if UNITY_ADDRESSABLES
using UnityEngine.AddressableAssets;
#endif

namespace DragonTD.Dragons
{
    public class DragonAssetLoader : MonoBehaviour
    {
        public static DragonAssetLoader Instance { get; private set; }

        private readonly Dictionary<string, Object> _cache = new();

        private void Awake()
        {
            if (Instance != null) { Destroy(gameObject); return; }
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }

        public async Task<Sprite> LoadSprite(string key) => await Load<Sprite>(key);
        public async Task<GameObject> LoadPrefab(string key) => await Load<GameObject>(key);
        public async Task<AudioClip> LoadAudio(string key) => await Load<AudioClip>(key);

        private async Task<T> Load<T>(string key) where T : Object
        {
            if (string.IsNullOrEmpty(key)) return null;
            if (_cache.TryGetValue(key, out var cached)) return cached as T;

#if UNITY_ADDRESSABLES
            var handle = Addressables.LoadAssetAsync<T>(key);
            var result = await handle.Task;
            if (result != null) _cache[key] = result;
            return result;
#else
            await Task.CompletedTask;
            return null;
#endif
        }

        public void Release(string key)
        {
#if UNITY_ADDRESSABLES
            if (_cache.TryGetValue(key, out var asset))
            {
                Addressables.Release(asset);
                _cache.Remove(key);
            }
#endif
        }

        // Returns portrait sprite — uses direct reference as fallback when Addressables not configured
        public async Task<Sprite> LoadDragonPortrait(DragonDefinition def)
        {
#if UNITY_ADDRESSABLES
            if (!string.IsNullOrEmpty(def.visualData.portraitKey))
                return await LoadSprite(def.visualData.portraitKey);
#else
            await Task.CompletedTask;
#endif
            return def.visualData.portrait;
        }
    }
}
