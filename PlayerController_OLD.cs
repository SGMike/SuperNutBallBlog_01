using UnityEngine;
using System.Collections;
using InControl;

public class PlayerController : MonoBehaviour {

    public int playerNum;

    private float               speed = 1500, speedSet, speedBoostSpeed, slowMod = 0.7f, speedBoostDuration = 3f;
    private float               jumpForce = 55, superJumpForce = 80, doubleJumpForce = 30, buttStompForce = -80, friction = 0.9f, frictionSet = 0.9f;
    private float               buttStompAttackForce = 90;
    private float               xDrag = 0.01f;
    private float               buttStompRotateSpeed = 55;
    private float               moveX, moveY;
    private bool                freezeVeloctiy;
    private bool                unfreeze;
    private bool                jumped;
    private bool                allowNegativeScaling;
    private bool                hitWithButtStomp;
    private bool                buttStompRequested;
    private bool                crouching;
    private bool                onWall;
    private bool                downBoostUsed;
    private bool                superJumpedRecently;
    public float                stunTimer, stunTimerSet = 0.3f;
    public bool                 grounded;
    public bool                 buttStomping;
    public bool                 facingRight;
    public bool                 doubleJumpEnabled, doubleJumped;
    public bool                 stunned;
    public bool                 speedBoostEnabled;
    
    
    public float                    currentForce;
    private Vector3                 normScale, leftScale;
    public Vector3                  forceDirection;
    
    private InputControl            control;
    private ParticleSystem          partSys;        
    private Color                   defaultColor;

    public Team team;
    public GameClasses.InputManager inputManager;
    public Attacker                 attacker;
    public AttackerTop              topAttacker;
    public ButtStompAttacker        buttStompAttacker;
    public NutGrabber               nutGrabber;
    public GameObject               squirrelGeo;
    public FXSpawner                fxSpawner;
    public Animator                 anim;
    public AudioSource              audioPlayer;
    public AudioClip[]              audioClips;    


	void Update() {
        if (GameManager.gameActive) {
            if (!stunned) {
                if (!nutGrabber.holdingNut) {
                    speed = speedSet;

                    if (speedBoostEnabled) 
                        speed = speedBoostSpeed;
                    
                } else {
                    speed = speedSet * slowMod;

                    if (speedBoostEnabled)
                        speed = speedBoostSpeed;
                }
                if (inputManager.requestCrouch && inputManager.requestAttack)
                    buttStompRequested = true;
                    
            }
        }

        if (!buttStomping && allowNegativeScaling) {
            if ((facingRight && inputManager.GetAxisX() < -0.2f) || (!facingRight && inputManager.GetAxisX() > 0.2f)) {
                Flip();
            }
        }
	}

	void FixedUpdate() {

        if (GameManager.gameActive) {

            GetGroundedInfo();

            if (!stunned) {
                if (inputManager.inputType == TypeOfInput.Controller) {
                    if (inputManager.GetAxisX() < -0.3f || inputManager.GetAxisX() > 0.3f) {
                        rigidbody.AddForce(Vector3.right * speed * inputManager.GetAxisX(), ForceMode.Acceleration);
                    }
                } else if (inputManager.inputType == TypeOfInput.Keyboard) {
                    rigidbody.AddForce(Vector3.right * speed * inputManager.GetAxisX(), ForceMode.Acceleration);
                }

                anim.SetFloat("xSpeed", Mathf.Abs(rigidbody.velocity.x));
                anim.SetFloat("ySpeed", Mathf.Abs(rigidbody.velocity.y));

                if (rigidbody.velocity.x > 5)
                    moveX = rigidbody.velocity.x * xDrag;

                if (rigidbody.velocity.x < 5) {
                    moveX = 0;
                }

                rigidbody.velocity = new Vector3(moveX, rigidbody.velocity.y, 0);

                if (partSys != null)
                    partSys.emissionRate = Mathf.Abs(rigidbody.velocity.x) * 33;

                //Only downboost on down TAP                
                if (!grounded && inputManager.requestCrouchFlick && !downBoostUsed && !superJumpedRecently) {
                    downBoostUsed = true;
                    inputManager.SetRequestCrouchFlick(false);
                    rigidbody.velocity = new Vector3(0, 0, 0);
                    rigidbody.AddForce(new Vector3(0, -30f, 0), ForceMode.Impulse);
                } else if (inputManager.requestCrouchFlick && grounded) {
                    inputManager.SetRequestCrouchFlick(false);
                }

                if (buttStompRequested) {
                    if (!grounded) {
                        //buttstomp
                        inputManager.SetVibration(0.3f, 0.3f);
                        buttStompAttacker.buttStompAttackActive = true;
                        allowNegativeScaling = false;
                        freezeVeloctiy = true;

                        audio.PlayOneShot(AudioLibrary.buttStompSwoosh01_);
                        anim.SetBool("ButtStomping", true);
                        Invoke("DisableButtStomping", 0.9f);

                        rigidbody.velocity = new Vector3(rigidbody.velocity.x, buttStompForce, 0);
                    }
                    buttStompRequested = false;
                }

                if (inputManager.requestCrouch) {
                    anim.SetBool("Crouching", true);

                    if (inputManager.requestJump) {
                        inputManager.requestJump = false;
                        if (grounded) {
                            //superjump
                            superJumpedRecently = true;
                            StartCoroutine(DisableSuperJumpedRecently(0.8f));
                            rigidbody.velocity = new Vector3(rigidbody.velocity.x, superJumpForce, rigidbody.velocity.z);
                            audioPlayer.PlayOneShot(AudioLibrary.jump03_);
                            anim.SetBool("Jumping", true);
                            Invoke("DisableJumpAnim", 0.4f);
                        }
                    }
                } else if (!inputManager.requestCrouch) {
                    anim.SetBool("Crouching", false);
                }

                if (inputManager.requestJump) {

                    if (grounded) {

                        //Only allow a double jump if a regular jump hasnt occured
                        jumped = true;
                        anim.SetBool("Jumping", true);

                        audioPlayer.PlayOneShot(AudioLibrary.jump01_);


                        //This is for the animation system
                        Invoke("DisableJumpAnim", 0.5f);
                        rigidbody.velocity += new Vector3(0, jumpForce, 0);
                    } else if (!grounded && !jumped && !doubleJumped) {

                        doubleJumped = true;

                        audioPlayer.PlayOneShot(AudioLibrary.jump02_);

                        fxSpawner.SpawnEffect(FXType.DoubleJump);
                        Vector3 resetYVelocity = rigidbody.velocity;
                        resetYVelocity.y = doubleJumpForce;
                        rigidbody.velocity = resetYVelocity;
                    } else if (onWall) {
                        onWall = false;
                        doubleJumped = false;
                        rigidbody.velocity = Vector3.zero;
                    }

                    inputManager.requestJump = false;
                }
                if (buttStomping) {
                    rigidbody.velocity = new Vector3(rigidbody.velocity.x, buttStompForce, rigidbody.velocity.z);
                    unfreeze = false;
                    buttStomping = false;
                }

                //for buttstomping
                if (freezeVeloctiy) {
                    rigidbody.velocity = Vector3.zero;

                    if (!unfreeze) {
                        Invoke("UnfreezeVelocity", 0.5f);
                        allowNegativeScaling = true;
                    }
                }
            }
        }
	}

    void OnTriggerEnter(Collider c) {
        if (c.tag == "Branch") {            
            if (c.gameObject.name.Contains("1")) {
                
            }
        }

        if (!TutorialController.tutorialInProgress) {
            
            if (c.tag == "Nut") {
                TutorialController.squirrelTouchedNut = true;
            } else if (c.tag == "Player") {
                TutorialController.squirrelTouchedPlayer = true;
            }
        }
    }    

    void OnCollisionEnter(Collision c) {
        if (buttStompAttacker.buttStompAttackActive  && c.gameObject.tag == "Ground") {
            fxSpawner.SpawnEffect(FXType.Buttstomp);

            float pitch = Random.Range(0.8f, 1.2f);
            audio.pitch = pitch;
            audio.PlayOneShot(AudioLibrary.buttStompHit01_);

            buttStompAttacker.buttStompAttackActive = false;
            inputManager.SetVibration(1f, 1f);
        }
    }

    void GetGroundedInfo() {
        Ray ray = new Ray(transform.position, Vector3.down);
        RaycastHit hit;
        if (Physics.Raycast(ray, out hit, 2f)) {
            grounded = true;
            downBoostUsed = false;
            doubleJumped = false;
            jumped = false;
        } else {
            grounded = false;
        }
    }

    void DisableJumpAnim() {
        anim.SetBool("Jumping", false);
    }

    void DisableButtStomping() {
        anim.SetBool("ButtStomping", false);
    }

    void DisableGettingHitAnimation() {
        anim.SetBool("GettingHit", false);
    }

    public void EnableSpeedBoost() {
        speedBoostEnabled = true;
        Invoke("DisableSpeedBoost", speedBoostDuration);
    }

    public void DisableSpeedBoost() {
        speedBoostEnabled = false;
    }

    public IEnumerator DisableSuperJumpedRecently(float time) {
        yield return new WaitForSeconds(time);
        superJumpedRecently = false;
        yield return null;
    }

    public void Stun(float length) {
        stunned = true;
        Invoke("UnStun", length);
        squirrelGeo.renderer.material.color = Color.red;

        inputManager.SetVibration(1, 1);
        anim.SetBool("GettingHit", true);
        Invoke("DisableGettingHitAnimation", 0.23f);

        //StartCoroutine(StunInAir());
        fxSpawner.SpawnEffect(FXType.Attack1);

        if (audioClips[0] != null) {
            audioPlayer.PlayOneShot(audioClips[0]);
        }
            
    }

    IEnumerator StunInAir() {
        rigidbody.useGravity = false;
        yield return new WaitForSeconds(0.05f);
        rigidbody.useGravity = true;
        yield return null;
    }

    public void UnStun() {
        squirrelGeo.renderer.material.color = defaultColor;
        stunned = false;
        anim.SetBool("GettingHit", false);
    }

    void Flip() {        
        if (facingRight)
            transform.localScale = leftScale;
        else
            transform.localScale = normScale;

        facingRight = !facingRight;
    }

    void UnfreezeVelocity() {
        buttStomping = true;
        transform.rotation = Quaternion.Euler(0, 0, 0);

        freezeVeloctiy = false;
        unfreeze = true;
    }

    public void AddForce(Vector3 direction, float force) {
        forceDirection = direction;
        currentForce = force;
        inputManager.requestAttack = true;
    }

	public PlayerController Init() {
        inputManager =                                              GetComponent<GameClasses.InputManager>();
        nutGrabber =                                                GetComponent<NutGrabber>();
        audioPlayer =       transform.FindChild("AudioPlayer").     GetComponent<AudioSource>();
        buttStompAttacker = transform.Find("ButtStompAttacker").    GetComponent<ButtStompAttacker>();
        fxSpawner =         GameObject.Find("FXSpawner_" + playerNum).GetComponent<FXSpawner>();        

        if (transform.FindChild("AttackBox")) {
            attacker = transform.FindChild("AttackBox").GetComponent<Attacker>();
            topAttacker = transform.FindChild("AttackBox_Top").GetComponent<AttackerTop>();
        } else {
            Debug.LogWarning("Attacker Not Found!");
        }

        if (transform.FindChild("Particle System")) {
            partSys = transform.FindChild("Particle System").GetComponent<ParticleSystem>();
        } else {
            Debug.LogWarning("Particle System Not Found!");
        }

        if (anim == null) {
            Debug.LogError("Link animator");
        }

        speedSet = speed;
        speedBoostSpeed = speed * 1.6f;
        normScale = transform.localScale;
        leftScale = new Vector3(-normScale.x, normScale.y, normScale.z);

        facingRight = true;
        allowNegativeScaling = true;
        defaultColor = squirrelGeo.renderer.material.color;

        return this;
	}
}