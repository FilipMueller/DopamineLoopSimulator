using UnityEngine;

public class VehiclePathFollower : MonoBehaviour
{
    [Header("Waypoints")]
    [SerializeField] private Transform startWaypoint;
    [SerializeField] private Transform endWaypoint;

    [Header("Movement")]
    [SerializeField] private float speed = 3f;
    [SerializeField] private float turnSpeed = 5f;
    [SerializeField] private float waypointReachDistance = 1f;

    [Header("Loop")]
    [SerializeField] private bool loop = true;
    [SerializeField] private float respawnDelay = 0f;

    private bool waitingToRespawn = false;
    private float respawnTimer = 0f;

    private void Start()
    {
        if (startWaypoint == null || endWaypoint == null)
        {
            Debug.LogWarning(gameObject.name + ": Start or end waypoint missing.");
            enabled = false;
            return;
        }

        // Important:
        // We do NOT move the car to the start waypoint here.
        // The car starts wherever you placed it in the scene.
    }

    private void Update()
    {
        if (waitingToRespawn)
        {
            respawnTimer -= Time.deltaTime;

            if (respawnTimer <= 0f)
            {
                TeleportToStart();
                waitingToRespawn = false;
            }

            return;
        }

        MoveTowardsEnd();
        RotateTowardsEnd();

        float distance = GetFlatDistance(transform.position, endWaypoint.position);

        if (distance <= waypointReachDistance)
        {
            if (loop)
            {
                waitingToRespawn = true;
                respawnTimer = respawnDelay;
            }
            else
            {
                enabled = false;
            }
        }
    }

    private void MoveTowardsEnd()
    {
        Vector3 targetPosition = endWaypoint.position;
        targetPosition.y = transform.position.y;

        transform.position = Vector3.MoveTowards(
            transform.position,
            targetPosition,
            speed * Time.deltaTime
        );
    }

    private void RotateTowardsEnd()
    {
        Vector3 direction = endWaypoint.position - transform.position;
        direction.y = 0f;

        if (direction.sqrMagnitude < 0.001f)
            return;

        Quaternion targetRotation = Quaternion.LookRotation(direction);

        transform.rotation = Quaternion.Slerp(
            transform.rotation,
            targetRotation,
            turnSpeed * Time.deltaTime
        );
    }

    private void TeleportToStart()
    {
        Vector3 newPosition = startWaypoint.position;
        newPosition.y = transform.position.y;

        transform.position = newPosition;
    }

    private float GetFlatDistance(Vector3 a, Vector3 b)
    {
        a.y = 0f;
        b.y = 0f;
        return Vector3.Distance(a, b);
    }
}