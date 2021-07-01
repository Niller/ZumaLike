using Leopotam.EcsLite;
using UnityEngine;

namespace Game
{
    public class GameManager : MonoBehaviour
    {
        private EcsWorld _world;
        private EcsSystems _systems;
        
        void Start () 
        {
            _world = new EcsWorld();
            _systems = new EcsSystems(_world);
            _systems.Init();
        }
        
        void Update () 
        {
            _systems?.Run ();
        }

        void OnDestroy () 
        {
            if (_systems != null) 
            {
                _systems.Destroy();
                _systems = null;
            }
            
            if (_world != null) 
            {
                _world.Destroy();
                _world = null;
            }
        }
    }
}