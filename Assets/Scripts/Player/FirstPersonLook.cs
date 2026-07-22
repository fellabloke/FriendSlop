using UnityEngine;
using Unity.Netcode;
using UnityEngine.InputSystem;

public class FirstPersonLook : NetworkBehaviour 
{
    [Header("References")]
    [SerializeField] private Transform characterTransform;

    [Header("Input Sockets")]
    [SerializeField] private InputActionReference lookAction;

    [Header("Settings")]
    public float sensitivity = 0.1f; 
    public float smoothing = 1.5f;

    private Vector2 currentVelocity;
    private Vector2 frameVelocity;

    void Reset()
    {
        characterTransform = GetComponentInParent<FirstPersonMovement>().transform;
    }

    public override void OnNetworkSpawn()
    {
        if (IsOwner)
        {
            if (GameManager.Instance != null && GameManager.Instance.CurrentState.Value == GameManager.GameState.inGame)
            {
                Cursor.lockState = CursorLockMode.Locked;
                Cursor.visible = false;
            }
            
            if (lookAction != null) lookAction.action.Enable();
        }
    }

    public override void OnNetworkDespawn()
    {
        if (IsOwner && lookAction != null) 
        {
            lookAction.action.Disable();
        }
    }

    void Update()
    {
        if (!IsOwner || lookAction == null) return;

        Vector2 rawLookInput = lookAction.action.ReadValue<Vector2>();

        Vector2 rawFrameVelocity = Vector2.Scale(rawLookInput, Vector2.one * sensitivity);
        frameVelocity = Vector2.Lerp(frameVelocity, rawFrameVelocity, 1f / smoothing);
        currentVelocity += frameVelocity;

        currentVelocity.y = Mathf.Clamp(currentVelocity.y, -90f, 90f);

        transform.localRotation = Quaternion.AngleAxis(-currentVelocity.y, Vector3.right);
        if (characterTransform != null)
        {
            characterTransform.localRotation = Quaternion.AngleAxis(currentVelocity.x, Vector3.up);
        }
    }

    public void LockCursor()
    {
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }
    public void UnlockCursor()
    {
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
    }
}