using UnityEngine;

public class FreeCamera : MonoBehaviour {
    public Transform cachedTrans;
    public float moveSpeed = 5f;
    public float rotateSpeed = 2f;
    
    private Vector3 pos;
    private float rotX;
    private float rotY;

    private void Awake() {
        pos = cachedTrans.position;
        rotX = cachedTrans.eulerAngles.y;
        rotY = cachedTrans.eulerAngles.x;
    }

    private void Update() {
        if(!Cursor.visible) {
            Vector3 dir = new Vector3(Input.GetAxisRaw("Horizontal"), 0f, Input.GetAxisRaw("Vertical"));
            dir = cachedTrans.TransformDirection(dir);
            pos += dir * moveSpeed * Time.unscaledDeltaTime;

            // Only allow rotation when mouse is locked.
            rotX += Input.GetAxisRaw("Mouse X") * rotateSpeed;
            rotY -= Input.GetAxisRaw("Mouse Y") * rotateSpeed;
        }

        rotY = Mathf.Clamp(rotY, -90f, 90f);
        cachedTrans.SetPositionAndRotation(pos, Quaternion.Euler(rotY, rotX, 0f));
    }
}