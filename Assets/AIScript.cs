using System.Collections;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using UnityEditor;
using UnityEditor.Experimental.GraphView;
using UnityEngine;
using UnityEngine.AI;

public class AIScript : MonoBehaviour
{
    // Public variables
    public NavMeshAgent navMeshAgent; // Reference to the NavMeshAgent component for pathfinding
    public float startWaitTime = 4; // Time the AI waits at a waypoint before moving to the next
    public float timeToRotate = 2; // Time the AI spends rotating when it detects the player nearby
    public float speedWalk = 6; // Speed of the AI while patrolling
    public float speedRun = 9; // Speed of the AI while chasing the player

    public float viewRadius = 15; // Radius within which the AI can detect the player
    public float viewAngle = 90; // Field of view angle for player detection
    public LayerMask playerMask; // Layer mask to identify the player
    public LayerMask obstacleMask; // Layer mask to identify obstacles that block the AI's view
    public float meshResolution = 1; // Parameter for fine-tuning the AI's view detection (not fully utilized)
    public int edgeIterations = 4; // Parameter for fine-tuning the AI's view detection (not fully utilized)
    public float edgeDistance = 0.5f; // Parameter for fine-tuning the AI's view detection (not fully utilized)

    public Transform[] waypoints; // Array of transforms representing the patrol points

    // Private variables
    int m_CurrentWaypointIndex; // Index of the current waypoint in the waypoints array

    Vector3 playerLastPosition = Vector3.zero; // Last known position of the player
    Vector3 m_PlayerPosition; // Current position of the player

    float m_WaitTime; // Timer for waiting at waypoints
    float m_TimeToRotate; // Timer for rotating when the player is near
    bool m_PlayerInRange; // Whether the player is within detection range
    bool m_PlayerNear; // Whether the player is near the AI
    bool m_IsPatrol; // Whether the AI is in patrol mode
    bool m_CaughtPlayer; // Whether the AI has caught the player

    // Start is called before the first frame update
    void Start()
    {
        // Initialize the AI's state
        m_PlayerPosition = Vector3.zero;
        m_IsPatrol = true; // Start in patrol mode
        m_CaughtPlayer = false; // Reset caught player flag
        m_PlayerInRange = false; // Reset player in range flag
        m_WaitTime = startWaitTime; // Set initial wait time
        m_TimeToRotate = timeToRotate; // Set initial rotation time

        m_CurrentWaypointIndex = 0; // Start at the first waypoint
        navMeshAgent = GetComponent<NavMeshAgent>(); // Get the NavMeshAgent component

        navMeshAgent.isStopped = false; // Ensure the agent is moving
        navMeshAgent.speed = speedWalk; // Set initial speed to walking speed
        navMeshAgent.SetDestination(waypoints[m_CurrentWaypointIndex].position); // Move to the first waypoint
    }

    // Update is called once per frame
    void Update()
    {
        EnvironmentView(); // Check for the player in the environment

        // Switch between chasing and patrolling based on the AI's state
        if (!m_IsPatrol)
        {
            Chasing(); // Chase the player
        }
        else
        {
            Patroling(); // Patrol between waypoints
        }
    }

    // Method to handle chasing behavior
    private void Chasing()
    {
        m_PlayerNear = false; // Reset player near flag
        playerLastPosition = Vector3.zero; // Reset player's last known position

        if (!m_CaughtPlayer) // If the AI hasn't caught the player
        {
            Move(speedRun); // Move at running speed

            // Find both the player and the canPickUp object
            GameObject player = GameObject.FindGameObjectWithTag("Player");
            GameObject canPickUp = GameObject.FindGameObjectWithTag("canPickUp");

            if (player != null && canPickUp != null)
            {
                float playerDistance = Vector3.Distance(transform.position, player.transform.position);
                float canPickUpDistance = Vector3.Distance(transform.position, canPickUp.transform.position);

                // Chase the closer object
                if (playerDistance < canPickUpDistance)
                {
                    navMeshAgent.SetDestination(player.transform.position);
                }
                else
                {
                    navMeshAgent.SetDestination(canPickUp.transform.position);
                }
            }
            else if (player != null)
            {
                navMeshAgent.SetDestination(player.transform.position);
            }
            else if (canPickUp != null)
            {
                navMeshAgent.SetDestination(canPickUp.transform.position);
            }
        }

        if (navMeshAgent.remainingDistance <= navMeshAgent.stoppingDistance)
        {
            if (m_WaitTime <= 0 && !m_CaughtPlayer && GameObject.FindGameObjectWithTag("Player") != null &&
                Vector3.Distance(transform.position, GameObject.FindGameObjectWithTag("Player").transform.position) >= 6f)
            {
                m_IsPatrol = true; // Switch back to patrol mode
                m_PlayerNear = false;
                Move(speedWalk);
                m_TimeToRotate = timeToRotate;
                m_WaitTime = startWaitTime;
                navMeshAgent.SetDestination(waypoints[m_CurrentWaypointIndex].position);
            }
            else
            {
                Stop();
                m_WaitTime -= Time.deltaTime;
            }
        }
    }


    // Method to handle patrolling behavior
    private void Patroling()
    {
        if (m_PlayerNear) // If the player is near
        {
            if (m_TimeToRotate <= 0) // If rotation time is over
            {
                Move(speedWalk); // Move at walking speed
                LookingPlayer(playerLastPosition); // Look towards the player's last known position
            }
            else
            {
                Stop(); // Stop moving
                m_TimeToRotate -= Time.deltaTime; // Decrease rotation time
            }
        }
        else
        {
            m_PlayerNear = false; // Reset player near flag
            playerLastPosition = Vector3.zero; // Reset player's last known position
            navMeshAgent.SetDestination(waypoints[m_CurrentWaypointIndex].position); // Move to the current waypoint

            // If the AI is close to the current waypoint
            if (navMeshAgent.remainingDistance <= navMeshAgent.stoppingDistance)
            {
                if (m_WaitTime <= 0) // If wait time is over
                {
                    NextPoint(); // Move to the next waypoint
                    Move(speedWalk); // Move at walking speed
                    m_WaitTime = startWaitTime; // Reset wait time
                }
                else
                {
                    Stop(); // Stop moving
                    m_WaitTime -= Time.deltaTime; // Decrease wait time
                }
            }
        }
    }

    // Method to move the AI at a specified speed
    void Move(float speed)
    {
        navMeshAgent.isStopped = false; // Ensure the agent is moving
        navMeshAgent.speed = speed; // Set the agent's speed
    }

    // Method to stop the AI
    void Stop()
    {
        navMeshAgent.isStopped = true; // Stop the agent
        navMeshAgent.speed = 0; // Set speed to zero
    }

    // Method to move the AI to the next waypoint
    public void NextPoint()
    {
        m_CurrentWaypointIndex = (m_CurrentWaypointIndex + 1) % waypoints.Length; // Cycle through waypoints
        navMeshAgent.SetDestination(waypoints[m_CurrentWaypointIndex].position); // Set destination to the next waypoint
    }

    // Method to mark the player as caught
    void CaughtPlayer()
    {
        m_CaughtPlayer = true; // Set caught player flag
    }

    // Method to look towards the player's last known position
    void LookingPlayer(Vector3 player)
    {
        navMeshAgent.SetDestination(player); // Set destination to the player's last known position

        // If the AI is close to the player's last known position
        if (Vector3.Distance(transform.position, player) <= 0.3)
        {
            if (m_WaitTime <= 0) // If wait time is over
            {
                m_PlayerNear = false; // Reset player near flag
                Move(speedWalk); // Move at walking speed
                navMeshAgent.SetDestination(waypoints[m_CurrentWaypointIndex].position); // Move to the next waypoint
                m_WaitTime = startWaitTime; // Reset wait time
                m_TimeToRotate = timeToRotate; // Reset rotation time
            }
            else
            {
                Stop(); // Stop moving
                m_WaitTime -= Time.deltaTime; // Decrease wait time
            }
        }
    }

    // Method to detect the player in the environment
    void EnvironmentView()
    {
        // Detect all colliders within the view radius for player and canPickUp
        Collider[] targetsInRange = Physics.OverlapSphere(transform.position, viewRadius, playerMask | LayerMask.GetMask("canPickUp"));

        Transform closestTarget = null;
        float closestDistance = Mathf.Infinity;

        foreach (Collider target in targetsInRange)
        {
            Transform targetTransform = target.transform;
            Vector3 dirToTarget = (targetTransform.position - transform.position).normalized;
            float distanceToTarget = Vector3.Distance(transform.position, targetTransform.position);

            // Check if the target is within the AI's field of view and not blocked by obstacles
            if (Vector3.Angle(transform.forward, dirToTarget) < viewAngle / 2)
            {
                if (!Physics.Raycast(transform.position, dirToTarget, distanceToTarget, obstacleMask))
                {
                    // If this target is the closest, prioritize it
                    if (distanceToTarget < closestDistance)
                    {
                        closestDistance = distanceToTarget;
                        closestTarget = targetTransform;
                    }
                }
            }
        }

        // If we found a valid target, chase it
        if (closestTarget != null)
        {
            m_IsPatrol = false;
            m_PlayerInRange = true;
            m_PlayerPosition = closestTarget.position;
        }
        else
        {
            m_PlayerInRange = false;
        }
    }

}