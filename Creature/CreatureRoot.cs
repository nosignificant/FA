using UnityEngine;
public class CreatureRoot : MonoBehaviour
{
    private static CreatureRoot _instance;

    public static Transform Container
    {
        get
        {
            if (_instance != null) return _instance.transform;

            var existing = FindObjectOfType<CreatureRoot>();
            if (existing != null) { _instance = existing; return _instance.transform; }

            var go = new GameObject("_Creatures");
            _instance = go.AddComponent<CreatureRoot>();
            return go.transform;
        }
    }
}
