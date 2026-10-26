using UnityEngine;
using UnityEngine.AI;

[RequireComponent(typeof(NavMeshAgent))]
public class VampireAnimationController : MonoBehaviour
{
    public enum AIState { SpawnWait, Idle, Patrol, Investigate, Chase }

    [Header("Composants")]
    private Animator animator;
    private NavMeshAgent agent;
    private Transform playerTransform;
    private AudioSource audioSource;

    [Header("Paramètres de Spawn")]
    [SerializeField] private float spawnWaitTime = 5f; // Durée de l'attente au spawn
    private float spawnTimer = 0f;

    [Header("Champs de Détection Visuelle")]
    [SerializeField] private float viewRadius = 10f;
    [SerializeField] private float viewAngle = 60f;
    [SerializeField] private LayerMask obstacleMask;
    [SerializeField] private LayerMask playerMask;

    [Header("Sensibilité Auditive")]
    [SerializeField] private float hearingRadius = 20f;
    [SerializeField] private float noiseThreshold = 0.5f;

    [Header("Patrouille Globale (NavMesh)")]
    [SerializeField] private float minPatrolDistance = 10f;
    [SerializeField] private float maxPatrolDistance = 35f;
    [SerializeField] private float minWaitTime = 2f;
    [SerializeField] private float maxWaitTime = 5f;

    [Header("Vitesses de Déplacement")]
    [SerializeField] private float walkSpeed = 1.5f;
    [SerializeField] private float investigateSpeed = 2.5f;
    [SerializeField] private float chaseSpeed = 4.5f;

    [Header("Paramètres de Chasse & Perte de Vue")]
    [SerializeField] private float loseSightTimerMax = 3.0f;
    private float currentLoseSightTimer = 0f;
    private Vector3 lastKnownPlayerPosition;

    // État actuel
    private AIState currentState = AIState.SpawnWait;
    private float waitTimer = 0f;
    private float stuckCheckTimer = 0f;

    private void Awake()
    {
        animator = GetComponent<Animator>();
        agent = GetComponent<NavMeshAgent>();
        audioSource = GetComponent<AudioSource>();
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
            // S'assure qu'il est bien arrêté pendant le délai de spawn
            agent.isStopped = true;
            agent.velocity = Vector3.zero;
            spawnTimer = spawnWaitTime;
            currentState = AIState.SpawnWait;
        }
    }

    private void Update()
    {
        if (agent == null || !agent.enabled || !agent.isOnNavMesh) return;

        // Gestion de l'état d'attente au spawn (ne fait rien d'autre pendant 5 secondes)
        if (currentState == AIState.SpawnWait)
        {
            spawnTimer -= Time.deltaTime;
            if (spawnTimer <= 0f)
            {
                GoToNextPatrolPoint();
            }
            UpdateAnimator();
            HandlePlayWalkingSound();
            return;
        }

        LookForPlayer();

        switch (currentState)
        {
            case AIState.Idle:
                HandleIdleState();
                break;
            case AIState.Patrol:
                HandlePatrolState();
                break;
            case AIState.Investigate:
                HandleInvestigateState();
                break;
            case AIState.Chase:
                HandleChaseState();
                break;
        }

        UpdateAnimator();
        HandlePlayWalkingSound();
    }

    public bool IsSpawned()
    {
        return currentState != AIState.SpawnWait;
    }

    private void HandlePlayWalkingSound()
    {
        if (audioSource == null) return;
        bool isMoving = agent.velocity.magnitude > 0.1f && !agent.isStopped;
        if (isMoving && !audioSource.isPlaying)
        {
            audioSource.Play();
        }
        else if (!isMoving && audioSource.isPlaying)
        {
            audioSource.Stop();
        }
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
        if (!agent.pathPending)
        {
            if (agent.remainingDistance <= agent.stoppingDistance || agent.pathStatus == NavMeshPathStatus.PathInvalid)
            {
                EnterIdleState();
                return;
            }
        }

        CheckIfStuck();
    }

    private void HandleInvestigateState()
    {
        if (!agent.pathPending && agent.remainingDistance <= agent.stoppingDistance)
        {
            waitTimer -= Time.deltaTime;
            agent.isStopped = true;

            if (waitTimer <= 0f)
            {
                GoToNextPatrolPoint();
            }
        }
        else
        {
            CheckIfStuck();
        }
    }

    private void HandleChaseState()
    {
        if (playerTransform != null)
        {
            float distToPlayer = Vector3.Distance(transform.position, playerTransform.position);
            bool canSeePlayerNow = CanSeePlayerDirectly(distToPlayer);

            if (canSeePlayerNow)
            {
                lastKnownPlayerPosition = playerTransform.position;
                currentLoseSightTimer = 0f;
                agent.SetDestination(lastKnownPlayerPosition);
            }
            else
            {
                currentLoseSightTimer += Time.deltaTime;
                agent.SetDestination(lastKnownPlayerPosition);

                if (currentLoseSightTimer >= loseSightTimerMax)
                {
                    StartInvestigating(lastKnownPlayerPosition);
                }
            }
        }
    }

    private bool CanSeePlayerDirectly(float distToPlayer)
    {
        if (distToPlayer > viewRadius) return false;

        Vector3 dirToPlayer = (playerTransform.position - transform.position).normalized;
        if (Vector3.Angle(transform.forward, dirToPlayer) < viewAngle / 2f)
        {
            if (!Physics.Raycast(transform.position + Vector3.up, dirToPlayer, distToPlayer, obstacleMask))
            {
                return true;
            }
        }
        return false;
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
            EnterIdleState();
            waitTimer = 1.0f;
        }
    }

    private void StartInvestigating(Vector3 targetPos)
    {
        currentState = AIState.Investigate;
        agent.isStopped = false;
        agent.speed = investigateSpeed;
        agent.SetDestination(targetPos);
        waitTimer = Random.Range(3f, 5f);
        stuckCheckTimer = 0f;
    }

    private void CheckIfStuck()
    {
        if (agent.velocity.sqrMagnitude < 0.01f)
        {
            stuckCheckTimer += Time.deltaTime;
            if (stuckCheckTimer >= 4.0f)
            {
                stuckCheckTimer = 0f;
                if (currentState == AIState.Patrol)
                    GoToNextPatrolPoint();
                else if (currentState == AIState.Investigate)
                    EnterIdleState();
            }
        }
        else
        {
            stuckCheckTimer = 0f;
        }
    }

    #region Vision et Détection Visuelle

    private void LookForPlayer()
    {
        // Empêche la détection pendant l'attente de spawn
        if (playerTransform == null || currentState == AIState.Chase || currentState == AIState.SpawnWait) return;

        float dstToPlayer = Vector3.Distance(transform.position, playerTransform.position);
        if (CanSeePlayerDirectly(dstToPlayer))
        {
            StartChasing();
        }
    }

    #endregion

    #region Écoute et Bruits

    public void ReceiveNoise(Vector3 noiseSourcePosition, float noiseIntensity)
    {
        // Ignore les bruits pendant l'attente de spawn ou s'il chasse déjà
        if (currentState == AIState.Chase || currentState == AIState.SpawnWait) return;

        float distance = Vector3.Distance(transform.position, noiseSourcePosition);

        if (distance <= hearingRadius)
        {
            float perceivedNoise = noiseIntensity * (1f - (distance / hearingRadius));

            if (perceivedNoise >= noiseThreshold && agent.isOnNavMesh)
            {
                StartInvestigating(noiseSourcePosition);
            }
        }
    }

    public void TriggerGlobalDetection(Vector3 targetPosition)
    {
        // Ignore la détection globale s'il est encore en phase de spawn
        if (currentState == AIState.SpawnWait) return;

        StartChasing();
        if (agent.isOnNavMesh)
        {
            lastKnownPlayerPosition = targetPosition;
            agent.SetDestination(targetPosition);
        }
    }

    #endregion

    #region Chasse & Animations

    public void StartChasing()
    {
        if (currentState == AIState.SpawnWait) return;

        currentState = AIState.Chase;
        currentLoseSightTimer = 0f;
        if (agent != null && agent.isOnNavMesh)
        {
            agent.isStopped = false;
            agent.speed = chaseSpeed;
            if (playerTransform != null)
            {
                lastKnownPlayerPosition = playerTransform.position;
            }
        }
    }

    private bool SetNewRandomDestination()
    {
        if (!agent.isOnNavMesh) return false;

        for (int i = 0; i < 30; i++)
        {
            Vector3 randomDirection = Random.insideUnitSphere * maxPatrolDistance;

            if (randomDirection.magnitude < minPatrolDistance)
            {
                randomDirection = randomDirection.normalized * minPatrolDistance;
            }

            randomDirection += transform.position;

            if (NavMesh.SamplePosition(randomDirection, out NavMeshHit hit, maxPatrolDistance, NavMesh.AllAreas))
            {
                NavMeshPath path = new NavMeshPath();
                if (agent.CalculatePath(hit.position, path) && path.status == NavMeshPathStatus.PathComplete)
                {
                    agent.SetDestination(hit.position);
                    return true;
                }
            }
        }

        if (NavMesh.SamplePosition(transform.position + Random.insideUnitSphere * minPatrolDistance, out NavMeshHit hitFallback, minPatrolDistance, NavMesh.AllAreas))
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