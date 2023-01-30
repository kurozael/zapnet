using UnityEngine;

public class DDOLS : MonoBehaviour
{
    private static DDOLS _instance;
    public static DDOLS Instance
    {
        get
        {
            return _instance;
        }
    }

    void Awake()
    {
        if (_instance)
        {
            Destroy(this);
        }

        gameObject.name = GetType().ToString();
        _instance = this;
        DontDestroyOnLoad(this);
    }

    public GameObject[] GetRootGameObjects()
    {
        return gameObject.scene.GetRootGameObjects();
    }
}
