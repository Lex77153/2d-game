using UnityEngine;

public class MovingPlatform : MonoBehaviour
{
    public Transform posA;
    public Transform posB;
    public float speed = 2f;
    private Vector3 targetPos;

    void Start()
    {
        targetPos = posB.position;
    }

    void Update()
    {
        
        transform.position = Vector3.MoveTowards(transform.position, targetPos, speed * Time.deltaTime);

        if (Vector3.Distance(transform.position, targetPos) < 0.1f)
        {
            if (targetPos == posA.position)
                targetPos = posB.position;
            else
                targetPos = posA.position;
        }
    }

    private void OnCollisionEnter2D(Collision2D collision)
    {
        if (collision.gameObject.CompareTag("Player"))
        {
            collision.transform.SetParent(transform);
        }
    }
    private void OnCollisionExit2D(Collision2D collision)
    {
        if (collision.gameObject.CompareTag("Player"))
        {
            collision.transform.SetParent(null);
        }
    }
}