using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Photon.Pun;

public class Sway : MonoBehaviourPunCallbacks
{
    #region Variables

    public float intensity;
    public float smooth;

    private Quaternion originRotation;
    public bool isMine;

    #endregion

    #region MonoBehaviour Callbacks
    private void Start()
    {
        originRotation = transform.localRotation;
    }
    private void Update()
    {
        UpdateSway();
    }
    #endregion

    #region Private Methods

    private void UpdateSway()
    {
        //Controls
        float xMouse = Input.GetAxis("Mouse X");
        float yMouse = Input.GetAxis("Mouse Y");
        if (!isMine)
        {
            xMouse = 0;
            yMouse = 0;
        }

        //Calculate tager rotation
        Quaternion xadj = Quaternion.AngleAxis(-intensity * xMouse, Vector3.up);
        Quaternion yadj = Quaternion.AngleAxis(intensity * yMouse, Vector3.right);

        Quaternion targetRotation = originRotation * xadj * yadj;

        //Rotate toward target rotation
        transform.localRotation = Quaternion.Lerp(transform.localRotation, targetRotation, Time.deltaTime * smooth);

    }

    #endregion
}
