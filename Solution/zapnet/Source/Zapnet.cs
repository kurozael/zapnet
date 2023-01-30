/* 
 * Copyright © Conna Wiles <https://kurozael.com>
 * All Rights Reserved
 * 
 * kurozael <mailto:kurozael@gmail.com>
 */

using System.Collections.Generic;
using UnityEngine;

namespace zapnet
{
    /// <summary>
    /// The global Zapnet behaviour that provides access to all Zapnet managers.
    /// </summary>
    public class Zapnet : MonoBehaviour
    {
        public delegate void OnClientStarted();
        public delegate void OnServerStarted();

        /// <summary>
        /// Invoked when the network client has started.
        /// </summary>
        public static event OnClientStarted onClientStarted;

        /// <summary>
        /// Invoked when the network server has started.
        /// </summary>
        public static event OnServerStarted onServerStarted;

        private static List<GameObject> _gameObjects;

        private static NetworkManager _network;
        private static PlayerManager _player;
        private static EntityManager _entity;
        private static PrefabManager _prefab;
        private static Zapnet _instance;

        internal static bool IsShuttingDown { set; get; }

        /// <summary>
        /// Provides global access to the Zapnet network manager.
        /// </summary>
        public static NetworkManager Network
        {
            get
            {
                if (!_network)
                {
                    _network = CreateManager<NetworkManager>();
                    _gameObjects.Add(_network.gameObject);
                }

                return _network;
            }
        }

        /// <summary>
        /// Provides global access to the Zapnet prefab manager.
        /// </summary>
        public static PrefabManager Prefab
        {
            get
            {
                if (!_prefab)
                {
                    _prefab = CreateManager<PrefabManager>();
                    _gameObjects.Add(_prefab.gameObject);
                }

                return _prefab;
            }
        }

        /// <summary>
        /// Provides global access to the Zapnet player manager.
        /// </summary>
        public static PlayerManager Player
        {
            get
            {
                if (!_player)
                {
                    _player = CreateManager<PlayerManager>();
                    _gameObjects.Add(_player.gameObject);
                }

                return _player;
            }
        }

        /// <summary>
        /// Provides global access to the Zapnet entity manager.
        /// </summary>
        public static EntityManager Entity
        {
            get
            {
                if (!_entity)
                {
                    _entity = CreateManager<EntityManager>();
                    _gameObjects.Add(_entity.gameObject);
                }

                return _entity;
            }
        }

        internal static void InvokeClientStarted()
        {
            onClientStarted?.Invoke();
        }

        internal static void InvokeServerStarted()
        {
            onServerStarted?.Invoke();
        }

        /// <summary>
        /// Initialize Zapnet and the network manager.
        /// </summary>
        public static void Initialize()
        {
            if (!_instance)
            {
                _instance = CreateManager<Zapnet>();
                _gameObjects = new List<GameObject>();
                _gameObjects.Add(_instance.gameObject);

                Network.Initialize();
            }
        }

        /// <summary>
        /// Shuts down Zapnet and cleans up its managers.
        /// </summary>
        public static void Shutdown()
        {
            if (_instance)
            {
                for (var i = 0; i < _gameObjects.Count; i++)
                {
                    Destroy(_gameObjects[i]);
                }

                _gameObjects.Clear();
                _instance = null;
                _network = null;
                _prefab = null;
                _player = null;
                _entity = null;
            }
        }

        private static T CreateManager<T>() where T : Component
        {
            if (IsShuttingDown)
            {
                return null;
            }

            var manager = Object.FindObjectOfType<T>();

            if (manager != null)
            {
                DontDestroyOnLoad(manager.gameObject);
                return manager;
            }

            manager = new GameObject(typeof(T).Name).AddComponent<T>();
            DontDestroyOnLoad(manager.gameObject);

            return manager;
        }

        private void OnApplicationQuit()
        {
            IsShuttingDown = true;
        }

        private void OnDestroy()
        {
            IsShuttingDown = true;
        }

        static Zapnet()
        {
            _gameObjects = new List<GameObject>();
        }
    }
}