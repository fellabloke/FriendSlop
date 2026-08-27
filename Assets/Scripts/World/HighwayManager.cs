using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

// Define the available lane states
public enum RoadWidthType
{
    TwoLane,
    ThreeLane,
    FourLane,
    TollBooth 
}

[System.Serializable]
public struct RoadSegmentData
{
    public string segmentName;
    public GameObject prefab;
    [Range(0f, 100f)]
    public float weight; 
    
    [Header("Connection Rules")]
    public RoadWidthType entryWidth;
    public RoadWidthType exitWidth;
}

public class HighwayManager : NetworkBehaviour  
{
    public static HighwayManager Instance;

    [Header("Speed Settings")]
    public NetworkVariable<float> currentSpeed = new NetworkVariable<float>(
        0f,
        NetworkVariableReadPermission.Everyone,
        NetworkVariableWritePermission.Server
    );

    [Header("Generation Settings")]
    public List<RoadSegmentData> availableSegments;
    [SerializeField] private int poolSize = 7;
    [SerializeField] private float chunkLength = 20f;

    private Queue<Transform> activeChunks = new Queue<Transform>();
    private RoadWidthType currentExpectedWidth = RoadWidthType.TwoLane;
    
    //Variables handled in VanController minda
    private float idleDecelerationRate = 8f;
    private float lastAccelerationTime;
    private float minSpeed = 0f;
    private float maxSpeed = 40f;

    VanController vanController;

    void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);
    }
    private void OnEnable()
    {
        VanController.OnVanSpawned += GrabVanStats;
    }

    private void OnDisable()
    {
        VanController.OnVanSpawned -= GrabVanStats;
    }

    private void GrabVanStats(VanController spawnedVan)
    {
        if (!IsServer) return; 

        vanController = spawnedVan;
        minSpeed = vanController.minSpeed;
        maxSpeed = vanController.maxSpeed;
        idleDecelerationRate = vanController.idleDecelerationRate;
    }
    public override void OnNetworkSpawn()
    {
        if (!IsServer) return;

        for(int i = 0; i < poolSize; i++)
        {
            RoadSegmentData segmentData = (i < 3) ? GetSafeStartingSegment() : GetWeightedRandomSegment(currentExpectedWidth);
            SpawnChunk(segmentData, i * chunkLength);
        }
    }

    void Update()
    {
        if (!IsSpawned || !IsServer) return;

        if (Time.time - lastAccelerationTime > 0.5f)
        {
            if (currentSpeed.Value > minSpeed)
            {
                currentSpeed.Value = Mathf.MoveTowards(currentSpeed.Value, minSpeed, idleDecelerationRate * Time.deltaTime);
            }
        }

        float moveDistance = currentSpeed.Value * Time.deltaTime;

        foreach (Transform chunk in activeChunks)
        {
            chunk.Translate(Vector3.back * moveDistance, Space.World);
        }

        if (activeChunks.Count > 0)
        {
            Transform oldestChunk = activeChunks.Peek();

            if (oldestChunk.position.z < -chunkLength)
            {
                RecycleOldestChunk();
            }
        }
    }

    private RoadSegmentData GetSafeStartingSegment()
    {
        foreach (var segment in availableSegments)
        {
            if (segment.entryWidth == RoadWidthType.TwoLane && segment.exitWidth == RoadWidthType.TwoLane)
            {
                return segment;
            }
        }
        
        return availableSegments[0]; 
    }

    private void SpawnChunk(RoadSegmentData data, float spawnZPosition)
    {
        GameObject chunk = Instantiate(data.prefab, new Vector3(0, 0, spawnZPosition), Quaternion.Euler(0, 90, 0));
        
        NetworkObject netObj = chunk.GetComponent<NetworkObject>();
        if (netObj != null)
        {
            netObj.Spawn();
        }
        activeChunks.Enqueue(chunk.transform);

        currentExpectedWidth = data.exitWidth;
    }

    private void RecycleOldestChunk()
    {
        Transform oldChunk = activeChunks.Dequeue();
        NetworkObject oldNetObj = oldChunk.GetComponent<NetworkObject>();
        
        if (oldNetObj != null)
            oldNetObj.Despawn(true);
        else
            Destroy(oldChunk.gameObject);

        float furthestZ = -100f;
        foreach (Transform t in activeChunks)
        {
            if (t.position.z > furthestZ) furthestZ = t.position.z;
        }

        RoadSegmentData nextSegment = GetWeightedRandomSegment(currentExpectedWidth);
        
        SpawnChunk(nextSegment, furthestZ + chunkLength);
    }

    private RoadSegmentData GetWeightedRandomSegment(RoadWidthType requiredEntryWidth)
    {
        List<RoadSegmentData> validSegments = new List<RoadSegmentData>();
        float totalWeight = 0f;

        foreach (var segment in availableSegments)
        {
            if (segment.entryWidth == requiredEntryWidth)
            {
                validSegments.Add(segment);
                totalWeight += segment.weight;
            }
        }

        if (validSegments.Count == 0)
        {
            currentExpectedWidth = RoadWidthType.TwoLane; 
            return GetSafeStartingSegment();
        }

        float randomVal = Random.Range(0, totalWeight);
        float currentWeight = 0f;

        foreach (var segment in validSegments)
        {
            currentWeight += segment.weight;
            if (randomVal <= currentWeight)
            {
                return segment;
            }
        }

        return validSegments[0]; 
    }

    [Rpc(SendTo.Server, InvokePermission = RpcInvokePermission.Everyone)]
    public void RequestSpeedChangeServerRPC(float gasBrakeInput)
    {
        lastAccelerationTime = Time.time;
        float newSpeed = currentSpeed.Value + (gasBrakeInput * Time.deltaTime);
        currentSpeed.Value = Mathf.Clamp(newSpeed, minSpeed, maxSpeed);
    }
}