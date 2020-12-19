/*
 * ZombieBehaviourAI.cs - by ThunderWire Studio
 * Version 3.0 Beta (May occur bugs sometimes)
 * 
 * PS: I did everything, what I could. Creating AI is nightmare.
 * 
 * Bugs please report here: thunderwiregames@gmail.com
*/

using System.Linq;
using System.Collections;
using UnityEngine;
using UnityEngine.AI;
using ThunderWire.Utility;
using Newtonsoft.Json.Linq;
using System.Collections.Generic;

/// <summary>
/// Zombie AI System Script
/// </summary>
[RequireComponent(typeof(AudioSource))]
public class ZombieBehaviourAI : MonoBehaviour, INPCReaction, IOnAnimatorState, ISaveable
{
    #region Structures
    [System.Serializable]
    public struct Reaction
    {
        public float distance;
        public int angle;
        public Vector3 position;

        public Reaction(float distance, int angle, Vector3 position)
        {
            this.distance = distance;
            this.angle = angle;
            this.position = position;
        }
    }

    [System.Serializable]
    public class NPCSounds
    {
        public AudioClip ScreamSound;
        public AudioClip EatingSound;
        public AudioClip AgonizeSound;
        public AudioClip TakeDamageSound;
        public AudioClip DieSound;
        public AudioClip[] IdleSounds;
        public AudioClip[] ReactionSounds;
        public AudioClip[] AttackSounds;
    }

    [System.Serializable]
    public class NPCSoundsVolume
    {
        public float ScreamVolume = 1f;
        public float EatingVolume = 1f;
        public float AgonizeVolume = 1f;
        public float TakeDamageVolume = 1f;
        public float DieVolume = 1f;
        public float IdleVolume = 1f;
        public float ReactionVolume = 1f;
        public float AttackVolume = 1f;
    }

    struct WaypointsData
    {
        public WaypointGroup waypointGroup;
        public float closestDistance;

        public WaypointsData(WaypointGroup wg, float dist)
        {
            waypointGroup = wg;
            closestDistance = dist;
        }
    }
    #endregion

    public enum PrimaryBehaviour { Idle, Chase, Patrol, AttractedBehaviour }
    public enum SecondaryBehaviour { Normal, Agony, Scream, Eating, Reaction }
    public enum ReactionTrigger { None, Hit, Sound }

    public enum GeneralBehaviour { Waypoint2Waypoint, W2WPatrol, W2WPatrolIdle }
    public enum IdleType { StandUpBack, StandUpFront, Idle, None }

    private AudioSource audioSource;
    private NavMeshAgent agent;
    private NPCHealth health;

    private WaypointGroup waypoints;
    private HungerPoint[] hungerPositions;
    private HungerPoint closestHungerPoint;

    private PlayerController playerController;
    private HealthManager playerHealth;
    private Transform playerObject;
    private Transform playerCam;

    [Header("Behaviour Main")]
    [ReadOnly, SerializeField] private PrimaryBehaviour primaryBehaviour = PrimaryBehaviour.Idle;
    [ReadOnly, SerializeField] private SecondaryBehaviour secondaryBehaviour = SecondaryBehaviour.Normal;
    [ReadOnly, SerializeField] private ReactionTrigger reactionTrigger = ReactionTrigger.None;
    [ReadOnly, SerializeField] private Waypoint nextWaypoint;
    private Reaction reaction;

    [Space(5)]
    [Tooltip("Starting Zombie Behaviour")]
    public IdleType sleepBehaviour = IdleType.StandUpBack;
    [Tooltip("General Zombie Behaviour")]
    public GeneralBehaviour zombieBehaviour = GeneralBehaviour.W2WPatrol;

    [Header("Main Setup")]
    public Animator animator;
    public LayerMask searchMask;
    public int attackAnimations = 4;
    public bool waypointsReassign = true;
    public bool gizmosEnabled;
    public bool playerInvisible;

    [Header("Player Damage")]
    [MinMax(1, 100)]
    public Vector2 damageValue = new Vector2(20, 40);
    public Vector2 damageKickback;
    public float kickbackTime;
    public bool damagePlayer;

    [Header("Behaviour Settings")]
    public bool enableScream = true;
    public bool enableAgony = true;
    public bool enableHunger = true;
    public bool soundReaction = true;
    public bool runToPlayer = true;
    public bool randomWaypoint = true;
    public bool hungerRecoverHealth = true;
    public float playerLostPatrol = 5f;
    public float hungerPoints = 30f;
    [MinMax(1, 30)]
    public Vector2 patrolTime = new Vector2(5, 10);
    [MinMax(10, 240)]
    public Vector2 screamNext = new Vector2(120, 150);
    [MinMax(10, 240)]
    public Vector2 agonyNext = new Vector2(60, 120);

    [Header("Sensors")]
    public Vector3 headOffset;
    public int reactionAngleTurn = 40;
    public float soundReactClose = 10f;
    public float soundReactFar = 20f;
    public bool soundReactionGizmos;

    [Header("Sensor Settings")]
    [Range(0, 179)]
    public float sightsFOV = 110;
    public float attackFOV = 30;
    public float sightsDistance = 15;
    public float attackDistance = 5f;
    public float idleHearRange = 10f;
    public float chaseTimeHide = 2f;

    [Header("AI Settings")]
    public float walkSpeed = 0.4f;
    public float runSpeed = 5.5f;
    public float agentRotationSpeed = 5f;
    public float speedChangeSpeed = 1f;

    [Header("Root Motion")]
    public bool walkRootMotion;

    [Header("Sounds")]
    public NPCSounds m_NPCSounds;
    public NPCSoundsVolume m_NPCSoundsVolume;

    [Space(5)]
    [Tooltip("Play Sounds when zombie is in attracted state.")]
    public bool playAttractedSounds = true;
    public bool eventPlayAttackSound;
    public bool eventPlayScreamSound;
    public bool eventPlayAgonySound;
    public bool eventPlayEatSound;

    [HideInInspector]
    public bool isDead = false;

    #region Private Variables
    private Vector3 playerHead;
    private Vector3 headPosition;

    private Vector3 lastChasePosition;
    private Vector3 lastWaypointPos;
    private Vector3 lastCorrectDestination;

    private float chaseTime;
    private float agonyTime;

    private bool enableRootMotion;
    private bool enableRootRotation;

    private bool enableSights;
    private bool shouldMove;

    private bool canContinue;
    private bool canAttack;
    private bool canScream;
    private bool canReact;
    private bool canPlaySound;

    private bool primaryPending;
    private bool secondaryPending;
    private bool reactionPending;

    private bool waypointsAssigned;
    private bool goToLastWaypoint;
    private bool chasePatrol;
    private bool hasTurned;

    private bool patrolWait;
    private bool agonyWait;
    private bool hungerWait;
    private bool loadSaved;

    private int lastAttack;
    private int lastSound;
    #endregion

    void Awake()
    {
        audioSource = GetComponent<AudioSource>();
        agent = GetComponent<NavMeshAgent>();
        playerController = PlayerController.Instance;
        playerObject = playerController.transform;
        playerHealth = playerController.gameObject.GetComponent<HealthManager>();

        if (GetComponent<NPCHealth>())
        {
            health = GetComponent<NPCHealth>();
        }

        playerCam = ScriptManager.Instance.MainCamera.transform;
        hungerPositions = FindObjectsOfType<HungerPoint>();
    }

    void Start()
    {
        agent.updateRotation = false;
        agent.updatePosition = false;
        agent.isStopped = false;
        waypointsAssigned = false;
        enableSights = true;
        canAttack = true;
        canReact = true;
        canPlaySound = true;

        if (!waypointsReassign)
        {
            WaypointGroup nextWaypoints = FindClosestWaypoints();

            if (nextWaypoints != waypoints)
            {
                waypoints = FindClosestWaypoints();
                goToLastWaypoint = false;
            }

            waypointsAssigned = true;
        }

        Invoke("LateStart", 1f);

        if (!agent.isOnNavMesh)
        {
            Debug.LogError("[Zombie AI] Please create NavMesh first!");
        }
    }

    void LateStart()
    {
        if (sleepBehaviour != IdleType.None)
        {
            primaryBehaviour = PrimaryBehaviour.Idle;
            animator.SetInteger("IdleState", (int)sleepBehaviour);
            animator.SetBool("Idle", true);
            animator.SetBool("Patrol", false);
        }
        else
        {
            animator.SetBool("Idle", false);
            animator.SetBool("Patrol", true);
            canContinue = true;
        }
    }

    void OnAnimatorMove()
    {
        if (enableRootMotion)
        {
            Vector3 position = animator.rootPosition;
            position.y = agent.nextPosition.y;
            transform.position = position;

            if (enableRootRotation)
            {
                transform.rotation = animator.rootRotation;
            }

            agent.nextPosition = position;
        }
    }

    void Update()
    {
        if (!loadSaved)
        {
            canScream = true;
            loadSaved = true;
        }

        if (isDead)
        {
            SetAnimatorState();
            StopAllCoroutines();
            agent.enabled = false;
            animator.enabled = false;

            if (GetComponent<Collider>())
            {
                GetComponent<Collider>().enabled = false;
            }

            return;
        }

        //Set correct head positions
        Vector3 _playerCam = playerCam.position;
        playerHead = new Vector3(playerObject.position.x, _playerCam.y, playerObject.position.z);
        headPosition = transform.position + headOffset;

        //Should we allow NavMeshAgent update transform position, when Root Motion is enabled?
        agent.updatePosition = !enableRootMotion;

        //Get player velocity magnitude
        Vector3 pvelocity = playerController.characterController.velocity;
        float pmagnitude = new Vector3(pvelocity.x, 0, pvelocity.z).magnitude;

        //Check if zombie is in agent destination
        shouldMove = !PathCompleted();

        //Is player currently moving?
        bool playerMoving = DistanceOf(_playerCam) > agent.stoppingDistance && pmagnitude > 1;

        //Set zombie animation to a starting behaviour
        if (sleepBehaviour != IdleType.None)
        {
            if (InDistance(idleHearRange))
            {
                animator.SetInteger("IdleState", (int)IdleType.None);
                animator.SetBool("Idle", false);
                sleepBehaviour = IdleType.None;
                PlaySoundRandom(m_NPCSounds.ReactionSounds, m_NPCSoundsVolume.ReactionVolume);
            }
        }
        else if (SearchForPlayer())
        {
            bool notAgonyOrEat = secondaryBehaviour != SecondaryBehaviour.Agony && secondaryBehaviour != SecondaryBehaviour.Eating;

            if (notAgonyOrEat)
            {
                //Should we enable root motion?
                enableRootMotion = runToPlayer ? false : walkRootMotion;

                //Reset variables
                enableRootRotation = false;
                primaryPending = false;
                hungerWait = false;
                reactionPending = false;
                canReact = true;
                canPlaySound = true;
                waypointsAssigned = false;
                enableSights = false;
            }

            if (primaryBehaviour != PrimaryBehaviour.Chase)
            {
                //Is player chased? If not, exectue Stand or Scream animation
                if (enableScream && canScream)
                {
                    canContinue = false;

                    SetAnimatorState();
                    animator.SetBool("Scream", true);

                    if (notAgonyOrEat)
                    {
                        SetAgentDestination(lastChasePosition, true);
                        StartCoroutine(NextScreamWait());
                        if (!eventPlayScreamSound) PlaySound(m_NPCSounds.ScreamSound, m_NPCSoundsVolume.ScreamVolume);
                        secondaryBehaviour = SecondaryBehaviour.Scream;
                        canScream = false;
                        canContinue = false;
                    }
                }
                else if(primaryBehaviour != PrimaryBehaviour.Idle)
                {
                    if (notAgonyOrEat)
                    {
                        SetAnimatorState(!runToPlayer, runToPlayer);
                        SetAgentDestination(lastChasePosition);
                        PlaySoundRandom(m_NPCSounds.ReactionSounds, m_NPCSoundsVolume.ReactionVolume);
                        canContinue = true;
                    }
                    else
                    {
                        SetAnimatorState(!runToPlayer, runToPlayer);
                        canContinue = false;
                    }
                }

                if (patrolWait)
                {
                    goToLastWaypoint = true;
                    patrolWait = false;
                    hungerWait = false;
                }
            }
            else if(secondaryBehaviour == SecondaryBehaviour.Scream && !canContinue)
            {
                //Rotate towards player if zombie is screaming and set next state
                SetAnimatorState(!runToPlayer, runToPlayer);
                SetAgentDestination(lastChasePosition, true);
                RotateTowards(lastChasePosition);
            }
            else if (secondaryBehaviour != SecondaryBehaviour.Normal && canContinue)
            {
                //Continue to chase
                secondaryBehaviour = SecondaryBehaviour.Normal;
            }
            else if (canContinue)
            {
                //Chase player if is visible and if time is passed
                SetSpeed(runToPlayer ? runSpeed : walkSpeed, true);
                SetAgentDestination(lastChasePosition);
                RotateTowards(agent.steeringTarget);

                SetAnimatorState(disableOthers: true);
                animator.SetBool(runToPlayer ? "Running" : "Walking", shouldMove || playerMoving);
                animator.SetBool("Patrol", !shouldMove);

                //Attack Player
                if (IsPlayerInSights(attackFOV) && InDistance(attackDistance) && canAttack)
                {
                    if (!eventPlayAttackSound) PlaySoundRandom(m_NPCSounds.AttackSounds, m_NPCSoundsVolume.AttackVolume);
                    animator.SetInteger("AttackState", lastAttack = Tools.RandomUnique(0, attackAnimations, lastAttack));
                    animator.SetTrigger("Attack");
                    canAttack = false;
                }
            }

            if (notAgonyOrEat)
            {
                primaryBehaviour = PrimaryBehaviour.Chase;
            }
        }
        else if(canContinue || secondaryBehaviour != SecondaryBehaviour.Scream)
        {
            if (!primaryPending && shouldMove)
            {
                RotateTowards(agent.steeringTarget);
            }

            if (shouldMove && primaryBehaviour == PrimaryBehaviour.Chase)
            {
                //Continue to last seen destination
                enableRootMotion = runToPlayer ? false : walkRootMotion;
                SetAgentDestination(lastChasePosition);
                SetAnimatorState(!runToPlayer, runToPlayer, false, false, true);
                SetSpeed(runToPlayer ? runSpeed : walkSpeed, true);
                enableSights = false;
                primaryPending = false;
                chasePatrol = true;
                secondaryBehaviour = SecondaryBehaviour.Normal;
            }
            else if(!reactionPending)
            {
                enableSights = true;
                SecondaryUpdate();
            }
            else
            {
                enableSights = true;
                ReactionUpdate();
            }
        }
    }

    /// <summary>
    /// Main Secondary Behaviour Loop
    /// </summary>
    void SecondaryUpdate()
    {
        if (!waypointsAssigned && waypointsReassign)
        {
            WaypointGroup nextWaypoints = FindClosestWaypoints();

            if (nextWaypoints != waypoints)
            {
                waypoints = FindClosestWaypoints();
                goToLastWaypoint = false;
            }

            waypointsAssigned = true;
        }

        if (chasePatrol)
        {
            PlaySoundRandom(m_NPCSounds.ReactionSounds, m_NPCSoundsVolume.ReactionVolume);
            SetAnimatorState(false, false, true, false, true);
            StartCoroutine(WaitSecondary(playerLostPatrol));
            agent.isStopped = true;
            agent.updateRotation = false;
            secondaryPending = true;
            primaryPending = true;
            primaryBehaviour = PrimaryBehaviour.Patrol;
            secondaryBehaviour = SecondaryBehaviour.Normal;
            chasePatrol = false;
        }

        if (secondaryBehaviour == SecondaryBehaviour.Normal)
        {
            if (!secondaryPending)
            {
                primaryPending = false;

                if (enableHunger)
                {
                    if(hungerPoints > 0)
                    {
                        hungerPoints -= Time.deltaTime;
                    }
                    else
                    {
                        closestHungerPoint = FindClosestHungerPoint();

                        if (closestHungerPoint && IsObjectVisible(sightsDistance, closestHungerPoint.transform.position))
                        {
                            enableRootMotion = false;
                            SetAgentDestination(closestHungerPoint.transform.position);
                            SetAnimatorState(false, true);
                            SetSpeed(runSpeed, true);
                            hungerWait = true;
                            patrolWait = false;
                            goToLastWaypoint = true;
                            secondaryPending = true;
                            hungerPoints = 10;
                            return;
                        }
                    }
                }

                if (enableAgony)
                {
                    if (!agonyWait)
                    {
                        agonyTime = Random.Range(agonyNext.x, agonyNext.y);
                        agonyWait = true;
                    }
                    else
                    {
                        if (agonyTime > 0)
                        {
                            agonyTime -= Time.deltaTime;
                        }
                        else
                        {
                            if (!eventPlayAgonySound) PlaySound(m_NPCSounds.AgonizeSound, m_NPCSoundsVolume.AgonizeVolume);
                            SetAnimatorState(disableOthers: true);
                            animator.SetTrigger("Agonize");
                            agent.isStopped = true;
                            secondaryPending = true;
                            agonyWait = false;
                            hungerPoints += 5;
                            secondaryBehaviour = SecondaryBehaviour.Agony;
                            return;
                        }
                    }
                }

                if (!shouldMove)
                {
                    if (zombieBehaviour == GeneralBehaviour.Waypoint2Waypoint)
                    {
                        enableRootMotion = walkRootMotion;
                        SetAgentDestination(NextWaypoint());
                        SetAnimatorState(true);
                        SetSpeed(walkSpeed, true);
                        primaryBehaviour = PrimaryBehaviour.AttractedBehaviour;
                    }
                    else if (zombieBehaviour == GeneralBehaviour.W2WPatrol || zombieBehaviour == GeneralBehaviour.W2WPatrolIdle)
                    {
                        if (!patrolWait)
                        {
                            enableRootMotion = walkRootMotion;
                            SetAgentDestination(goToLastWaypoint ? lastWaypointPos : NextWaypoint());
                            SetAnimatorState(true);
                            SetSpeed(walkSpeed, true);
                            secondaryPending = false;
                            patrolWait = true;
                            goToLastWaypoint = false;
                            canPlaySound = true;
                            primaryBehaviour = PrimaryBehaviour.AttractedBehaviour;
                        }
                        else
                        {
                            if (zombieBehaviour == GeneralBehaviour.W2WPatrol)
                            {
                                SetAnimatorState(false, false, true, false, true);
                            }
                            else if (zombieBehaviour == GeneralBehaviour.W2WPatrolIdle)
                            {
                                SetAnimatorState(false, false, false, true, true);
                            }

                            if (canPlaySound && playAttractedSounds)
                            {
                                PlaySoundRandom(m_NPCSounds.IdleSounds, m_NPCSoundsVolume.IdleVolume);
                                canPlaySound = false;
                            }

                            StartCoroutine(WaitSecondary(Random.Range(patrolTime.x, patrolTime.y)));
                            agent.isStopped = true;
                            secondaryPending = true;
                            patrolWait = false;
                            primaryBehaviour = PrimaryBehaviour.Patrol;
                        }
                    }
                }
                else if(!secondaryPending)
                {
                    SetAnimatorState(true);
                    agent.isStopped = false;

                    if (canPlaySound && playAttractedSounds)
                    {
                        PlaySoundRandom(m_NPCSounds.IdleSounds, m_NPCSoundsVolume.IdleVolume);
                        canPlaySound = false;
                    }
                }
            }
            else if (!shouldMove)
            {
                if (hungerWait)
                {
                    HungerPoint.HungerPoints hunger_points = closestHungerPoint.GetHungerPoints();
                    hungerPoints = hunger_points.hungerPoints;

                    if (hungerRecoverHealth)
                    {
                        health.Health += hunger_points.healthPoints;
                    }

                    SetAnimatorState();
                    if (!eventPlayEatSound) PlaySound(m_NPCSounds.EatingSound, m_NPCSoundsVolume.EatingVolume);
                    animator.SetTrigger("Eat");
                    hungerWait = false;
                    secondaryBehaviour = SecondaryBehaviour.Eating;
                }
            }
        }
        else if (!canContinue)
        {
            SetAnimatorState(false, false, true, false, true);
            agent.isStopped = true;
            agent.updateRotation = false;
            secondaryPending = true;
            primaryPending = true;
            canContinue = true;
        }
    }

    /// <summary>
    /// Main Reaction Behaviour Loop
    /// </summary>
    void ReactionUpdate()
    {
        if (reactionTrigger == ReactionTrigger.Hit)
        {
            if(!secondaryPending && !hasTurned)
            {
                StartCoroutine(WaitSecondary(Random.Range(patrolTime.x, patrolTime.y)));
                secondaryPending = true;
                canReact = true;
                hasTurned = true;
            }
            else if(!secondaryPending)
            {
                if (!waypointsAssigned && waypointsReassign)
                {
                    WaypointGroup nextWaypoints = FindClosestWaypoints();

                    if (nextWaypoints != waypoints)
                    {
                        waypoints = FindClosestWaypoints();
                        goToLastWaypoint = false;
                    }

                    waypointsAssigned = true;
                }

                PlaySoundRandom(m_NPCSounds.IdleSounds, m_NPCSoundsVolume.IdleVolume);
                secondaryBehaviour = SecondaryBehaviour.Normal;
                primaryBehaviour = PrimaryBehaviour.AttractedBehaviour;
                reactionTrigger = ReactionTrigger.None;
                reaction = new Reaction();
                enableRootRotation = false;
                enableRootMotion = walkRootMotion;
                primaryPending = false;
                canReact = true;
                reactionPending = false;
            }
        }
        else if(reactionTrigger == ReactionTrigger.Sound)
        {
            if (!secondaryPending && primaryBehaviour == PrimaryBehaviour.AttractedBehaviour)
            {
                canReact = true;

                if (reaction.distance <= soundReactClose && !IsObjectVisible(sightsDistance, reaction.position))
                {
                    SetAnimatorState(false, false, true);
                    StartCoroutine(WaitSecondary(Random.Range(patrolTime.x, patrolTime.y)));
                    primaryBehaviour = PrimaryBehaviour.Patrol;
                    secondaryPending = true;
                }
                else if(reaction.distance <= soundReactFar && canContinue)
                {
                    if (!shouldMove)
                    {
                        if (!patrolWait)
                        {
                            agent.isStopped = false;
                            enableRootRotation = false;
                            enableRootMotion = false;
                            SetSpeed(runSpeed, false);
                            SetAgentDestination(reaction.position);
                            SetAnimatorState(false, true);
                            patrolWait = true;
                        }
                        else
                        {
                            SetAnimatorState(false, false, true);
                            StartCoroutine(WaitSecondary(Random.Range(patrolTime.x, patrolTime.y)));
                            primaryBehaviour = PrimaryBehaviour.Patrol;
                            secondaryPending = true;
                        }
                    }
                    else
                    {
                        RotateTowards(agent.steeringTarget);
                        SetAnimatorState(false, true);
                    }
                }
            }
            else if (!secondaryPending)
            {
                if (!waypointsAssigned && waypointsReassign)
                {
                    WaypointGroup nextWaypoints = FindClosestWaypoints();

                    if (nextWaypoints != waypoints)
                    {
                        waypoints = FindClosestWaypoints();
                        goToLastWaypoint = false;
                    }

                    waypointsAssigned = true;
                }

                PlaySoundRandom(m_NPCSounds.IdleSounds, m_NPCSoundsVolume.IdleVolume);
                secondaryBehaviour = SecondaryBehaviour.Normal;
                primaryBehaviour = PrimaryBehaviour.AttractedBehaviour;
                reactionTrigger = ReactionTrigger.None;
                reaction = new Reaction();
                patrolWait = false;
                enableRootRotation = false;
                enableRootMotion = walkRootMotion;
                primaryPending = false;
                canReact = true;
                reactionPending = false;
            }
        }
    }

    /// <summary>
    /// Find Waypoints by closest point distance.
    /// </summary>
    WaypointGroup FindClosestWaypoints()
    {
        WaypointsData[] waypoints = (from w in FindObjectsOfType<WaypointGroup>()
                                  select new WaypointsData(w, 0)).ToArray();

        for (int i = 0; i < waypoints.Length; i++)
        {
            float distance = 0;

            foreach (var point in waypoints[i].waypointGroup.Waypoints)
            {
                float newDistance = 0;

                if ((newDistance = Vector3.Distance(transform.position, point.transform.position)) < distance || distance == 0)
                {
                    distance = newDistance;
                }
            }

            waypoints[i].closestDistance = distance;
        }

        return waypoints.OrderBy(x => x.closestDistance).FirstOrDefault().waypointGroup;
    }

    /// <summary>
    /// Find Closest Hunger Point based on NavMeshPath.
    /// </summary>
    HungerPoint FindClosestHungerPoint()
    {
        if (hungerPositions.Length > 0)
        {
            HungerPoint closest = hungerPositions[0];
            float closest_length = CalculatePathLength(closest.transform.position);

            foreach (var point in hungerPositions)
            {
                float length = 0;

                if ((length = CalculatePathLength(point.transform.position)) > closest_length)
                {
                    closest_length = length;
                    closest = point;
                }
            }

            return closest;
        }

        return null;
    }

    IEnumerator NextScreamWait()
    {
        yield return new WaitForSeconds(Random.Range(screamNext.x, screamNext.y));
        canScream = true;
    }

    IEnumerator WaitSecondary(float time)
    {
        yield return new WaitForSeconds(time);

        if (primaryBehaviour != PrimaryBehaviour.Chase)
        {
            secondaryBehaviour = SecondaryBehaviour.Normal;
            secondaryPending = false;
        }
    }

    IEnumerator WaitForAnimation(float time, SecondaryBehaviour anim, bool changeBehaviour = true)
    {
        yield return new WaitForSeconds(time);

        if (secondaryBehaviour == anim)
        {
            secondaryPending = false;

            if (changeBehaviour)
            {
                secondaryBehaviour = SecondaryBehaviour.Normal;
            }
        }
    }

    IEnumerator WaiToContinue(float time)
    {
        yield return new WaitForSeconds(time);
        canContinue = true;
    }

    /// <summary>
    /// Function is called by weapon hit from NPCHealth script.
    /// </summary>
    public void HitReaction()
    {
        sleepBehaviour = IdleType.None;

        if (secondaryBehaviour != SecondaryBehaviour.Eating)
        {
            animator.SetTrigger("Hit");
        }

        if(primaryBehaviour != PrimaryBehaviour.Chase)
        {
            PlaySound(m_NPCSounds.TakeDamageSound, m_NPCSoundsVolume.TakeDamageVolume);

            if (canReact)
            {
                int angle = transform.RealSignedAngle(playerObject.position);

                if (Mathf.Abs(angle) >= reactionAngleTurn)
                {
                    //Stop pending operations
                    StopAllCoroutines();

                    agent.ResetPath();
                    agent.updateRotation = false;
                    agent.isStopped = true;
                    patrolWait = false;
                    goToLastWaypoint = true;
                    enableRootMotion = true;
                    enableRootRotation = true;
                    secondaryPending = true;
                    primaryPending = true;
                    hasTurned = false;
                    waypointsAssigned = false;

                    SetAnimatorState();
                    animator.SetFloat("TurnAngle", angle);
                    animator.SetTrigger("Turn");

                    reaction = new Reaction();
                    reactionTrigger = ReactionTrigger.Hit;
                    secondaryBehaviour = SecondaryBehaviour.Reaction;
                    primaryBehaviour = PrimaryBehaviour.AttractedBehaviour;
                    reactionPending = true;
                    canReact = false;
                }
            }
        }
    }

    /// <summary>
    /// Function is called by sound impact.
    /// </summary>
    public void SoundReaction(int type, float distance, Vector3 pos)
    {
        if (!soundReaction) return;

        sleepBehaviour = IdleType.None;

        if (primaryBehaviour != PrimaryBehaviour.Chase)
        {
            PlaySoundRandom(m_NPCSounds.ReactionSounds, m_NPCSoundsVolume.ReactionVolume);

            if (canReact && distance <= soundReactFar)
            {
                int angle = transform.RealSignedAngle(pos);
                reaction = new Reaction(distance, angle, type == 0 ? pos : playerObject.position);

                //Stop pending operations
                StopAllCoroutines();
                SetAnimatorState();

                agent.ResetPath();
                patrolWait = false;
                goToLastWaypoint = true;
                enableRootMotion = true;
                enableRootRotation = true;
                primaryPending = true;
                waypointsAssigned = false;

                if (Mathf.Abs(angle) >= reactionAngleTurn)
                {
                    animator.SetFloat("TurnAngle", angle);
                    animator.SetTrigger("Turn");
                    secondaryPending = true;
                }
                else
                {
                    secondaryPending = false;
                }

                reactionTrigger = ReactionTrigger.Sound;
                secondaryBehaviour = SecondaryBehaviour.Reaction;
                primaryBehaviour = PrimaryBehaviour.AttractedBehaviour;
                reactionPending = true;
                canReact = false;
            }
        }
    }

    /// <summary>
    /// Callback for IOnAnimatorState interface.
    /// Function is called from Animator State Behaviour.
    /// </summary>
    public void OnStateEnter(AnimatorStateInfo state, string name)
    {
        if (name == "AttackIdle")
        {
            canAttack = true;
        }
        else if(name == "Scream")
        {
            StartCoroutine(WaiToContinue(state.length));
            SetAnimatorState(!runToPlayer, runToPlayer, disableOthers: true);
        }
        else if (name == "Agony")
        {
            StartCoroutine(WaitForAnimation(state.length, SecondaryBehaviour.Agony));
            SetAnimatorState(true);
        }
        else if (name == "Eat")
        {
            StartCoroutine(WaitForAnimation(state.length, SecondaryBehaviour.Eating));
            SetAnimatorState(true);
        }
        else if (name == "Turn")
        {
            StartCoroutine(WaitForAnimation(state.length, SecondaryBehaviour.Reaction, false));
            SetAnimatorState(false, false, true);
        }
        else if(!enableScream || secondaryBehaviour == SecondaryBehaviour.Reaction && reactionPending)
        {
            StartCoroutine(WaiToContinue(state.length));
            SetAnimatorState(!runToPlayer, runToPlayer, disableOthers: true);
        }
    }

    /// <summary>
    /// Callback for Death Event
    /// </summary>
    public void DeathTrigger()
    {
        PlaySound(m_NPCSounds.DieSound, m_NPCSoundsVolume.DieVolume);
        isDead = true;
    }

    /// <summary>
    /// Get Path Possible Next Waypoint
    /// </summary>
    Vector3? NextWaypoint()
    {
        if (waypoints && waypoints.Waypoints.Count > 1)
        {
            if (randomWaypoint)
            {
                Waypoint[] possibleWaypoints = waypoints.Waypoints.Where(x => !x.isOccupied && IsPathPossible(x.transform.position) && x != nextWaypoint).ToArray();

                if (possibleWaypoints.Length > 0)
                {
                    if (nextWaypoint)
                    {
                        nextWaypoint.isOccupied = false;
                        nextWaypoint.occupiedBy = null;
                    }

                    nextWaypoint = possibleWaypoints[Random.Range(0, possibleWaypoints.Length - 1)];
                    nextWaypoint.isOccupied = true;
                    nextWaypoint.occupiedBy = gameObject;

                    return lastWaypointPos = nextWaypoint.transform.position;
                }
                else
                {
                    return transform.position;
                }
            }
            else
            {
                List<Waypoint> possibleWaypoints = waypoints.Waypoints.Where(x => (!x.isOccupied || x.isOccupied && x.occupiedBy == gameObject) && IsPathPossible(x.transform.position)).OrderBy(x => x.gameObject.name).ToList();

                if (nextWaypoint)
                {
                    nextWaypoint.isOccupied = false;
                    nextWaypoint.occupiedBy = null;
                }

                int next = nextWaypoint != null ? possibleWaypoints.IndexOf(nextWaypoint) == possibleWaypoints.Count - 1 ? 0 : possibleWaypoints.IndexOf(nextWaypoint) + 1 : 0;

                nextWaypoint = possibleWaypoints[next];
                nextWaypoint.isOccupied = true;
                nextWaypoint.occupiedBy = gameObject;

                return lastWaypointPos = nextWaypoint.transform.position;
            }
        }

        Debug.LogError("[AI Waypoint] Could not set next waypoint!");
        return null;
    }

    bool IsPathPossible(Vector3 pos)
    {
        NavMeshPath path = new NavMeshPath();
        agent.CalculatePath(pos, path);
        return path.status != NavMeshPathStatus.PathPartial && path.status != NavMeshPathStatus.PathInvalid;
    }

    /// <summary>
    /// Trigger Animation State
    /// </summary>
    void SetAnimatorState(bool walk = false, bool run = false, bool patrol = false, bool idle = false, bool disableOthers = false)
    {
        animator.SetBool("Walking", walk);
        animator.SetBool("Running", run);
        animator.SetBool("Patrol", patrol);
        animator.SetBool("Idle", idle);

        if (disableOthers)
        {
            animator.SetBool("Scream", false);
        }
    }

    /// <summary>
    /// Set Agent or RootMotion Speed
    /// </summary>
    void SetSpeed(float speed, bool agentLerp = false)
    {
        if (enableRootMotion)
        {
            animator.SetFloat("MovementSpeed", speed);
            agent.speed = speed;
        }
        else
        {
            if (agentLerp)
            {
                StartCoroutine(LerpAgentSpeed(speed));
            }
            else
            {
                agent.speed = speed;
            }
        }
    }

    IEnumerator LerpAgentSpeed(float speed)
    {
        bool right = agent.speed > speed ? false : true;

        while(right ? agent.speed < speed : agent.speed > speed)
        {
            agent.speed = Mathf.Lerp(agent.speed, speed, Time.deltaTime * speedChangeSpeed);
            yield return null;
        }

        agent.speed = speed;
    }

    /// <summary>
    /// Play Single Sound
    /// </summary>
    void PlaySound(AudioClip clip, float volume = 1f)
    {
        if (audioSource && clip != null)
        {
            audioSource.volume = volume;
            audioSource.clip = clip;
            audioSource.Play();
        }
    }

    /// <summary>
    /// Play Random Sounds
    /// </summary>
    void PlaySoundRandom(AudioClip[] clips, float volume = 1f)
    {
        if (audioSource && clips.Length > 0)
        {
            lastSound = Tools.RandomUnique(0, clips.Length, lastSound);
            audioSource.volume = volume;
            audioSource.clip = clips[lastSound];
            audioSource.Play();
        }
    }

    /// <summary>
    /// Play Sound by Animation or Apply Player Damage Event
    /// </summary>
    public void PlaySoundOrDamageEvent(int type)
    {
        if(type == 0)
        {
            //Attack Sound
            if (eventPlayAttackSound)
            {
                PlaySoundRandom(m_NPCSounds.AttackSounds, m_NPCSoundsVolume.AttackVolume);
            }

            //Damage Player
            if (damagePlayer && IsPlayerInSights(attackFOV) && InDistance(attackDistance))
            {
                StartCoroutine(playerController.ApplyKickback(new Vector3(damageKickback.x, damageKickback.y, 0), kickbackTime));
                playerHealth.ApplyDamage(Random.Range(damageValue.x, damageValue.y));
            }
        }
        else if(type == 1 && eventPlayScreamSound)
        {
            //Scream
            PlaySound(m_NPCSounds.ScreamSound, m_NPCSoundsVolume.ScreamVolume);
        }
        else if (type == 2 && eventPlayAgonySound)
        {
            //Agony
            PlaySound(m_NPCSounds.AgonizeSound, m_NPCSoundsVolume.AgonizeVolume);
        }
        else if (type == 3 &&eventPlayEatSound)
        {
            //Eat
            PlaySound(m_NPCSounds.EatingSound, m_NPCSoundsVolume.EatingVolume);
        }
    }

    /// <summary>
    /// Is Agent Path Completed?
    /// </summary>
    bool PathCompleted()
    {
        if (agent.remainingDistance <= agent.stoppingDistance && agent.velocity.sqrMagnitude <= 0.1f && !agent.pathPending)
        {
            return true;
        }

        return false;
    }

    /// <summary>
    /// Main Search Function
    /// </summary>
    bool SearchForPlayer()
    {
        if (playerHealth.isDead || playerInvisible) return false;
        bool sightsResult = false;

        if (enableSights)
        {
            if (IsObjectVisible(sightsDistance, playerHead) && IsPlayerInSights(sightsFOV))
            {
                chaseTime = 0;
                sightsResult = true;
            }
        }
        else if(IsObjectVisible(sightsDistance, playerHead))
        {
            sightsResult = true;
        }
        else if (chaseTime < chaseTimeHide)
        {
            chaseTime += Time.deltaTime;
            sightsResult = true;
        }

        if (sightsResult)
        {
            lastChasePosition = playerObject.position;
            return true;
        }

        return false;
    }

    /// <summary>
    /// Is Object Visible in Distance?
    /// </summary>
    bool IsObjectVisible(float distance, Vector3 position)
    {
        if (Vector3.Distance(transform.position, position) <= distance)
        {
            return !Physics.Linecast(headPosition, position, searchMask, QueryTriggerInteraction.Collide);
        }

        return false;
    }

    /// <summary>
    /// Is Player in NPC Field of View?
    /// </summary>
    bool IsPlayerInSights(float FOV)
    {
        Vector3 playerDir = playerObject.position - transform.position;
        return Vector3.Angle(transform.forward, playerDir) <= FOV * 0.5;
    }

    /// <summary>
    /// Is Player in Distance?
    /// </summary>
    bool InDistance(float distance)
    {
        return DistanceOf(playerObject.position) <= distance;
    }

    /// <summary>
    /// Distance from transform to target
    /// </summary>
    float DistanceOf(Vector3 target)
    {
        return Vector3.Distance(transform.position, target);
    }

    /// <summary>
    /// Calculate NavMeshPath Length to target
    /// </summary>
    float CalculatePathLength(Vector3 targetPosition)
    {
        float pathLength = 0;
        NavMeshPath path = new NavMeshPath();
        agent.CalculatePath(targetPosition, path);

        Vector3[] allWayPoints = new Vector3[path.corners.Length + 2];
        allWayPoints[0] = transform.position;
        allWayPoints[allWayPoints.Length - 1] = targetPosition;

        for (int i = 0; i < path.corners.Length; i++)
        {
            allWayPoints[i + 1] = path.corners[i];
        }

        for (int i = 0; i < allWayPoints.Length - 1; i++)
        {
            pathLength += Vector3.Distance(allWayPoints[i], allWayPoints[i + 1]);
        }

        return pathLength;
    }

    /// <summary>
    /// Set Reachable Agent Destination
    /// </summary>
    void SetAgentDestination(Vector3? destination, bool isStopped = false)
    {
        if (destination.HasValue)
        {
            NavMeshPath path = new NavMeshPath();
            Vector3 dest = destination.Value;

            agent.isStopped = isStopped;

            if (agent.CalculatePath(dest, path))
            {
                agent.SetDestination(dest);
                lastCorrectDestination = dest;
            }
            else
            {
                agent.SetDestination(lastCorrectDestination);
            }
        }
        else
        {
            Debug.LogError("[Zombie AI] Could not set agent path destination!");
        }
    }

    /// <summary>
    /// Rotate Towards Target
    /// </summary>
    void RotateTowards(Vector3 target)
    {
        if (target != Vector3.zero || target != null)
        {
            //Make sure that agent.updateRotation is false
            agent.updateRotation = false;

            //Rotate transform towards target
            Vector3 direction = (target - transform.position).normalized;
            Quaternion lookRotation = Quaternion.LookRotation(new Vector3(direction.x, 0, direction.z));
            transform.rotation = Quaternion.SlerpUnclamped(transform.rotation, lookRotation, Time.deltaTime * agentRotationSpeed);
        }
    }

    void OnDrawGizmosSelected()
    {
        if (!gizmosEnabled) return;

        if (Application.isPlaying)
        {
            Vector3 dir = headPosition - playerHead;
            Gizmos.DrawRay(headPosition, -dir);
        }

        Vector3 pos = transform.position;
        pos += headOffset;
        Gizmos.color = Color.cyan;
        Gizmos.DrawWireSphere(pos, 0.05f);

        Vector3 leftRayDirection = Quaternion.AngleAxis(-(sightsFOV / 2), Vector3.up) * transform.forward;
        Vector3 rightRayDirection = Quaternion.AngleAxis(sightsFOV / 2, Vector3.up) * transform.forward;

        if (Application.isPlaying && playerObject)
        {
            Gizmos.color = IsPlayerInSights(sightsFOV) && IsObjectVisible(sightsDistance, playerHead) ? Color.green : Color.yellow;
        }
        else
        {
            Gizmos.color = Color.yellow;
        }

        Gizmos.DrawRay(transform.position, leftRayDirection * sightsDistance);
        Gizmos.DrawRay(transform.position, rightRayDirection * sightsDistance);

        Vector3 leftAttackRay = Quaternion.AngleAxis(-(attackFOV / 2), Vector3.up) * transform.forward;
        Vector3 rightAttackRay = Quaternion.AngleAxis(attackFOV / 2, Vector3.up) * transform.forward;
        Gizmos.color = Color.red;
        Gizmos.DrawRay(transform.position, leftAttackRay * attackDistance);
        Gizmos.DrawRay(transform.position, rightAttackRay * attackDistance);

        if (!soundReactionGizmos)
        {
            if (sleepBehaviour != IdleType.None)
            {
                Gizmos.color = Color.white;
                Gizmos.DrawWireSphere(transform.position, idleHearRange);
            }
        }
        else
        {
            Gizmos.color = Color.green;
            Gizmos.DrawWireSphere(transform.position, soundReactClose);

            Gizmos.color = Color.blue;
            Gizmos.DrawWireSphere(transform.position, soundReactFar);
        }
    }

    public Dictionary<string, object> OnSave()
    {
        return new Dictionary<string, object>()
        {
            { "position", transform.position },
            { "rotation_y", transform.eulerAngles.y },
            { "sleep_behaviour", sleepBehaviour },
            { "primary_behaviour", primaryBehaviour },
            { "secondary_behaviour", secondaryBehaviour },
            { "reaction_data", reaction },
            { "npc_dead", isDead },
            { "hunger_points", hungerPoints },
            { "agony_time", agonyTime },
            { "last_waypoint_pos", lastWaypointPos },
            { "last_chase_pos", lastChasePosition },
            { "can_continue", canContinue },
            { "can_scream", canScream }
        };
    }

    public void OnLoad(JToken token)
    {
        loadSaved = true;

        GetComponent<NavMeshAgent>().Warp(token["position"].ToObject<Vector3>());
        Vector3 rotation = transform.eulerAngles;
        rotation.y = (float)token["rotation_y"];
        transform.eulerAngles = rotation;

        sleepBehaviour = token["sleep_behaviour"].ToObject<IdleType>();

        reaction = token["reaction_data"].ToObject<Reaction>();
        hungerPoints = (float)token["hunger_points"];
        agonyTime = (float)token["agony_time"];

        lastWaypointPos = token["last_waypoint_pos"].ToObject<Vector3>();
        lastChasePosition = token["last_chase_pos"].ToObject<Vector3>();

        canContinue = token["can_continue"].ToObject<bool>();
        canScream = token["can_scream"].ToObject<bool>();

        if ((bool)token["npc_dead"])
        {
            isDead = true;
            gameObject.SetActive(false);
        }
    }
}