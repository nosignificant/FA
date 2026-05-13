using UnityEngine;

public class MouseTarget : MonoBehaviour
{
    public Camera mainCamera;
    public float heightOffset = 2f;

    void Update()
    {
        Vector3 mousePos = Input.mousePosition;

        Ray ray = mainCamera.ScreenPointToRay(mousePos);
        Plane groundPlane = new Plane(Vector3.up, Vector3.zero);

        if (groundPlane.Raycast(ray, out float enter))
        {
            Vector3 worldPos = ray.GetPoint(enter);

            worldPos.y += heightOffset;

            transform.position = worldPos;
        }
    }
}