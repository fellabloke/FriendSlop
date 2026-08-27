using Unity.Netcode;
using UnityEngine;

public class TrafficManager : NetworkBehaviour
{
    [Header("Spawning Settings")]
    public GameObject[] civilianCarPrefabs;
    public float spawnZPosition = 300f; 
    public float minSpawnDelay = 1.5f;
    public float maxSpawnDelay = 4.0f;

    [Header("Lanes")]
    public float[] laneXPositions = { -6.0f, 0f, -2.0f }; 

    private float nextSpawnTime;

    public override void OnNetworkSpawn()
    {
        if (!IsServer) return; 
        ScheduleNextSpawn();
    }

    void Update()
    {
        if (!IsServer) return;

        if (Time.time >= nextSpawnTime)
        {
            SpawnCar();
            ScheduleNextSpawn();
        }
    }

    private void ScheduleNextSpawn()
    {
        nextSpawnTime = Time.time + Random.Range(minSpawnDelay, maxSpawnDelay);
    }

    private void SpawnCar()
    {
        if (civilianCarPrefabs.Length == 0) return;

        GameObject prefabToSpawn = civilianCarPrefabs[Random.Range(0, civilianCarPrefabs.Length)];
        float chosenLaneX = laneXPositions[Random.Range(0, laneXPositions.Length)];

        Vector3 spawnPosition = new Vector3(chosenLaneX, 0.5f, spawnZPosition);
        GameObject carInstance = Instantiate(prefabToSpawn, spawnPosition, Quaternion.identity);

        NetworkObject netObj = carInstance.GetComponent<NetworkObject>();
        if (netObj != null)
        {
            netObj.Spawn();
        }
    }
}
