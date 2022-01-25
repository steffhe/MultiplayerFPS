using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Photon.Pun;

public class DeadPlayer : MonoBehaviour
{
    // Start is called before the first frame update
    void Start()
    {
        GetComponent<Rigidbody>().AddForce(Vector3.back, ForceMode.Impulse);
        StartCoroutine(Destroy(5f));
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    IEnumerator Destroy(float time)
    {
        yield return new WaitForSeconds(time);
        PhotonNetwork.Destroy(gameObject);
    }
}
