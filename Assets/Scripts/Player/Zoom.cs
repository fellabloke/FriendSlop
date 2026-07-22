using UnityEngine;
using Unity.Netcode;
using UnityEngine.InputSystem;

[RequireComponent(typeof(Camera))]
public class Zoom : NetworkBehaviour
{
    private Camera cam;

    [Header("Input Sockets")]
    [SerializeField] private InputActionReference zoomAction;

    [Header("Zoom FOV Settings")]
    [SerializeField] private float defaultFOV = 60f;
    [SerializeField] private float maxZoomFOV = 50f;
    
    [SerializeField] private float zoomSpeed = 10f;

    private float currentZoomProgress = 0f; 

    void Awake()
    {
        cam = GetComponent<Camera>();
        if (cam != null)
        {
            defaultFOV = cam.fieldOfView;
        }
    }

    public override void OnNetworkSpawn()
    {
        if (IsOwner && zoomAction != null)
        {
            zoomAction.action.Enable();
        }
    }

    public override void OnNetworkDespawn()
    {
        if (IsOwner && zoomAction != null)
        {
            zoomAction.action.Disable();
        }
    }

    void Update()
    {
        if (!IsOwner || cam == null || zoomAction == null) return;

        HandleZoom();
    }

    private void HandleZoom()
    {
        bool isHoldingZoom = zoomAction.action.IsPressed();

        float targetProgress = isHoldingZoom ? 1f : 0f;

        currentZoomProgress = Mathf.MoveTowards(currentZoomProgress, targetProgress, zoomSpeed * Time.deltaTime);

        cam.fieldOfView = Mathf.Lerp(defaultFOV, maxZoomFOV, currentZoomProgress);
    }
}