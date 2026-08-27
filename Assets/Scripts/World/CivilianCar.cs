using Unity.Netcode;
using System.Collections;
using UnityEngine;

public class CivilianCar : NetworkBehaviour
{
    [Header("Car Settings")]
    public float drivingSpeed = 25f; 
    public float despawnZPosition = -50f; 

    public bool IsCrashed { get; private set; } = false;
    private Rigidbody rb;
    public float recoveryTime = 2.5f;
    private float lastPushedTime = -999f;

    void Awake()
    {
        rb = GetComponent<Rigidbody>();
    }
    public void RegisterPush()
    {
        lastPushedTime = Time.time;
    }

    void Update()
    {
        if (!IsServer) return;

        if (transform.position.z < despawnZPosition)
        {
            GetComponent<NetworkObject>().Despawn();
            return;
        }

        if (IsCrashed) return; 

        float highwaySpeed = HighwayManager.Instance != null ? HighwayManager.Instance.currentSpeed.Value : 0f;
        float relativeSpeed = drivingSpeed - highwaySpeed; 

        transform.Translate(Vector3.forward * relativeSpeed * Time.deltaTime, Space.World);
    }
    void FixedUpdate()
    {
        if (!IsServer) return;

        if (IsCrashed && !rb.isKinematic)
        {
            float highwaySpeed = HighwayManager.Instance != null ? HighwayManager.Instance.currentSpeed.Value : 0f;

            float targetZVelocity = drivingSpeed - highwaySpeed;

            Vector3 currentVel = rb.linearVelocity;

            currentVel.z = Mathf.Lerp(currentVel.z, targetZVelocity, Time.fixedDeltaTime * 2f);

            rb.linearVelocity = currentVel;
        }
    }

    private void OnCollisionEnter(Collision collision)
    {
        if (!IsServer) return;

        if(collision.gameObject.CompareTag("Obstacle"))
        {
            rb.isKinematic = false;
            IsCrashed = true;
        }   
    }
    public void TriggerSwerve(Vector3 impactDirection, float severity)
    {
        if (IsCrashed) return;
        IsCrashed = true;

        rb.isKinematic = false;

        float highwaySpeed = HighwayManager.Instance != null ? HighwayManager.Instance.currentSpeed.Value : 0f;
        float relativeSpeed = drivingSpeed - highwaySpeed;

        float swerveForce = severity * 0.5f;

        rb.linearVelocity = new Vector3(impactDirection.x * swerveForce, 0f, relativeSpeed);

        float steerTorque = impactDirection.x * (severity * 15f);
        rb.AddTorque(new Vector3(0f, steerTorque, 0f), ForceMode.Impulse);

        StartCoroutine(RecoverRoutine());
    }
    public void TriggerRearEnd(float severity)
    {
        if (IsCrashed) return;
        IsCrashed = true;

        rb.isKinematic = false;

        float highwaySpeed = HighwayManager.Instance != null ? HighwayManager.Instance.currentSpeed.Value : 0f;
        float relativeSpeed = drivingSpeed - highwaySpeed; 

        float forwardBoost = severity * 1.1f; 

        rb.linearVelocity = new Vector3(0f, 0f, relativeSpeed + forwardBoost);

        rb.AddTorque(new Vector3(0f, Random.Range(-5f, 5f), 0f), ForceMode.Impulse);

        StartCoroutine(RecoverRoutine());
    }
    private IEnumerator RecoverRoutine()
    {
        yield return new WaitForSeconds(recoveryTime);

        while (Time.time - lastPushedTime < 0.5f)
        {
            yield return null;
        }
        rb.linearVelocity = Vector3.zero;
        rb.angularVelocity = Vector3.zero;
        
        rb.isKinematic = true;

        transform.rotation = Quaternion.identity; 

        IsCrashed = false;
    }
}