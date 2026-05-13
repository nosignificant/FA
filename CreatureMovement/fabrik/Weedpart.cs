
using UnityEngine;
using System.Collections;
using System.Collections.Generic;
public class Weedpart : MonoBehaviour
{
    float moveDuration = 3f;


    public void FollowTarget(Vector3 targetPos)
    {
        transform.position = Vector3.Lerp(transform.position, targetPos, Time.deltaTime);

        transform.LookAt(targetPos);
    }

    public IEnumerator Rotate(Transform root, float radius)
    {

        float time = 0f;

        while (true)
        {
            time += Time.deltaTime / moveDuration;

            float angle = time * 2 * Mathf.PI;

            float x = Mathf.Sin(angle) * radius;
            float z = Mathf.Cos(angle) * radius;

            Vector3 orbitPos = new Vector3(x, 0, z);

            transform.position = root.position + orbitPos;

            yield return null;
        }
    }
}