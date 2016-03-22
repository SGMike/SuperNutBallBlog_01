using UnityEngine;
using System.Collections;
using InControl;

public class PlayerController : MonoBehaviour
{
    public delegate void PlayerEvent(PlayerController playerController);
    public event PlayerEvent OnHit;

    //TODO: Give players playerData
    private int m_playerNumber;

    [SerializeField]
    private float m_speed = 80, m_slowMod = 0.7f;
    private float superJumpForce = 80, buttStompForce = -160;
    private float m_recentlyStunnedTimer = 0;
    private float m_buttStompStartHeight;
    private bool m_dashCooledDown = true;
    private bool allowNegativeScaling;
    private bool hitWithButtStomp;
    private bool m_downBoostAvailable;
    private bool m_superJumpedRecently;
    private bool m_buttStomping;
    private bool m_buttStompInitiated;
    private bool m_stunned;
    private bool m_speedBoostEnabled;
    private bool m_blasting;
    private bool m_dashAttackActive;
    private bool m_recentlyStunned = false;
    private bool m_longJumpedMidAir = false;

    private Vector2 m_controlAxes;
    private Vector3 m_aimVector;

    private InputControl control;
    [SerializeField]    private ParticleSystem m_particleSystem_DustTrail;
    [SerializeField]    private ParticleSystem m_particleSystem_GlideTrail; //TODO: Write a null check and get with string in Init()
    private Color m_defaultColor;
    private UIManager m_uiManager;

    private Team m_team;
    private InputController m_inputController;

    [SerializeField] private MovementController m_movementController;
    [SerializeField] private Attacker m_attacker;
    [SerializeField] private AttackerTop m_attackerTop;
    [SerializeField] private ButtStompAttacker m_buttStompAttacker;
    [SerializeField] private NutGrabber m_nutGrabber;
    [SerializeField] private AudioSource m_audioPlayer;
    [SerializeField] private Rigidbody m_rigidbody;
    [SerializeField] private SquirrelSqueaker m_squeaker;
    [SerializeField] private FXSpawner m_fxSpawnerPrefab;
    private FXSpawner m_fxSpawner;
    private Renderer m_renderer;
    private Vector3 m_startingPlayerPosition;
    private CapsuleCollider m_collider;
    

    public Animator m_animator;
    public GameObject squirrelGeo;
    public AudioClip[] m_playerHurtAudioClips;

	//---------------------------------------------------------------------------------- -
	//  Getters and Setters
	//---------------------------------------------------------------------------------- -
    public void SetTeam(Team team) { m_team = team; }
    public Team GetTeam() { return m_team; }
    public void SetPlayerNumber(int number) { m_playerNumber = number; }

    public int GetPlayerNumber() { return m_playerNumber; }
    public NutGrabber GetNutGrabber() { return m_nutGrabber; }
    public ButtStompAttacker GetButtStompAttacker() { return m_buttStompAttacker; }
    public AudioSource GetAudioSource() { return m_audioPlayer; }
    public Rigidbody GetRigidbody() { return m_rigidbody; }
    public bool PlayerIsCrouching() { return m_movementController.IsCrouching(); }
    public bool IsGrounded() { return m_movementController.IsGrounded(); }
    public bool IsStunned() { return m_stunned; }
    public bool IsRecentlyStunned() { return m_recentlyStunned; }

    //---------------------------------------------------------------------------------- -
    //  Initialization Function
    //---------------------------------------------------------------------------------- -
    public PlayerController Init(UIManager uiManager)
    {
        //  Registering for events  //
        GameManager.OnNutScored += MaxOutControllerVibration;

        if (m_movementController == null)
            m_movementController = GetComponent<MovementController>();

        if (m_inputController == null)
            m_inputController = GetComponent<InputController>();

        if (m_nutGrabber == null)
            m_nutGrabber = GetComponent<NutGrabber>();

        if (m_audioPlayer == null)
            m_audioPlayer = transform.Find("AudioPlayer").GetComponent<AudioSource>();

        if (m_buttStompAttacker == null)
            m_buttStompAttacker = transform.Find("ButtStompAttacker").GetComponent<ButtStompAttacker>();

        if (m_rigidbody == null)
            m_rigidbody = GetComponent<Rigidbody>();

        if (m_renderer == null)
            m_renderer = squirrelGeo.GetComponent<Renderer>();

        if (m_squeaker == null)
            m_squeaker = GetComponent<SquirrelSqueaker>();

        if (m_collider == null)
            m_collider = GetComponent<CapsuleCollider>();

        m_fxSpawner = Instantiate(m_fxSpawnerPrefab).GetComponent<FXSpawner>();
        //m_fxSpawner = GameObject.Find("FXSpawner_" + m_playerNumber).GetComponent<FXSpawner>();

        if (squirrelGeo == null)
            Debug.LogError("Player geometry is not assigned, assign it before continuing.");

        m_uiManager = uiManager;

        if (transform.FindChild("AttackBox"))
        {
            m_attacker = transform.FindChild("AttackBox").GetComponent<Attacker>();
            m_attackerTop = transform.FindChild("AttackBox_Top").GetComponent<AttackerTop>();
        }
        else
        {
            Debug.LogWarning("Attacker Not Found!");
        }

        if (transform.FindChild("Particle System"))
        {
            m_particleSystem_DustTrail = transform.FindChild("Particle System").GetComponent<ParticleSystem>();
        }
        else
        {
            Debug.LogWarning("Particle System Not Found!");
        }

        if (m_animator == null)
        {
            Debug.LogError("Link animator");
        }


        m_controlAxes = new Vector2(0, 0);

        allowNegativeScaling = true;

        m_defaultColor = m_renderer.material.color;

        //  Create a UI indicator for this player  //
        m_uiManager.SpawnPlayerMarker(gameObject);

        InitializeComponents();

        //  Log the starting player position so they can respawn if needed  //
        m_startingPlayerPosition = transform.position;

        //  Registering for player events  //
        m_nutGrabber.OnNutGrabberGrabbed += OnNutGrabbed;
        m_nutGrabber.OnNutGrabberReleased += OnNutReleased;
        m_movementController.OnGrounded += OnGrounded;
        m_movementController.OnJump += OnJump;
        m_movementController.OnDoubleJump += OnDoubleJump;

        return this;
    }

    //---------------------------------------------------------------------------------- -
    //  On Disable
    //---------------------------------------------------------------------------------- -
    private void OnDisable()
    {
        //  Unregistering for events  //
        GameManager.OnNutScored -= MaxOutControllerVibration;

        //  Unregistering for player events  //
        m_nutGrabber.OnNutGrabberGrabbed -= OnNutGrabbed;
        m_nutGrabber.OnNutGrabberReleased -= OnNutReleased;
        m_movementController.OnGrounded -= OnGrounded;
        m_movementController.OnJump -= OnJump;
        m_movementController.OnDoubleJump -= OnDoubleJump;
    }

    //---------------------------------------------------------------------------------- -
    //  Initialize Components Function
    //---------------------------------------------------------------------------------- -
    private void InitializeComponents()
    {
        m_inputController.Init(this);

        m_movementController.Init(m_rigidbody, m_animator);
        m_attacker.Init(this, m_inputController, m_nutGrabber, m_animator);
        m_buttStompAttacker.Init(m_inputController);
        m_attackerTop.Init(this, m_inputController, m_animator);
        m_nutGrabber.Init(this, m_inputController, m_movementController, m_animator);
        m_squeaker.Init(this);
        GetComponent<FootstepController>().Init(this, m_animator);
        m_fxSpawner.Init(gameObject);
    }

    //---------------------------------------------------------------------------------- -
    //  Set Controller Vibration Function
    //---------------------------------------------------------------------------------- -
    public void SetControllerVibration(float L, float R)
    {
        m_inputController.SetVibration(L, R);
    }

    //---------------------------------------------------------------------------------- -
    //  Max Out Controller Vibration Function
    //      -Used at end of game to keep controller vibrating, doesn't really work right
    //---------------------------------------------------------------------------------- -
    private void MaxOutControllerVibration(Team team = Team.None)
    {
        SetControllerVibration(24f, 24f);
    }

    //---------------------------------------------------------------------------------- -
    //  Fixed Update
    //---------------------------------------------------------------------------------- -
    void FixedUpdate()
    {
        //  Update Particles  //
        UpdateParticles();

        //  Update Movement  //
        MoveHorizontal(m_controlAxes.x);

        //  Update Recently Stunned Timer  //
        UpdateRecentlyStunnedTimer();
    }

    //---------------------------------------------------------------------------------- -
    //  Update Recently Stunned Timer
    //---------------------------------------------------------------------------------- -
    private void UpdateRecentlyStunnedTimer()
    {
        if (m_recentlyStunnedTimer > 0)
        {
            m_recentlyStunnedTimer -= Time.fixedDeltaTime;
        }

        if (m_recentlyStunnedTimer <= 0 && m_recentlyStunned)
        {
            m_recentlyStunned = false;
        }
    }

    //---------------------------------------------------------------------------------- -
    //  Reset Player Position
    //      -This is for debugging use, if a player gets stuck they can reset with this
    //---------------------------------------------------------------------------------- -
    public void ResetPlayerPosition()
    {
        transform.position = m_startingPlayerPosition;
    }

    //---------------------------------------------------------------------------------- -
    //  Update Particles Function
    //---------------------------------------------------------------------------------- -
    private void UpdateParticles()
    {
        //************************************************************************************
          //  Dust trail  
            ParticleSystem.EmissionModule emissionModule = m_particleSystem_DustTrail.emission;
            ParticleSystem.MinMaxCurve emissionRate = emissionModule.rate;
            float emissionValue = Mathf.Abs(m_rigidbody.velocity.x) * 0.5f;
            float deviation = 3f;

            if (emissionValue < 5f || !m_movementController.IsGrounded())
            {
                emissionValue = 0;
                deviation = 0;
            }

            emissionRate.constantMax = emissionValue + deviation;
            emissionRate.constantMin = emissionValue - deviation;

            emissionModule.rate = emissionRate;
        
    }

    //---------------------------------------------------------------------------------- -
    //  Set Control Axes Function
    //---------------------------------------------------------------------------------- -
    public void SetControlAxes(Vector2 value)
    {
        m_controlAxes = value;

        float degrees = 0;
        degrees = Mathf.Atan2(value.y, value.x) * Mathf.Rad2Deg;
    }

    private void MoveHorizontal(float axis)
    {
        if (!allowNegativeScaling)
            return;

        if (m_stunned)
            return;

        m_movementController.MoveHorizontal(axis);
    }

    //---------------------------------------------------------------------------------- -
    //  On Nut Grabbed Function
    //---------------------------------------------------------------------------------- -
    private void OnNutGrabbed(NutGrabber nutGrabber)
    {
        m_movementController.SetSpeed(m_speed * m_slowMod);

        m_fxSpawner.SpawnEffect(FXType.GrabNut, true);
    }

    //---------------------------------------------------------------------------------- -
    //  On Nut Released Function
    //---------------------------------------------------------------------------------- -
    private void OnNutReleased(NutGrabber nutGrabber)
    {
        m_movementController.SetSpeed(m_speed);
    }

    //---------------------------------------------------------------------------------- -
    //  Down Boost Function  //TODO: COMMAND PATTERN
    //---------------------------------------------------------------------------------- -
    public void DownBoost()
    {
        if (m_movementController.IsGrounded() || !m_downBoostAvailable || m_superJumpedRecently)
            return;

        m_downBoostAvailable = false;
        m_rigidbody.velocity = new Vector3(m_rigidbody.velocity.x, 0, 0);
        m_rigidbody.AddForce(new Vector3(0, -30f, 0), ForceMode.Impulse);
    }

    //---------------------------------------------------------------------------------- -
    //  On Collision Enter
    //---------------------------------------------------------------------------------- -
    void OnCollisionEnter(Collision c)
    {
        if (c.gameObject.tag == "Ground")
        {
            if (m_buttStomping)
            {
                DisableButtStomp();
            }
        }

        if (c.gameObject.tag == "Player")
        {
            //  if Dashing  //
            if (m_dashAttackActive)
            {
                HandleDashCollision(c);
            }
        }

        if (c.gameObject.tag == "Nut")
        {
            HandleNutCollision(c);
        }

        if (c.gameObject.tag == "Hittable")
        {
            if (m_dashAttackActive)
            {
                HandleHittableCollision(c);
            }
        }
    }

    //-------------------------------------------
    //  Dash Collision  //
    private void HandleDashCollision(Collision c)
    {
        PlayerController otherPlayerController = c.gameObject.GetComponent<PlayerController>();

        if (otherPlayerController.IsRecentlyStunned())
            return;

        //  Play audio  //
        m_audioPlayer.PlayOneShot(ApplicationManager.Get().GetAudioLibrary().hit01);

        otherPlayerController.Stun(0.4f, Vector3.up * 2.0f + m_rigidbody.velocity * 2.0f);
    }

    //-------------------------------------------            
    //  Hittable Collision  //
    private void HandleHittableCollision(Collision c)
    {
        //  Play audio  //
        m_audioPlayer.PlayOneShot(ApplicationManager.Get().GetAudioLibrary().hit01);

        //  Create a force  //
        const float k_attackForce = 10.0F;
        Vector3 force = ((-1 * Vector3.Normalize(transform.parent.position - c.transform.position)) + Vector3.up) * k_attackForce;
        c.transform.GetComponent<Rigidbody>().AddForce(force);
    }

    //-------------------------------------------
    //  Nut Collision
    private void HandleNutCollision(Collision c)
    {
        //  Dont deal damage if dashing //
        if (m_dashAttackActive)
        {
            float forceFactor = 35.0F;
            c.gameObject.GetComponent<NutController>().AddForce(Vector3.up * forceFactor + m_rigidbody.velocity * forceFactor);
            m_audioPlayer.PlayOneShot(AudioLibrary.Get().buttStompHit01);
            return;
        }

        float fixedCollisionMagnitude = c.impulse.magnitude + m_rigidbody.velocity.magnitude * 4f;
        float nutVelocityThreshold = 10.0f;
        float collisionForce = 100f;
        float collisionThreshold = 68f;
        float nutStunTime = 0.3f;

        //  If the nut's velocity is below nutVelocityThreshold then it will not do damamge  //
        if (fixedCollisionMagnitude < collisionThreshold || c.rigidbody.velocity.magnitude < nutVelocityThreshold)
            return;

        //  Apply an upward force if grounded  //
        if (IsGrounded())
            m_rigidbody.AddForce(Vector3.up * collisionForce);

        // TODO: Apply proper force
        Stun(nutStunTime, Vector3.zero);
        m_rigidbody.AddForce((-1 * Vector3.Normalize(transform.position - (transform.position - Vector3.down))) * collisionForce * m_rigidbody.velocity.magnitude);
        m_audioPlayer.PlayOneShot(AudioLibrary.Get().buttStompHit01);
    }

    //---------------------------------------------------------------------------------- -
    //  Set Crouching State Function
    //---------------------------------------------------------------------------------- -
    public void SetCrouchingState(bool value)
    {
        m_movementController.SetCrouchingState(value);

        //  On Crouch  //
        if (m_movementController.IsCrouching())
        {
            m_collider.height = 1.0f;
            m_collider.center = Vector3.zero;

            m_movementController.SetSpeed(m_speed * m_slowMod);
        }

        //  On Crouch Disable  //
        if (!m_movementController.IsCrouching())
        {
            m_collider.height = 1.6f;
            m_collider.center = new Vector3(0, 0.24f, 0);

            m_movementController.SetSpeed(m_speed);

            //  If holding the nut, then revert to slow speed  //
            if (m_nutGrabber.IsHoldingNut())
                m_movementController.SetSpeed(m_speed * m_slowMod);
        }
    }

    //---------------------------------------------------------------------------------- -
    //  Dash Function
    //---------------------------------------------------------------------------------- -
    public void Dash(Vector2 axis)
    {
        if (m_nutGrabber.IsHoldingNut() || !m_dashCooledDown)
            return;

        //  Do an up attack initially  //
        m_attackerTop.Attack();

        float dashForce = 50f;

        m_rigidbody.velocity = Vector3.zero;

        Vector2 unitVector = axis.normalized;

        m_rigidbody.AddForce(unitVector * dashForce, ForceMode.Impulse);

        m_dashCooledDown = false;

        //  Play Animation  //
        m_animator.SetTrigger("Attacking2");

        m_dashAttackActive = true;

        StartCoroutine(RunDashCooldown(0.8f));
    }

    private IEnumerator RunDashCooldown(float time)
    {
        yield return new WaitForSeconds(time);
        m_dashCooledDown = true;
        m_dashAttackActive = false;
    }

    //---------------------------------------------------------------------------------- -
    //  Butt Stomp Function
    //---------------------------------------------------------------------------------- -
    private IEnumerator ButtStomp()
    {
        if (m_buttStompInitiated || m_buttStomping)
            yield break;

        m_buttStompInitiated = true;

        //  Set Controller Vibration  //
        SetControllerVibration(0.3f, 0.3f);

        //  Log the height  //
        m_buttStompStartHeight = transform.position.y;

        m_buttStompAttacker.EnableAttack();
        allowNegativeScaling = false;
        //m_freezeVelocity = true;

        m_audioPlayer.PlayOneShot(AudioLibrary.Get().buttStompSwoosh01);

        //  Play Animation  //
        m_animator.SetTrigger("ButtstompStart");
        m_animator.SetBool("ButtStomping", true);

        //  FREEZE  //
        float freezeTimer = 0.5f;

        while (freezeTimer > 0)
        {
            //  Set Velocity to zero  //
            Vector3 newVelocity = new Vector3(m_rigidbody.velocity.x * 0.95f, 0, 0);
            //m_rigidbody.velocity = Vector3.zero;
            m_rigidbody.velocity = newVelocity;

            //  Iterate Timer  //
            freezeTimer -= Time.deltaTime;
            yield return new WaitForEndOfFrame();
        }

        //  Mark that we're in this action  //
        m_buttStomping = true;

        while (m_buttStomping)
        {
            //  Apply a downward force  //
            //m_rigidbody.velocity = new Vector3(m_rigidbody.velocity.x, buttStompForce, m_rigidbody.velocity.z);
            m_rigidbody.velocity = new Vector3(0, buttStompForce, 0);
            yield return new WaitForEndOfFrame();
        }
    }

    //---------------------------------------------------------------------------------- -
    private void DisableButtStomp()
    {
        //  Calculate the distance the buttstomp went  //
        float buttStompDistance = m_buttStompStartHeight - transform.position.y;

        //  Spawn FX  //
        SpawnEffect(FXType.Buttstomp);

        //  Play audio  //
        m_audioPlayer.pitch = Random.Range(0.8f, 1.2f);
        m_audioPlayer.PlayOneShot(AudioLibrary.Get().buttStompHit01);

        //  Disable the attack from doing damage  //
        m_buttStompAttacker.DisableAttack();

        //  Apply Controller Vibration  //
        SetControllerVibration(1f, 1f);

        //  Disable Animation  //
        m_animator.SetTrigger("ButtstompEnd");
        m_animator.SetBool("ButtStomping", false);
        allowNegativeScaling = true;

        //  Apply a recoil  //
        float recoilForce = 40f;
        recoilForce += buttStompDistance * 1f;          //TODO: Figure out a good math function for this
        m_rigidbody.AddForce(Vector3.up * recoilForce, ForceMode.Impulse);

        //  Apply a speed boost  //
        //EnableSpeedBoost(buttStompDistance / 60f);    //TODO: Figure out a good math function for this
        EnableSpeedBoost(0.1f);

        //  Apply gameplay bools  //
        m_movementController.SetGrounded(true);
        m_downBoostAvailable = true;

        //  Mark the completion of the action  //
        m_buttStomping = false;
        m_buttStompInitiated = false;
    }

    //---------------------------------------------------------------------------------- -
    //  Attack Function
    //---------------------------------------------------------------------------------- -
    public void Attack(Vector2 axes)
    {
        if (m_stunned)
            return;


        //  Experimental Unnamed thing currently called Blast  //
        if (!m_movementController.IsGrounded() && (axes.y < 0.4f && axes.y > -0.4f))
        {
            //  If moving in the direction you're facing  //
            if ((m_movementController.IsFacingRight() && axes.x > 0.3f) || (!m_movementController.IsFacingRight() && axes.x < -0.3f))
            {
                //Blast(); 
                return;
            }
        }

        //  Buttstomp  //
        if (axes.y < -0.4f && !m_movementController.IsGrounded())
        {
            //buttStompRequested = true;
            StartCoroutine(ButtStomp());
            return;
        }

        if (m_nutGrabber.IsHoldingNut())
            return;

        //  Upward Attack  //
        if (axes.y > 0.4f)
        {
            m_attackerTop.Attack();
            return;
        }

        //  Standard Attack  //
        if (axes.y < 0.2f && !m_movementController.IsCrouching())
        {
            m_attacker.Attack();
            return;
        }

    }

    //---------------------------------------------------------------------------------- -
    //  Throw Function
    //---------------------------------------------------------------------------------- -
    public void Throw(Vector2 axes)
    {
        //  Throw Nut  //
        if (m_nutGrabber.IsHoldingNut())
        {
            m_nutGrabber.ThrowNut(axes);
            return;
        }
    }

    //---------------------------------------------------------------------------------- -
    //  Blast Function
    //---------------------------------------------------------------------------------- -
    public void Blast()
    {
        if (m_blasting)
            return;

        const float k_blastPower = 47f;

        float xAxis = 1.0f;

        if (!m_movementController.IsFacingRight())
            xAxis *= -1;

        m_rigidbody.AddForce(new Vector3(xAxis, -1.0f, 0.0f) * k_blastPower, ForceMode.Impulse);

        m_blasting = true;
    }

    //---------------------------------------------------------------------------------- -
    //  Jump Function
    //---------------------------------------------------------------------------------- -
    public void Jump()
    {
        //  Stunned  //
        if (m_stunned)
            return;

        //  Enable Gliding  //
        if (!m_nutGrabber.IsHoldingNut())//  Do not allow a player holding the nut to glide  //
        {
            m_movementController.EnableGlide();
        }

        //  SuperJump  //
        if (m_movementController.IsGrounded() && (m_movementController.IsCrouching() || m_controlAxes.y > 0.6f))
        {
            SuperJump();

            return;
        }

        m_movementController.Jump();
    }

    //---------------------------------------------------------------------------------- -
    //  Release Glide
    //---------------------------------------------------------------------------------- -
    public void ReleaseGlide()
    {
        m_movementController.DisableGlide();
    }

    //---------------------------------------------------------------------------------- -
    //  On Jump Event Reaction
    //---------------------------------------------------------------------------------- -
    private void OnJump()
    {
        //TODO: Fix bools to triggers
        m_animator.SetBool("Jumping", true);

        m_audioPlayer.PlayOneShot(AudioLibrary.Get().jump01);

        //This is for the animation system
        Invoke("DisableJumpAnim", 0.5f);
    }

    //---------------------------------------------------------------------------------- -
    //  On Double Jump Event Reaction
    //---------------------------------------------------------------------------------- -
    private void OnDoubleJump()
    {
        m_audioPlayer.PlayOneShot(AudioLibrary.Get().jump02);

        SpawnEffect(FXType.DoubleJump);
    }

    //---------------------------------------------------------------------------------- -
    //  Long Jump Function
    //---------------------------------------------------------------------------------- -
    public void LongJump(Vector2 axes)
    {
        if (m_longJumpedMidAir)
            return;

        if (m_nutGrabber.IsHoldingNut())
            return;

        //  Declare vector  //
        Vector3 resultantForce = new Vector3(0, 0, 0);

        //  Differentiate logic between on the ground and in the air
        if (m_movementController.IsGrounded()) // if grounded, jump up
        {
            //  Jump upwards
            resultantForce.x = 0.5f;
            resultantForce.y = 0.5f;
        }
        else if (!m_movementController.IsGrounded()) // if in the air
        {
            //  Can't do it if holding the nut  //
            if (m_nutGrabber.IsHoldingNut())
                return;

            //  Dont allow the player to spam unless they hit the ground again  //
            m_longJumpedMidAir = true;

            //  Add extra force for speed  //
            resultantForce.x = 0.8f;

            //  Disable gravity temporarily  //
            Vector3 newVeloctity = m_rigidbody.velocity;
            newVeloctity.y = 0;
            m_rigidbody.velocity = newVeloctity;

            StartCoroutine(ShiftInAir());
        }

        //  Determine direction  //
        if (axes.x < -0.9f)
            resultantForce.x *= -1.0f;

        //  Apply the force!  //
        m_rigidbody.AddForce(resultantForce * superJumpForce * 1f, ForceMode.VelocityChange);

        //  Change facing direction  //
        m_movementController.MoveHorizontal(axes.x);

        //  Play Audio  //
        m_audioPlayer.PlayOneShot(AudioLibrary.Get().jump03);

        //  Play Animation  //
        m_animator.SetTrigger("LongJump");
        //m_animator.SetBool("Jumping", true);

        //  Disable constant jumps  // 
        m_superJumpedRecently = true;

        //  Mark super jumped recently  //
        StartCoroutine(DisableSuperJumpedRecently(0.8f));

        //  Disable Animation  //
        Invoke("DisableJumpAnim", 0.4f);
    }

    private IEnumerator ShiftInAir()
    {
        //  Disable gravity  //
        m_rigidbody.useGravity = false;

        //  Determine how long to dash before cooling down  //
        const float shiftTime = 0.2f;
        yield return new WaitForSeconds(shiftTime);

        //  Reduce the velocity  //
        m_rigidbody.velocity *= 0.3f;

        //  Re-enable gravity  //
        m_rigidbody.useGravity = true;
    }

    //---------------------------------------------------------------------------------- -
    //  Super Jump Function
    //---------------------------------------------------------------------------------- -
    public void SuperJump()
    {
        // SuperJump // 
        m_superJumpedRecently = true;

        //  Mark super jumped recently  //
        StartCoroutine(DisableSuperJumpedRecently(0.8f));

        //  Add the force  //
        m_rigidbody.AddForce(Vector3.up * superJumpForce, ForceMode.VelocityChange);

        //  Play Audio  //
        m_audioPlayer.PlayOneShot(AudioLibrary.Get().jump03);

        //  Play Animation  //
        m_animator.SetBool("Jumping", true);

        //  Disable Animation  //
        Invoke("DisableJumpAnim", 0.4f);
    }

    //---------------------------------------------------------------------------------- -
    //  Grab Function
    //---------------------------------------------------------------------------------- -
    public void Grab(bool value)
    {
        m_nutGrabber.Grab(value);

    }

    //---------------------------------------------------------------------------------- -
    //  Squeak Function
    //---------------------------------------------------------------------------------- -
    public void Squeak()
    {
        m_squeaker.Squeak();
    }

    //---------------------------------------------------------------------------------- -
    //  Update Grounded State Function
    //---------------------------------------------------------------------------------- -
    void UpdateGroundedState()
    {
        Ray ray = new Ray(transform.position, Vector3.down);
        RaycastHit hit;

        if (Physics.Raycast(ray, out hit, 2f))
        {
            m_movementController.SetGrounded(true);
            m_downBoostAvailable = true;
        }
        else
        {
            m_movementController.SetGrounded(false);
        }
    }
    //---------------------------------------------------------------------------------- -
    void DisableJumpAnim()
    {
        m_animator.SetBool("Jumping", false);
    }
    //---------------------------------------------------------------------------------- -
    void DisableGettingHitAnimation()
    {
        m_animator.SetBool("GettingHit", false);
    }
    //---------------------------------------------------------------------------------- -
    public void SpawnEffect(FXType fxType)
    {
        m_fxSpawner.SpawnEffect(fxType);
    }

    //---------------------------------------------------------------------------------- -
    //  Enable Speed Boost Function
    //---------------------------------------------------------------------------------- -
    public void EnableSpeedBoost(float duration)
    {
        m_speedBoostEnabled = true;

        //  Play Audio  //
        /*
        if (audioClips[0] != null)
        {
            m_audioPlayer.PlayOneShot(audioClips[0]);
        }
        */

        //  Spawn Effects  //
        SpawnEffect(FXType.Buttstomp);

        Invoke("DisableSpeedBoost", duration);
    }

    //---------------------------------------------------------------------------------- -
    //  Disable Speed Boost Function
    //---------------------------------------------------------------------------------- -
    public void DisableSpeedBoost()
    {
        m_speedBoostEnabled = false;
    }

    //---------------------------------------------------------------------------------- -
    //  Disable Super Jumped Recently Function
    //---------------------------------------------------------------------------------- -
    private IEnumerator DisableSuperJumpedRecently(float time)
    {
        yield return new WaitForSeconds(time);
        m_superJumpedRecently = false;
        yield return null;
    }

    //---------------------------------------------------------------------------------- -
    //  Stun Function
    //---------------------------------------------------------------------------------- -
    public void Stun(float length, Vector3 force)
    {
        if (m_recentlyStunned)
            return;

        //  Apply stun bool  //
        m_stunned = true;
        Invoke("UnStun", length);

        //  Set recently stunned timer  //
        m_recentlyStunnedTimer = 0.5f;
        m_recentlyStunned = true;

        //  Apply Force //
        if (force != Vector3.zero)
        {
            m_rigidbody.AddForce(force, ForceMode.Impulse);
        }

        //  Change Color  //
        m_renderer.material.color = Color.red;

        //  Disable Nut Grab  //
        m_nutGrabber.DisableGrab();

        //  Set vibration  //
        m_inputController.SetVibration(1, 1);

        //  Animate  //
        m_animator.SetBool("GettingHit", true);
        Invoke("DisableGettingHitAnimation", 0.23f);

        //  Spawn Particle  //
        SpawnEffect(FXType.Attack1);

        //  Play Audio  //
        m_audioPlayer.PlayOneShot(m_playerHurtAudioClips[0]);

        //  Fire Event  //
        OnHit(this);
    }

    IEnumerator StunInAir()
    {
        m_rigidbody.useGravity = false;
        yield return new WaitForSeconds(0.05f);
        m_rigidbody.useGravity = true;
        yield return null;
    }

    public void UnStun()
    {
        m_renderer.material.color = m_defaultColor;

        m_stunned = false;
        m_animator.SetBool("GettingHit", false);
    }

    private void OnGrounded()
    {
        if (m_blasting)
        {
            RunBlastReaction();
        }

        m_longJumpedMidAir = false;
    }

    private void RunBlastReaction()
    {
        Vector3 newVelocity = m_rigidbody.velocity;
        newVelocity.x *= 0.3f;
        newVelocity.y *= -0.3f;
        m_rigidbody.velocity = newVelocity;

        m_blasting = false;
    }
}