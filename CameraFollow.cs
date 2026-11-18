using UnityEngine;

public class CameraFollow : MonoBehaviour
{
    public Transform target;           
    public Vector2 deadzone = new Vector2(1.5f, 1f); 
    public float smoothSpeed = 30f;     
    public Vector3 offset = new Vector3(0, 0, -10f);

    private void LateUpdate()
    {
        if (target == null) return;

        Vector3 currentPos = transform.position;
        Vector3 targetPos = target.position + offset;

        float deltaX = targetPos.x - currentPos.x;
        float deltaY = targetPos.y - currentPos.y;

        if (Mathf.Abs(deltaX) > deadzone.x)
        {
            currentPos.x = Mathf.Lerp(currentPos.x, targetPos.x - Mathf.Sign(deltaX) * deadzone.x, Time.deltaTime * smoothSpeed);
        }

        if (Mathf.Abs(deltaY) > deadzone.y)
        {
            currentPos.y = Mathf.Lerp(currentPos.y, targetPos.y - Mathf.Sign(deltaY) * deadzone.y, Time.deltaTime * smoothSpeed);
        }

        transform.position = currentPos;
    }
    
    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireCube(transform.position, new Vector3(deadzone.x * 2, deadzone.y * 2, 0));
    }
}