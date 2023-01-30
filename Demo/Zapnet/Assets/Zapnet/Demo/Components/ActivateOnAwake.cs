using UnityEngine;

public class ActivateOnAwake : MonoBehaviour
{
    public MonoBehaviour[] components;
    public GameObject[] gameObjects;

    public void Activate()
    {
        for (var i = 0; i < gameObjects.Length; i++)
        {
            gameObjects[i].SetActive(true);
        }

        for (var i = 0; i < components.Length; i++)
        {
            components[i].enabled = true;
        }
    }

    private void Awake()
    {
        Activate();
    }
}
