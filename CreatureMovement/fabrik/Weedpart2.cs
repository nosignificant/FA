
using UnityEngine;
using System.Collections;
using System.Collections.Generic;
public class Weedpart2 : MonoBehaviour
{
    public LayerMask ground;
    Vector3 root;
    Weed2 weed2;
    bool isRoot;
    void Start()
    {
        weed2 = GetComponentInParent<Weed2>();
        if (weed2 != null) root = weed2.root.transform.position;
    }

    public Vector3 SetTargetGround(Vector3 movingDir)
    {

        Vector3 rayOrigin = movingDir + (Vector3.up * 10.0f);

        Debug.DrawRay(rayOrigin, Vector3.down * 20f, Color.red, 1.0f);
        bool found = Physics.Raycast(rayOrigin, Vector3.down, out RaycastHit rest, 500, ground);

        if (found) return rest.point;
        else return transform.position;
    }

    public void rootFollowTarget(Transform rootTarget)
    {
        if (this == weed2.root) isRoot = true;

        if (isRoot)
        {
            Vector3 v = new Vector3(0, 5, 0);
            Vector3 rootTargetPos = rootTarget.position + v;
            transform.position = Vector3.Lerp(transform.position, rootTargetPos, Time.deltaTime);
        }
    }
}