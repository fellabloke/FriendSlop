using Unity.Netcode;
using UnityEngine;

public class HighwayManager : NetworkBehaviour  
{
    public static HighwayManager Instance;

    public NetworkVariable<float> currentSpeed = new NetworkVariable<float>(
        0f,
        NetworkVariableReadPermission.Everyone,
        NetworkVariableWritePermission.Server
    );
    public float minSpeed = 1f;
    public float maxSpeed = 40f;
    public float accelarationRate = 5f;

    [SerializeField] private GameObject roadChunkPrefab;
    [SerializeField] private int poolSize = 7;
    [SerializeField] private float chunkLength = 700f;

    private Transform[] activeChunks;

    void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);
    }

    public override void OnNetworkSpawn()
    {
        activeChunks = new Transform[poolSize];

        for(int i = 0; i < poolSize; i++)
        {
            GameObject chunk = Instantiate(roadChunkPrefab, transform);
            chunk.transform.position = new Vector3(0, 0, i * chunkLength);
            activeChunks[i] = chunk.transform;
        }
    }

    void Update()
    {
        if (!IsSpawned) return;

        float moveDistance = currentSpeed.Value * Time.deltaTime;

        for (int i = 0; i < poolSize; i++)
        {
            activeChunks[i].Translate(Vector3.back * moveDistance, Space.World);

            if (activeChunks[i].position.z < -chunkLength)
            {
                float furthestZ = -100f;
                foreach (Transform t in activeChunks)
                {
                    if (t.position.z > furthestZ) furthestZ = t.position.z;
                }
                activeChunks[i].position = new Vector3(0, 0, furthestZ + chunkLength);
            }
        }
    }

    [Rpc(SendTo.Server, InvokePermission = RpcInvokePermission.Everyone)]
    public void RequestSpeedChangeServerRPC(float gasBrakeInput)
    {
        float newSpeed = currentSpeed.Value + (gasBrakeInput * accelarationRate * Time.deltaTime);

        currentSpeed.Value = Mathf.Clamp(newSpeed, minSpeed, maxSpeed);
    }
}
