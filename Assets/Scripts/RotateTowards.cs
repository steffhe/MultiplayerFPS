using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class RotateTowards : MonoBehaviour
{
    public float moveSpeed = 1f;
    public GameObject plane;
    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        Vector3 moveInput = new Vector3(Input.GetAxisRaw("Horizontal"), Input.GetAxisRaw("Vertical"), 0f);
        Vector3 moveVelocity = moveInput * moveSpeed;

        Ray cameraRay = Camera.main.ScreenPointToRay(Input.mousePosition);


        var filter = GetComponent<MeshFilter>();
        Vector3 normal = plane.transform.position;

        if (filter && filter.mesh.normals.Length > 0)
            normal = filter.transform.TransformDirection(filter.mesh.normals[0]);

        var planecomp = new Plane(normal, plane.transform.position);
        Plane groundPlane = planecomp;
        float rayLength;

        if (groundPlane.Raycast(cameraRay, out rayLength))
        {
            Vector3 pointToLook = cameraRay.GetPoint(rayLength);
            Debug.DrawLine(cameraRay.origin, pointToLook, Color.cyan);

            transform.LookAt(new Vector3(pointToLook.x, pointToLook.y, pointToLook.z));
        }
    }
}
