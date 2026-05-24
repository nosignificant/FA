using UnityEngine;

public class TransformCycler : MonoBehaviour
{
    [Header("Refs")]
    public Creature[] creatures;
    public Camera cam;
    public EncyclopediaUI ui;

    [Header("Settings")]
    public float speed = 5f;

    private int current = 0;

    private void Awake()
    {
        if (cam == null) cam = Camera.main;
    }

    private void Start()
    {
        UpdateUI();
        UpdateRotate();
    }

    private void Update()
    {
        if (creatures == null || creatures.Length == 0 || cam == null) return;

        if (Input.GetKeyDown(KeyCode.D)) Move(1);
        if (Input.GetKeyDown(KeyCode.A)) Move(-1);

        float targetX = creatures[current].rootTransform.position.x;
        Vector3 pos = cam.transform.position;
        pos.x = Mathf.Lerp(pos.x, targetX, speed * Time.deltaTime);
        cam.transform.position = pos;
    }

    private void Move(int dir)
    {
        current = (current + dir + creatures.Length) % creatures.Length;
        UpdateUI();
        UpdateRotate();
    }

    private void UpdateUI()
    {
        if (ui != null) ui.Show(creatures[current]);
    }

    private void UpdateRotate()
    {
        for (int i = 0; i < creatures.Length; i++)
        {
            if (creatures[i] == null) continue;
            var rotate = creatures[i].GetComponentInChildren<Rotate>();
            if (rotate == null) continue;
            rotate.isSelfRotate = (i == current);
        }
    }
}
