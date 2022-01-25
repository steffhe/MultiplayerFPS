  using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Photon.Pun;
using UnityEngine.UI;

public class Weapon : MonoBehaviourPunCallbacks
{
    #region Variables
    public Gun[] loadout;
    public Transform weaponParent;
    public GameObject bulletholePrefab;
    public GameObject hitmark;
    public LayerMask canBeShot;
    public bool isAiming = false;
    public LineRenderer lineRend;

    public GameObject weaponImagePrefab;


    private float currentCooldown;
    public int currentIndex;
    private GameObject currentWeapon;
    private bool isReloading;
    #endregion

    #region MonoBehaviour Callbacks
    // Start is called before the first frame update
    void Start()
    {
        if (photonView.IsMine)
        {
            GameObject arsenal = GameObject.Find("HUD/Arsenal");
            foreach (Transform child in arsenal.transform)
            {
                GameObject.Destroy(child.gameObject);
            }

            int ind = 0;
            foreach (Gun gun in loadout)
            {
                gun.Initialize();

                GameObject weaponImage = weaponImagePrefab;
                Rect rect = new Rect(0, 0, gun.image.width, gun.image.height);
                weaponImage.GetComponent<Image>().sprite = Sprite.Create(gun.image, rect, new Vector2(0.5f, 0.5f), 100);
                //Transform pos = arsenal.transform;
                //float y = pos.position.y + (ind * 100f);
                //pos.position = new Vector3(pos.localPosition.x,y,pos.localPosition.z);
                GameObject weaponimageObject = Instantiate(weaponImage, new Vector3(arsenal.transform.position.x, arsenal.transform.position.y + (ind * 120f), arsenal.transform.position.z), arsenal.transform.rotation, arsenal.transform) as GameObject;
                weaponimageObject.name = "WeaponImage" + ind.ToString();
                ind += 1;
            }
        }
        photonView.RPC("Equip", RpcTarget.All, 0);

        hitmark = GameObject.Find("HUD/Crosshair/Hitmark");

    }

    // Update is called once per frame
    void Update()
    {
        if (Pause.paused && photonView.IsMine) return;

        if (photonView.IsMine && Input.GetAxis("Mouse ScrollWheel") > 0f) // forward
        {
            currentIndex = Mathf.Min(currentIndex + 1, loadout.Length - 1);
            photonView.RPC("Equip", RpcTarget.All, currentIndex);
        }
        else if (photonView.IsMine && Input.GetAxis("Mouse ScrollWheel") < 0f) // backwards
        {
            currentIndex = Mathf.Max(currentIndex - 1, 0);
            photonView.RPC("Equip", RpcTarget.All, currentIndex);
        }

        if (photonView.IsMine && Input.GetKeyDown(KeyCode.Alpha1))
        {
            photonView.RPC("Equip", RpcTarget.All, 0);
        }
        if (photonView.IsMine && Input.GetKeyDown(KeyCode.Alpha2))
        {
            photonView.RPC("Equip", RpcTarget.All, 1);
        }
        if (photonView.IsMine && Input.GetKeyDown(KeyCode.Alpha3))
        {
            photonView.RPC("Equip", RpcTarget.All, 2);
        }

        if (currentWeapon != null)
        {
           if (photonView.IsMine)
            {

         Aim(Input.GetMouseButton(1));
                if(loadout[currentIndex].burst != 1)
                {

                if (Input.GetMouseButtonDown(0) && currentCooldown <= 0 )
            {
                    if (loadout[currentIndex].FireBullet()) { photonView.RPC("Shoot", RpcTarget.All); }
                    else
                    {
                        StartCoroutine(Reload(loadout[currentIndex].reloadTime));
                    }
            }
                }
                else
                {
                    if (Input.GetMouseButton(0) && currentCooldown <= 0)
                    {
                        if (loadout[currentIndex].FireBullet()) { photonView.RPC("Shoot", RpcTarget.All); }
                        else
                        {
                            StartCoroutine(Reload(loadout[currentIndex].reloadTime));
                        }
                    }
                }

                if ( Input.GetKeyDown(KeyCode.R))
                {
                    StartCoroutine(Reload(loadout[currentIndex].reloadTime));
                }

                //cooldown
                if (currentCooldown > 0) currentCooldown -= Time.deltaTime;
            }
        //weapon position elasticity
        currentWeapon.transform.localPosition = Vector3.Lerp(currentWeapon.transform.localPosition, Vector3.zero,Time.deltaTime * 4f);
        }


    }
    #endregion

    #region Public Methods

    public void RefreshAmmo(Text text)
    {
        int clip = loadout[currentIndex].GetClip();
        int stash = loadout[currentIndex].GetStash();

        text.text = clip.ToString("d2") + " / " + stash.ToString("d2");
    }
    #endregion

    #region Private Methods

    IEnumerator Reload(float reloadTime)
    {
        isReloading = true;
        currentWeapon.SetActive(false);
        yield return new WaitForSeconds(reloadTime);
        loadout[currentIndex].Reload();
        currentWeapon.SetActive(true);
        isReloading = false;
    }

    IEnumerator HitPlayer(bool targetIsDead)
    {

        if (targetIsDead)
        {
            hitmark.GetComponent<Image>().color = Color.red;
        }
        else
        {
            hitmark.GetComponent<Image>().color = Color.white;
        }
        hitmark.SetActive(true);
        yield return new WaitForSeconds(0.5f);
        hitmark.SetActive(false);
    }

    IEnumerator ShotLine(Vector3 hitPos)
    {
        if (lineRend != null) { 
        lineRend.enabled = true;
        if (hitPos != Vector3.zero)
        {
            lineRend.SetPosition(0, hitPos);
        }
        else
        {
            Transform cam = transform.Find("Cameras/Normal Camera");

            Vector3 line = cam.position + cam.forward * 1000f;

            lineRend.SetPosition(0, line);
        }
        lineRend.SetPosition(1, lineRend.gameObject.transform.position);
        yield return new WaitForSeconds(0.5f);
        lineRend.enabled = false;
        }
    }

    [PunRPC]
    void Equip(int ind)
    {
        if (currentWeapon != null)
        {
            if (isReloading) StopCoroutine("Reload");
            Destroy(currentWeapon);
        }
        currentIndex = ind;
        GameObject newWeapon = Instantiate(loadout[ind].prefab, weaponParent.position, weaponParent.rotation, weaponParent) as GameObject;
        newWeapon.transform.localPosition = Vector3.zero;
        newWeapon.transform.localEulerAngles = Vector3.zero;
        newWeapon.GetComponent<Sway>().isMine = photonView.IsMine;
        GetComponent<AudioSource>().clip = loadout[ind].shootingSound;

        currentWeapon = newWeapon;
        lineRend = currentWeapon.GetComponentInChildren<LineRenderer>();

        if (photonView.IsMine)
        {
            int curInd = 0;
            foreach (Gun gun in loadout)
            {
                Image img = GameObject.Find("HUD/Arsenal/WeaponImage" + curInd).GetComponent<Image>();
                if (curInd == currentIndex)
                {
                    img.color = new Color(img.GetComponent<Image>().color.r, img.color.g, img.color.b, 0.75f);
                }
                else
                {
                    img.color = new Color(img.GetComponent<Image>().color.r, img.color.g, img.color.b, 0.30f);
                }
                curInd += 1;

            }
        }
    }
  
    void Aim(bool isAiming)
    {
        if ((this.isAiming != isAiming) && isAiming == true)
        {
            if(currentIndex == 2)
            {
                loadout[currentIndex].bloom /= 10;
            }
            else
            {

            loadout[currentIndex].bloom /= 2;
            }

        }
        else if ((this.isAiming != isAiming) && isAiming == false)
        {
            if (currentIndex == 2)
            {
                loadout[currentIndex].bloom *= 10;
            }
            else
            {

                loadout[currentIndex].bloom *= 2;
            }
        }

        this.isAiming = isAiming;
        Transform anchor = currentWeapon.transform.Find("Anchor");
        Transform stateAds = currentWeapon.transform.Find("States/ADS");
        Transform stateHip = currentWeapon.transform.Find("States/Hip");

        if (isAiming)
        {
            //aim
            anchor.position = Vector3.Lerp(anchor.position, stateAds.position, Time.deltaTime * loadout[currentIndex].aimSpeed );
        }
        else
        {
            //hip
            anchor.position = Vector3.Lerp(anchor.position, stateHip.position, Time.deltaTime * loadout[currentIndex].aimSpeed);
        }

    }
    [PunRPC]
    void Shoot()
    {
        Transform spawn = transform.Find("Cameras/Normal Camera");

        //bloom
        Vector3 bloom = spawn.position + spawn.forward * 1000f;
        bloom += Random.Range(-loadout[currentIndex].bloom, loadout[currentIndex].bloom) * spawn.up;
        bloom += Random.Range(-loadout[currentIndex].bloom, loadout[currentIndex].bloom) * spawn.right;
        bloom -= spawn.position;
        bloom.Normalize();


        //cooldown
        currentCooldown = loadout[currentIndex].fireRate;
        GetComponent<AudioSource>().PlayOneShot(loadout[currentIndex].shootingSound);

        //raycast
        RaycastHit hit = new RaycastHit();
        if(Physics.Raycast(spawn.position, bloom, out hit, 1000f, canBeShot))
        {

            if (hit.point == null)
            {
                StartCoroutine(ShotLine(Vector3.zero));
            }
            else
            {
                StartCoroutine(ShotLine(hit.point));

            }

            if (hit.collider.gameObject.layer != 8)
            {
                GameObject newHole = Instantiate(bulletholePrefab, hit.point + hit.normal * 0.001f, Quaternion.identity) as GameObject;
                newHole.transform.LookAt(hit.point + hit.normal);
                Destroy(newHole, 5);
            }
            else
            {
                StopCoroutine("HitPlayer");
                bool targetIsDead = false;
                if (hit.collider.gameObject.tag == "Head")
                {
                    targetIsDead = (hit.collider.transform.root.gameObject.GetPhotonView().GetComponent<Player>().currentHealth - (loadout[currentIndex].damage * 2)) <= 0;
                }
                else
                {
                    targetIsDead = (hit.collider.transform.root.gameObject.GetPhotonView().GetComponent<Player>().currentHealth - loadout[currentIndex].damage) <= 0;

                }
                StartCoroutine("HitPlayer",targetIsDead);
            }

            if (photonView.IsMine)
            {
                if (hit.collider.gameObject.layer == 8)
                {
                    //RPC Damage
                    if (hit.collider.gameObject.tag == "Head")
                    {
                        hit.collider.transform.root.gameObject.GetPhotonView().RPC("TakeDamage", RpcTarget.All, loadout[currentIndex].damage * 2, PhotonNetwork.LocalPlayer.ActorNumber);
                    }
                    else
                    {
                    hit.collider.transform.root.gameObject.GetPhotonView().RPC("TakeDamage", RpcTarget.All, loadout[currentIndex].damage, PhotonNetwork.LocalPlayer.ActorNumber);

                    }
                }
            }
        }
        //gun fx
        currentWeapon.transform.Rotate(-loadout[currentIndex].recoil, 0 ,0);
        currentWeapon.transform.position -= currentWeapon.transform.forward * loadout[currentIndex].kickback;


    }

    [PunRPC]
    private void TakeDamage(int damage, int actor)
    {
        GetComponent<Player>().TakeDamage(damage, actor);
    }
    #endregion


}
