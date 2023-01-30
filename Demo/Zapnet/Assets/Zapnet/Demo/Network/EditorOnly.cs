using UnityEngine;
using zapnet;

public class EditorOnly : MonoBehaviour
{
    public bool deleteGameObject = true;
    public bool onlyDeactivate = false;
    public Component[] components;

    private bool _hasDeleted;

    public void DeleteComponents()
    {
        if (_hasDeleted)
        {
            return;
        }

        for (var i = 0; i < components.Length; i++)
        {
            if (onlyDeactivate)
            {
                if (components[i] is MonoBehaviour)
                {
                    (components[i] as MonoBehaviour).enabled = false;
                }
            }
            else
            {
                if (components[i] is Transform)
                {
                    DestroyImmediate(components[i].gameObject);
                }
                else
                {
                    DestroyImmediate(components[i]);
                }
            }
        }

        if (deleteGameObject && gameObject != null)
        {
            if (onlyDeactivate)
            {
                gameObject.SetActive(false);
            }
            else
            {
                DestroyImmediate(gameObject);
            }
        }

        _hasDeleted = true;

        DestroyImmediate(this);
    }

    private void Awake()
    {
        DeleteComponents();
    }
}
