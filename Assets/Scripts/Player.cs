using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Photon.Pun;
using UnityEngine.UI;
using System;
using TMPro;

public class Player : MonoBehaviourPunCallbacks, IPunObservable
{
    #region Variables
    public float speed;
    public float sprintModifier;
    public float crouchModifier;
    public float jumpForce;
    public int maxHealth;
    public int currentHealth;
    public Camera normalCam;
    public GameObject cameraParent;
    public Transform weaponParent;
    public Transform groundDetector;
    public LayerMask ground;
    public float lengthOfSlide;
    public float slideModifier;
    public float crouchAmount;
    public float slideAmount;
    public GameObject standingCollider;
    public GameObject standingColliderHead;
    public GameObject crouchingCollider;
    public GameObject crouchingColliderHead;
    public GameObject deadPrefab;

    public ProfileData playerProfile;
    public TextMeshPro playerUsername;

    private Transform uiHealthBar;
    private Text uiAmmo;
    private Text uiUsername;

    private Rigidbody rig;
    private float baseFOV;
    private float sprintFOVModifier = 1.25f;

    private Vector3 weaponParentCurrentPosition;
    private Vector3 weaponParentorigin;
    private Vector3 targetWeaponBobPosition;

    private float movementCounter;
    private float idleCounter;
    private Manager manager;
    private Weapon weapon;

    private bool isDead;

    private bool crouched;
    private bool sliding;
    private float slideTime;
    private Vector3 slideDirection;

    private Vector3 origin;

    private float aimAngle;



    #endregion

    #region Photon Callbacks
    public void OnPhotonSerializeView(PhotonStream stream, PhotonMessageInfo info)
    {
        if (stream.IsWriting)
        {
            stream.SendNext((int)weaponParent.transform.localEulerAngles.x * 100f);
            stream.SendNext(currentHealth);
        }
        else
        {
            aimAngle = (float)stream.ReceiveNext() / 100f;
            currentHealth = (int)stream.ReceiveNext();
        }
    }
    #endregion


    #region MonoBehaviour Callbacks
    private void Start()
    {
        manager = GameObject.Find("Manager").GetComponent<Manager>();
        weapon = GetComponent<Weapon>();

        currentHealth = maxHealth;
            cameraParent.SetActive(photonView.IsMine);

        if (!photonView.IsMine)
        {
            gameObject.layer = 8;
            standingCollider.layer = 8;
            standingColliderHead.layer = 8;
            crouchingCollider.layer = 8;
            crouchingColliderHead.layer = 8;
        }
        else
        {
            int playerLayer = LayerMask.NameToLayer("LocalPlayer");
            normalCam.cullingMask &= ~(1 << playerLayer);
        }

            weaponParentorigin = weaponParent.localPosition;
        weaponParentCurrentPosition = weaponParentorigin;
        baseFOV = normalCam.fieldOfView;
        origin = normalCam.transform.localPosition;

        // if (Camera.main)Camera.main.enabled = false;

        rig = GetComponent<Rigidbody>();

        if (photonView.IsMine)
        {
            uiHealthBar =  GameObject.Find("HUD/Health/Bar").transform;
            uiAmmo = GameObject.Find("HUD/Ammo/Text").GetComponent<Text>();
            uiUsername = GameObject.Find("HUD/Username/Text").GetComponent<Text>();
            RefreshHealthBar();

            uiUsername.text = Launcher.myProfile.username;
            photonView.RPC("SyncProfile", RpcTarget.All, Launcher.myProfile.username, Launcher.myProfile.level, Launcher.myProfile.xp);


        }
    }


    private void Update()
    {
        if (!photonView.IsMine)
        {
            RefreshMultiplayerState();
            return;
        }

        //Axis'
        float hmove = Input.GetAxisRaw("Horizontal");
        float vmove = Input.GetAxisRaw("Vertical");

        //Controls
        bool sprint = Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.RightShift);
        bool jump = Input.GetKeyDown(KeyCode.Space);
        bool crouch = Input.GetKeyDown(KeyCode.LeftControl);
        bool pause = Input.GetKeyDown(KeyCode.Escape);

        //States
        bool isgrounded = Physics.Raycast(groundDetector.position, Vector3.down, 0.15f, ground);
        bool isJumping = jump && isgrounded;
        bool isSprinting = sprint && vmove > 0 && !isJumping && isgrounded;
        bool isCrouching = crouch && !isJumping && !isSprinting && isgrounded;

        //Pause
        if (pause)
        {
            GameObject.Find("Pause").GetComponent<Pause>().TogglePause();
        }
        if ( Pause.paused)
        {
             hmove = 0f;
             vmove = 0f;
             sprint = false;
             jump = false;
             crouch = false;
             isgrounded = false;
             isJumping = false;
             isSprinting = false;
             isCrouching = false;
        }

        //Crouching
        if (isCrouching)
        {
           photonView.RPC("SetCrouch", RpcTarget.All, !crouched);
        }

        //Jumping
        if (isJumping)
        {
            if (crouched) photonView.RPC("SetCrouch", RpcTarget.All, false);
            rig.AddForce(Vector3.up * jumpForce);
        }

        //TEST
        //if (Input.GetKeyDown(KeyCode.U)) TakeDamage(100);

        //Headbob
        if (sliding) {
            HeadBob(movementCounter, .15f, .075f);
            weaponParent.localPosition = Vector3.Lerp(weaponParent.localPosition, targetWeaponBobPosition, Time.deltaTime * 10f);
        }
        else if (hmove == 0 && vmove == 0)
        {
            HeadBob(idleCounter, .025f, .025f);
            idleCounter += Time.deltaTime * 2f;
            weaponParent.localPosition = Vector3.Lerp(weaponParent.localPosition, targetWeaponBobPosition, Time.deltaTime * 2f);
        }
        else if (!isSprinting && !crouched)
        {
            HeadBob(movementCounter, .035f, .035f);
            movementCounter += Time.deltaTime * 3f;
            weaponParent.localPosition = Vector3.Lerp(weaponParent.localPosition, targetWeaponBobPosition, Time.deltaTime * 6f);
        }
        else if (crouched)
        {
            HeadBob(movementCounter, .002f, .002f);
            movementCounter += Time.deltaTime * 3f;
            weaponParent.localPosition = Vector3.Lerp(weaponParent.localPosition, targetWeaponBobPosition, Time.deltaTime * 6f);
        }
        else if (isSprinting)
        {
            HeadBob(movementCounter, .15f, .075f);
            movementCounter += Time.deltaTime * 7f;
        weaponParent.localPosition = Vector3.Lerp(weaponParent.localPosition, targetWeaponBobPosition, Time.deltaTime * 10f);
        }

        //UI Refreshes
        RefreshHealthBar();
        weapon.RefreshAmmo(uiAmmo);

    }


    // Update is called once per frame
    void FixedUpdate()
    {
        if(!photonView.IsMine) return;

        //Axis'
        float hmove = Input.GetAxisRaw("Horizontal");
        float vmove = Input.GetAxisRaw("Vertical");

        //Controls
        bool sprint = Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.RightShift);
        bool jump = Input.GetKey(KeyCode.Space);
        bool slide = Input.GetKeyDown(KeyCode.C);


        //States
        bool isgrounded = Physics.Raycast(groundDetector.position, Vector3.down, 0.1f,ground );
        bool isJumping = jump && isgrounded;
        bool isSprinting = sprint && vmove > 0 && !isJumping && isgrounded;
        bool isSliding = isSprinting && slide && !sliding;

        if (Pause.paused)
        {
            hmove = 0f;
            vmove = 0f;
            sprint = false;
            jump = false;
            isgrounded = false;
            isJumping = false;
            isSprinting = false;
            isSliding = false;
            slide = false;
        }

        Vector3 direction = Vector3.zero;
        float adjustedSpeed = speed;

        //Movement
        if (!sliding)
        {

        direction = new Vector3(hmove, 0, vmove);
        direction.Normalize();
            direction = transform.TransformDirection(direction);

            if (isSprinting)
            {
                if (crouched) photonView.RPC("SetCrouch", RpcTarget.All, false);
                adjustedSpeed *= sprintModifier;
            }else if (crouched){
                adjustedSpeed *= crouchModifier;
            }

        }
        else
        {
            direction = slideDirection;
            adjustedSpeed *= slideModifier;
            slideTime -= Time.deltaTime;
            if (slideTime <= 0)
            {
                if (crouched) photonView.RPC("SetCrouch", RpcTarget.All, false);
                sliding = false;
                weaponParentCurrentPosition += Vector3.up * (slideAmount - crouchAmount);
            }
        }

        Vector3 targetVelocity = direction * adjustedSpeed * Time.deltaTime;
        targetVelocity.y = rig.velocity.y;

        rig.velocity = targetVelocity;

        //Sliding
        if (isSliding)
        {
            sliding = true;
            slideDirection = direction;
            slideTime = lengthOfSlide;
            // Adjust cam
            weaponParentCurrentPosition += Vector3.down * (slideAmount - crouchAmount);
            if (!crouched) photonView.RPC("SetCrouch", RpcTarget.All, true);

        }

        //Camera stuff
        if (sliding)
        {
            normalCam.fieldOfView = Mathf.Lerp(normalCam.fieldOfView, baseFOV * sprintFOVModifier * 1.25f, Time.deltaTime * 8);
            normalCam.transform.localPosition = Vector3.Lerp(normalCam.transform.localPosition, origin + Vector3.down * slideAmount, Time.deltaTime * 6f);
        }
        else{

         if (isSprinting) { normalCam.fieldOfView =  Mathf.Lerp(normalCam.fieldOfView,baseFOV * sprintFOVModifier, Time.deltaTime * 8) ; }
        else { normalCam.fieldOfView = Mathf.Lerp(normalCam.fieldOfView, baseFOV, Time.deltaTime * 8); }

            if (crouched) { normalCam.transform.localPosition = Vector3.Lerp(normalCam.transform.localPosition, origin + Vector3.down * crouchAmount, Time.deltaTime * 6f); }
            else { normalCam.transform.localPosition = Vector3.Lerp(normalCam.transform.localPosition, origin, Time.deltaTime * 6f); }

        }

        if (weapon.isAiming)
        {
            if (weapon.currentIndex == 2)
            {
                normalCam.fieldOfView = Mathf.Lerp(normalCam.fieldOfView, 1f, Time.deltaTime * 15f);
            }
            else
            {
            normalCam.fieldOfView = Mathf.Lerp(normalCam.fieldOfView, baseFOV / 4f, Time.deltaTime * 8);

            }
        }
    }

    #endregion

    #region Private Methods

    private void RefreshMultiplayerState()
    {
        float cacheEulY = weaponParent.localEulerAngles.y;

        Quaternion targetRotation = Quaternion.identity * Quaternion.AngleAxis(aimAngle, Vector3.right);
        weaponParent.rotation = Quaternion.Slerp(weaponParent.rotation, targetRotation, Time.deltaTime * 8f);

        Vector3 finalRotation = weaponParent.localEulerAngles;
        finalRotation.y = cacheEulY;

        weaponParent.localEulerAngles = finalRotation;
    }


    [PunRPC]
    void SetCrouch(bool state)
    {
        if (crouched == state) return;

        crouched = state;

        if (crouched)
        {
            standingCollider.SetActive(false);
            standingColliderHead.SetActive(false);
            crouchingCollider.SetActive(true);
            crouchingColliderHead.SetActive(true);
            weaponParentCurrentPosition += Vector3.down * crouchAmount;

        }
        else
        {
            standingCollider.SetActive(true);
            standingColliderHead.SetActive(true);
            crouchingCollider.SetActive(false);
            crouchingColliderHead.SetActive(false);
            weaponParentCurrentPosition -= Vector3.down * crouchAmount;

        }

    }

    void HeadBob(float z, float xIntensity, float yIntensity)
    {
        float aimAdjust = 1f;
        if (weapon.isAiming)
        {
            aimAdjust = 0.1f;
        }
        targetWeaponBobPosition = weaponParentCurrentPosition + new Vector3(Mathf.Cos(z * 2) * xIntensity * aimAdjust, Mathf.Sin(z) * yIntensity * aimAdjust,0);
    }

    void RefreshHealthBar()
    {
        float healthRatio = (float)currentHealth / (float)maxHealth;
        uiHealthBar.localScale = new Vector3(healthRatio, 1, 1);
    }

    [PunRPC]
    private void SyncProfile(string username, int level, int xp)
    {
        playerProfile = new ProfileData(username, level,xp,150f, 150f);
        playerUsername.text = playerProfile.username;
    }
    #endregion


    #region Public Methods

    public void TakeDamage(int damage, int actor)
    {
        if (photonView.IsMine)
        {
        currentHealth -= damage;
            if (currentHealth < 0) currentHealth = 0;

            RefreshHealthBar();

            if (currentHealth <= 0 ) //&& !isDead
            {
                GameObject dp = PhotonNetwork.Instantiate("Dead Player", transform.position, transform.rotation);
                dp.GetComponent<Rigidbody>().velocity = rig.velocity;

                manager.Spawn();
                manager.ChangeStat_S(PhotonNetwork.LocalPlayer.ActorNumber, 1, 1);

                if (actor >= 0) manager.ChangeStat_S(actor, 0, 1);

                PhotonNetwork.Destroy(gameObject);
            }
        }
    }




    #endregion

}
