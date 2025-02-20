using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PickUpScript : MonoBehaviour
{
    public GameObject player;         // Reference to the player object
    public Transform holdPos;         // Position where the held object will be placed
    public float throwForce = 500f;   // Force at which the object is thrown
    public float pickUpRange = 5f;    // How far the player can pick up an object from

    private float rotationSensitivity = 1f;  // How fast/slow the object rotates
    private GameObject heldObj;              // Currently held object
    private Rigidbody heldObjRb;             // Rigidbody of the held object
    private bool canDrop = true;             // Used to prevent drop/throw while rotating
    private int holdLayer;                   // Layer index for held objects

    // Layer mask that will ignore the player's collider during raycast
    private int layerMask;

    void Start()
    {
        // Set the layer for held objects 
        holdLayer = LayerMask.NameToLayer("holdLayer");

        // Create a layer mask that ignores the "Player" layer.
        // The ~ (bitwise NOT) operator inverts the mask.
        layerMask = ~LayerMask.GetMask("Player");
    }

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.E))
        {
            if (heldObj == null)
            {
                RaycastHit hit;
                // Cast the ray from this object's position forward, using the layer mask
                if (Physics.Raycast(transform.position, transform.forward, out hit, pickUpRange, layerMask))
                {
                    if (hit.transform.gameObject.CompareTag("canPickUp"))
                    {
                        PickUpObject(hit.transform.gameObject);
                    }
                }
            }
            else
            {
                if (canDrop)
                {
                    StopClipping();
                    DropObject();
                }
            }
        }

        if (heldObj != null)
        {
            MoveObject();
            RotateObject();

            if (Input.GetKeyDown(KeyCode.Mouse0) && canDrop)
            {
                StopClipping();
                ThrowObject();
            }
        }
    }

    void PickUpObject(GameObject pickUpObj)
    {
        if (pickUpObj.GetComponent<Rigidbody>())
        {
            heldObj = pickUpObj;
            heldObjRb = pickUpObj.GetComponent<Rigidbody>();

            // Disable physics simulation while held
            heldObjRb.isKinematic = true;

            // Parent the object to the hold position
            heldObj.transform.parent = holdPos;

            // Change its layer to the holdLayer (optional, if you have collision rules set up)
            heldObj.layer = holdLayer;

            // Ignore collisions with the player to avoid interference
            Physics.IgnoreCollision(heldObj.GetComponent<Collider>(), player.GetComponent<Collider>(), true);
        }
    }

    void DropObject()
    {
        // Re-enable collision with the player
        Physics.IgnoreCollision(heldObj.GetComponent<Collider>(), player.GetComponent<Collider>(), false);

        heldObj.layer = 0;  // Reset to default layer
        heldObjRb.isKinematic = false;
        heldObj.transform.parent = null;  // Unparent the object
        heldObj = null;
    }

    void MoveObject()
    {
        // Keep the object at the hold position
        heldObj.transform.position = holdPos.position;
    }

    void RotateObject()
    {
        if (Input.GetKey(KeyCode.R))
        {
            canDrop = false; // Prevent drop/throw during rotation

            float XaxisRotation = Input.GetAxis("Mouse X") * rotationSensitivity;
            float YaxisRotation = Input.GetAxis("Mouse Y") * rotationSensitivity;

            // Rotate the object based on mouse movement
            heldObj.transform.Rotate(Vector3.down, XaxisRotation);
            heldObj.transform.Rotate(Vector3.right, YaxisRotation);
        }
        else
        {
            canDrop = true;
        }
    }

    void ThrowObject()
    {
        Physics.IgnoreCollision(heldObj.GetComponent<Collider>(), player.GetComponent<Collider>(), false);
        heldObj.layer = 0;
        heldObjRb.isKinematic = false;
        heldObj.transform.parent = null;

        // Apply a force in the forward direction
        heldObjRb.AddForce(transform.forward * throwForce);
        heldObj = null;
    }

    void StopClipping()
    {
        float clipRange = Vector3.Distance(heldObj.transform.position, transform.position);
        RaycastHit[] hits = Physics.RaycastAll(transform.position, transform.forward, clipRange);

        if (hits.Length > 1)
        {
            // Adjust position if the raycast is hitting something other than the held object
            heldObj.transform.position = transform.position + new Vector3(0f, -0.5f, 0f);
        }
    }
}
