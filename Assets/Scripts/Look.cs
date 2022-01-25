using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using Photon.Pun;

public class Look : MonoBehaviourPunCallbacks
{

    #region Variables
    public static bool cursorLocked = true;

    public Transform player;
    public Transform cam;
    public Transform weapon;

    public float xSensitivity;
    public float ySensitivity;
    public float maxAngle;

    private Quaternion camCenter;

    private Slider xslider;
    private Slider yslider;

    private ProfileData myProfile;

    #endregion

    #region MonoBehaviour Callbacks
    // Start is called before the first frame update
    void Start()
    {
        myProfile = Data.LoadProfile();
        xSensitivity = myProfile.xsens;
        ySensitivity = myProfile.ysens;

        camCenter = cam.localRotation;


        xslider = GameObject.Find("Canvas/Pause/Anchor/Sensitivity/XSens").GetComponent<Slider>();
        yslider = GameObject.Find("Canvas/Pause/Anchor/Sensitivity/YSens").GetComponent<Slider>();

        xslider.value = xSensitivity;
        yslider.value = ySensitivity;

        xslider.onValueChanged.AddListener(delegate { SetXSens(); });
        yslider.onValueChanged.AddListener(delegate { SetYSens(); });
    }


    // Update is called once per frame
    void Update()
    {
        if (!photonView.IsMine) return;
        if (Pause.paused && photonView.IsMine) return;

        SetX();
        SetY();
        UpdateCursorLock();
    }

    #endregion

    #region Private Methods

    void SetY()
    {
        float input = Input.GetAxis("Mouse Y") * ySensitivity * Time.deltaTime;
        Quaternion adj = Quaternion.AngleAxis(input, -Vector3.right);
        Quaternion delta = cam.localRotation * adj;
        if (Quaternion.Angle(camCenter, delta) < maxAngle)
        {
            cam.localRotation = delta;
        }
            weapon.rotation = cam.rotation;
    }

    void SetX()
    {
        float input = Input.GetAxis("Mouse X") * xSensitivity * Time.deltaTime;
        Quaternion adj = Quaternion.AngleAxis(input, Vector3.up);
        Quaternion delta = player.localRotation * adj;
            player.localRotation = delta;

    }

    void UpdateCursorLock()
    {
        if (cursorLocked)
        {
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;

            if (Input.GetKeyDown(KeyCode.Escape))
            {
                cursorLocked = false;
            }
        }
        else {
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;

            if (Input.GetKeyDown(KeyCode.Escape))
            {
                cursorLocked = true;
            }
        }

    }

    public void SetXSens()
    {
        xSensitivity = xslider.value;
        myProfile.xsens = xSensitivity;
        Data.SaveProfile(myProfile);
    }

    public void SetYSens()
    {
        ySensitivity = yslider.value;
        myProfile.ysens = ySensitivity;
        Data.SaveProfile(myProfile);

    }

    #endregion
}
