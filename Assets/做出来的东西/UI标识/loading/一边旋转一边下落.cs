using UnityEngine;

public class 一边旋转一边下落 : MonoBehaviour
{
    private float speed = 1.5f;
    private Rigidbody2D rb;
    public bool 左右;

    // Start is called before the first frame update
    private void Start()
    {
        rb = GetComponent<Rigidbody2D>();
    }

    // Update is called once per frame
    private void Update()
    {
        //transform.position += Vector3.down * speed * Time.deltaTime;
        if (左右)
        {
            transform.Rotate(0, 0, speed * 360 * Time.deltaTime);
        }
        else
        {
            transform.Rotate(0, 0, -speed * 360 * Time.deltaTime);
        }
    }

    private void OnBecameInvisible()
    {
        Destroy(gameObject);
    }
}