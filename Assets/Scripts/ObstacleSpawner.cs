using System.Collections.Generic;
using System.Linq;
using Unity.Netcode;
using UnityEngine;

public class ObstacleSpawner : NetworkBehaviour
{
    public GameObject[] obstacles;
    private List<Transform> spawnedObstacles = new List<Transform>();
    public int spawnAmount;
    public Transform spawnLocation;

    [Header("Speed Settings")]
    public NetworkVariable<float> currentSpeed = new NetworkVariable<float>(
        10f,
        NetworkVariableReadPermission.Everyone,
        NetworkVariableWritePermission.Server
    );

    public override void OnNetworkSpawn()
    {
        for (int i = 0; i < spawnAmount; i++)
        {
            GameObject spawnedObstacle = Instantiate(obstacles[0], new Vector3(spawnLocation.position.x,spawnLocation.position.y, spawnLocation.position.z + i * 50), Quaternion.identity);
            spawnedObstacles.Add(spawnedObstacle.transform);
        }
    }
    
    void Update()
    {
        if (!IsServer) return;

        float moveDistance = currentSpeed.Value * Time.deltaTime;
        foreach (Transform obstacle in spawnedObstacles)
        {
            obstacle.Translate(Vector3.back * moveDistance, Space.World);
        }
    }
}
