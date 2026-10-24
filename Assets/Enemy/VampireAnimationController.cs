using UnityEngine;
using UnityEngine.AI;

[RequireComponent(typeof(NavMeshAgent))]
public class VampireAnimationController : MonoBehaviour
{
    public enum AIState { Idle, Patrol, Chase }

    [Header("Composants")]
    private Animator animator;
    private NavMeshAgent agent;
    private Transform playerTransform;

    [Header("Champs de Détection Visuelle")]
    [SerializeField] private float viewRadius = 8f;
    [SerializeField] private float viewAngle = 50f;
    [SerializeField] private LayerMask obstacleMask;
    [SerializeField] private LayerMask playerMask;

    [Header("Sensibilité Auditive")]
    [SerializeField] private float hearingRadius = 20f;
    [SerializeField] private float noiseThreshold = 0.5f;

    [Header("Patrouille Aléatoire")]
    [SerializeField] private float minPatrolDistance = 6f;
    [SerializeField] private float maxPatrolDistance = 15f;
    [SerializeField] private float minWaitTime = 3f;
    [SerializeField] private float maxWaitTime = 6f;

    [Header("Vitesses de Déplacement")]
    [SerializeField] private float walkSpeed = 1.5f;
    [SerializeField] private float chaseSpeed = 4.5f;

    // État actuel
    private AIState currentState = AIState.Idle;
    private float waitTimer = 0f;
    private float stuckCheckTimer = 0f;

    private void Awake()
    {
        animator = GetComponent<Animator>();
        agent = GetComponent<NavMeshAgent>();
    }

    private void Start()
    {
        GameObject playerObj = GameObject.FindGameObjectWithTag("Player");
        if (playerObj != null)
        {
            playerTransform = playerObj.transform;
        }

        if (agent != null && agent.enabled && agent.isOnNavMesh)
        {
            agent.speed = walkSpeed;
            // Démarrage directement en sélection de patrouille
            GoToNextPatrolPoint();
        }
    }

    private void Update()
    {
        if (agent == null || !agent.enabled || !agent.isOnNavMesh) return;

        LookForPlayer();

        switch (currentState)
        {
            case AIState.Idle:
                HandleIdleState();
                break;
            case AIState.Patrol:
                HandlePatrolState();
                break;
            case AIState.Chase:
                HandleChaseState();
                break;
        }

        UpdateAnimator();
    }

    private void HandleIdleState()
    {
        waitTimer -= Time.deltaTime;
        if (waitTimer <= 0f)
        {
            GoToNextPatrolPoint();
        }
    }

    private void HandlePatrolState()
    {
        // 1. Vérifie si le chemin est valide et si l'agent est arrivé
        if (!agent.pathPending)
        {
            if (agent.remainingDistance <= agent.stoppingDistance || agent.pathStatus == NavMeshPathStatus.PathInvalid)
            {
                EnterIdleState();
                return;
            }
        }

        // 2. Sécurité Anti-Blocage : Si l'ennemi ne bouge quasiment pas pendant 5 secondes en patrouille, on relance
        if (agent.velocity.sqrMagnitude < 0.01f)
        {
            stuckCheckTimer += Time.deltaTime;
            if (stuckCheckTimer >= 5.0f)
            {
                stuckCheckTimer = 0f;
                GoToNextPatrolPoint();
            }
        }
        else
        {
            stuckCheckTimer = 0f;
        }
    }

    private void HandleChaseState()
    {
        if (playerTransform != null)
        {
            agent.SetDestination(playerTransform.position);
        }
    }

    private void EnterIdleState()
    {
        currentState = AIState.Idle;
        waitTimer = Random.Range(minWaitTime, maxWaitTime);
        agent.isStopped = true;
        agent.velocity = Vector3.zero;
    }

    private void GoToNextPatrolPoint()
    {
        agent.isStopped = false;
        agent.speed = walkSpeed;

        if (SetNewRandomDestination())
        {
            currentState = AIState.Patrol;
            stuckCheckTimer = 0f;
        }
        else
        {
            // S'il n'a pas réussi à trouver un point, il attend 1 seconde et réessaie
            EnterIdleState();
            waitTimer = 1.0f;
        }
    }

    #region Vision et Détection Visuelle

    private void LookForPlayer()
    {
        if (playerTransform == null) return;

        Vector3 dirToPlayer = (playerTransform.position - transform.position).normalized;
        float dstToPlayer = Vector3.Distance(transform.position, playerTransform.position);

        if (dstToPlayer <= viewRadius)
        {
            if (Vector3.Angle(transform.forward, dirToPlayer) < viewAngle / 2f)
            {
                if (!Physics.Raycast(transform.position + Vector3.up, dirToPlayer, dstToPlayer, obstacleMask))
                {
                    StartChasing();
                }
            }
        }
    }

    #endregion

    #region Écoute et Bruits

    public void ReceiveNoise(Vector3 noiseSourcePosition, float noiseIntensity)
    {
        if (currentState == AIState.Chase) return;

        float distance = Vector3.Distance(transform.position, noiseSourcePosition);

        if (distance <= hearingRadius)
        {
            float perceivedNoise = noiseIntensity * (1f - (distance / hearingRadius));

            if (perceivedNoise >= noiseThreshold && agent.isOnNavMesh)
            {
                currentState = AIState.Patrol;
                agent.isStopped = false;
                agent.speed = walkSpeed;
                agent.SetDestination(noiseSourcePosition);
            }
        }
    }

    public void TriggerGlobalDetection(Vector3 targetPosition)
    {
        StartChasing();
        if (agent.isOnNavMesh)
        {
            agent.SetDestination(targetPosition);
        }
    }

    #endregion

    #region Chasse & Animations

    public void StartChasing()
    {
        currentState = AIState.Chase;
        if (agent != null && agent.isOnNavMesh)
        {
            agent.isStopped = false;
            agent.speed = chaseSpeed;
        }
    }

    public void StopChasing()
    {
        GoToNextPatrolPoint();
    }

    private bool SetNewRandomDestination()
    {
        if (!agent.isOnNavMesh) return false;

        // Cherche un point éloigné
        for (int i = 0; i < 20; i++)
        {
            Vector3 randomDirection = Random.insideUnitSphere * maxPatrolDistance;

            if (randomDirection.magnitude < minPatrolDistance)
            {
                randomDirection = randomDirection.normalized * minPatrolDistance;
            }

            randomDirection += transform.position;

            if (NavMesh.SamplePosition(randomDirection, out NavMeshHit hit, maxPatrolDistance, NavMesh.AllAreas))
            {
                agent.SetDestination(hit.position);
                return true;
            }
        }

        // Fallback
        Vector3 fallbackDirection = Random.insideUnitSphere * maxPatrolDistance + transform.position;
        if (NavMesh.SamplePosition(fallbackDirection, out NavMeshHit hitFallback, maxPatrolDistance, NavMesh.AllAreas))
        {
            agent.SetDestination(hitFallback.position);
            return true;
        }

        return false;
    }

    private void UpdateAnimator()
    {
        if (animator == null) return;

        bool isMoving = agent.velocity.magnitude > 0.1f && !agent.isStopped;
        bool isChasing = (currentState == AIState.Chase);

        SetAnimatorBoolSafe("IsWalking", isMoving);
        SetAnimatorBoolSafe("IsChasing", isChasing);
    }

    private void SetAnimatorBoolSafe(string paramName, bool value)
    {
        foreach (AnimatorControllerParameter param in animator.parameters)
        {
            if (param.type == AnimatorControllerParameterType.Bool && param.name == paramName)
            {
                animator.SetBool(paramName, value);
                break;
            }
        }
    }

    #endregion
}