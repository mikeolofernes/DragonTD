using UnityEngine;

namespace DragonTD.Core
{
    public class SceneBootstrap : MonoBehaviour
    {
        private void Start()
        {
            GameManager.Instance.StartBattle();
        }
    }
}
