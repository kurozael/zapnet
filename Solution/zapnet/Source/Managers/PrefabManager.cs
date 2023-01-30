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
    /// A behaviour to manage all network prefabs.
    /// </summary>
    public class PrefabManager : MonoBehaviour
    {
        private Dictionary<string, NetworkPrefab> _nameToPrefab;
        private Dictionary<string, ushort> _nameToId;
        private Dictionary<ushort, NetworkPrefab> _idToPrefab;
        private ushort _nextFreeId = 1;

        /// <summary>
        /// [Shared] Get the network table. This is a dictionary of prefab names
        /// and their corresponding unique network identifiers that can be
        /// sent across the network.
        /// </summary>
        /// <returns></returns>
        public Dictionary<string, ushort> GetNetworkTable()
        {
            return _nameToId;
        }

        /// <summary>
        /// [Shared] Load a single network prefab and manually specify its unique network identifier.
        /// Use this with caution, and make sure your prefabs are loaded with the same
        /// unique network identifier for both your Server and Client. This must be called
		/// after you have started a Server or a Client.
        /// </summary>
        /// <param name="prefab"></param>
        /// <param name="id"></param>
        public void LoadWithId(NetworkPrefab prefab, ushort id)
        {
            var isServer = Zapnet.Network.IsServer;

            if (_idToPrefab.ContainsKey(id))
            {
                return;
            }

            _nameToPrefab[prefab.uniqueName] = prefab;

            if (isServer)
            {
                Debug.Log(prefab.uniqueName + " = " + id);
                _nameToId[prefab.uniqueName] = id;
                _idToPrefab[id] = prefab;

                if (_nextFreeId <= id)
                {
                    _nextFreeId = ++id;
                }
            }
        }

        /// <summary>
        /// [Shared] Load all network prefabs in the provided array. This must be called after you
		/// have started a Server or a Client.
        /// </summary>
        /// <param name="prefabs"></param>
        public void LoadAll(NetworkPrefab[] prefabs)
        {
            var isServer = Zapnet.Network.IsServer;

            for (var i = 0; i < prefabs.Length; i++)
            {
                var prefab = prefabs[i];

                _nameToPrefab[prefab.uniqueName] = prefab;

                if (isServer)
                {
                    Debug.Log(prefab.uniqueName + " = " + _nextFreeId);
                    _nameToId[prefab.uniqueName] = _nextFreeId;
                    _idToPrefab[_nextFreeId] = prefab;
                    _nextFreeId++;
                }
            }
        }

        /// <summary>
        /// [Shared] Load all network prefabs in the provided directory. The directory must
        /// live inside a Resources folder in your Unity project. This must be called
		/// after you have started a Server or a Client.
        /// </summary>
        /// <param name="directory"></param>
        public void LoadAll(string directory = "")
        {
            LoadAll(Resources.LoadAll<NetworkPrefab>(directory));
        }

        /// <summary>
        /// [Shared] Get the unique network identifier for the provided unique name.
        /// </summary>
        /// <param name="uniqueName"></param>
        /// <returns></returns>
        public ushort GetId(string uniqueName)
        {
            if (_nameToId.TryGetValue(uniqueName, out var id))
            {
                return id;
            }

            return 0;
        }

        /// <summary>
        /// [Shared] Find a prefab by its unique network identifier.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="id"></param>
        /// <returns></returns>
        public T Find<T>(ushort id) where T : MonoBehaviour
        {
            if (_idToPrefab.TryGetValue(id, out var prefab))
            {
                return prefab.GetComponent<T>();
            }
            else
            {
                return null;
            }
        }

        /// <summary>
        /// [Shared] Find a prefab by its unique network identifier.
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>
        public NetworkPrefab Find(ushort id)
        {
            return Find<NetworkPrefab>(id);
        }

        /// <summary>
        /// [Shared] Find a prefab by its unique name.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="uniqueName"></param>
        /// <returns></returns>
        public T Find<T>(string uniqueName) where T : MonoBehaviour
        {
            if (_nameToPrefab.TryGetValue(uniqueName, out var prefab))
            {
                return prefab.GetComponent<T>();
            }
            else
            {
                return null;
            }
        }

        /// <summary>
        /// [Shared] Find a prefab by its unique name.
        /// </summary>
        /// <param name="uniqueName"></param>
        /// <returns></returns>
        public NetworkPrefab Find(string uniqueName)
        {
            return Find<NetworkPrefab>(uniqueName);
        }

        internal void Add(string uniqueName, ushort id)
        {
            if (Zapnet.Network.IsClient)
            {
                var prefab = Find(uniqueName);
                _nameToId[uniqueName] = id;
                _idToPrefab[id] = prefab;
            }
        }

        protected virtual void Awake()
        {
            _idToPrefab = new Dictionary<ushort, NetworkPrefab>();
            _nameToPrefab = new Dictionary<string, NetworkPrefab>();
            _nameToId = new Dictionary<string, ushort>();
        }
    }
}
