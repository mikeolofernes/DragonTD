using UnityEngine;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine.AddressableAssets;

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

            var handle = Addressables.LoadAssetAsync<T>(key);
            var result = await handle.Task;
            if (result != null) _cache[key] = result;
            return result;
        }

        public void Release(string key)
        {
            if (_cache.TryGetValue(key, out var asset))
            {
                Addressables.Release(asset);
                _cache.Remove(key);
            }
        }

        // Convenience: returns portrait sprite — uses direct reference as fallback for prototype
        public async Task<Sprite> LoadDragonPortrait(DragonDefinition def)
        {
            if (!string.IsNullOrEmpty(def.visualData.portraitKey))
                return await LoadSprite(def.visualData.portraitKey);
            return def.visualData.portrait; // prototype fallback
        }
    }
}
