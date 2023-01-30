#if UNITY_EDITOR
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.Callbacks;
using UnityEngine;
using UnityEngine.SceneManagement;

public static class StripNetworkObjects
{
    [PostProcessScene]
    public static void Process()
    {
        if (!BuildPipeline.isBuildingPlayer && !EditorApplication.isPlaying)
        {
            return;
        }
		
        var isServer = !EditorApplication.isPlaying && EditorUserBuildSettings.enableHeadlessMode;

        foreach (var target in FindObjectsOfTypeAll<ActivateOnAwake>(true))
        {
            target.Activate();
        }

        if (isServer)
        {
            var typesToRemove = new List<System.Type>
            {
                //typeof(Renderer)
            };

            typesToRemove.ForEach((type) =>
            {
                var components = Resources.FindObjectsOfTypeAll(type);

                foreach (var component in components)
                {
                    var behaviour = (component as Component);

                    if (behaviour)
                    {
                        var gameObject = behaviour.gameObject;

                        if (gameObject && gameObject.hideFlags == HideFlags.None && gameObject.scene.rootCount > 0)
                        {
                            try
                            {
                                Object.DestroyImmediate(component);
                            }
                            catch
                            {

                            }
                        }
                    }
                }
            });

            foreach (var target in FindObjectsOfTypeAll<ClientOnly>(true))
            {
                if (target != null)
                {
                    target.DeleteComponents();
                }
            }
        }
        else
        {
            foreach (var target in FindObjectsOfTypeAll<ServerOnly>(true))
            {
                target.DeleteComponents();
            }
        }
    }

    private static List<GameObject> GetAllSceneGameObjects(Scene scene)
    {
        var allGameObjects = new List<GameObject>();

        scene.GetRootGameObjects(allGameObjects);

        var ddols = DDOLS.Instance;

        if (ddols != null)
        {
            allGameObjects.AddRange(ddols.GetRootGameObjects());
        }

        return allGameObjects;
    }

    private static List<T> FindObjectsOfTypeAll<T>(bool findInActive = false) where T : Component
    {
        var results = new List<T>();

        for (var i = 0; i < SceneManager.sceneCount; i++)
        {
            var scene = SceneManager.GetSceneAt(i);

            if (scene != null && scene.isLoaded)
            {
                var allGameObjects = GetAllSceneGameObjects(scene);

                for (int j = 0; j < allGameObjects.Count; j++)
                {
                    var gameObject = allGameObjects[j];
                    results.AddRange(gameObject.GetComponentsInChildren<T>(findInActive));
                }
            }
        }

        return results;
    }
}
#endif
