using UnityEngine;

public class BoidSpawner : MonoBehaviour
{
    public GameObject boidPrefab;
    public float spawnRadius = 100f;
    public int spawnCount = 100;

    public float initSpeed = 2f;

    void Start()
    {
        for (int i = 0; i < spawnCount; i++)
        {
            Vector3 randomPos = Random.insideUnitSphere * spawnRadius;
            GameObject newBoid = Instantiate(boidPrefab, randomPos, Random.rotation);
            newBoid.GetComponent<Boid>().velocity = newBoid.transform.forward * initSpeed;
        }
    }
}