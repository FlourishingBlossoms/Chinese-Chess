using UnityEngine;

public class 少女分形 : MonoBehaviour
{
    public GameObject 少女祈祷中;
    public GameObject Loading;
    public GameObject 月亮1;
    public GameObject 月亮2;
    public GameObject 位置3;
    public GameObject 位置4;
    private float 计时器;
    private float 透明度;
    private bool 透明;
    private Renderer 少女祈祷中Renderer;

    // Start is called before the first frame update
    private void Start()
    {
        计时器 = 0;
        透明度 = 1.0f;
        透明 = false;

        if (少女祈祷中 != null)
        {
            少女祈祷中Renderer = 少女祈祷中.GetComponent<Renderer>();
        }
    }

    // Update is called once per frame
    private void Update()
    {
        计时器 += Time.deltaTime;

        if (透明)
        {
            透明度 += 1.0f * Time.deltaTime;
        }
        else
        {
            透明度 -= 1.0f * Time.deltaTime;
        }
        if (透明度 < 40f / 255f)
        {
            透明 = true;
            透明度 = 40f / 255f;
        }
        else if (透明度 > 1.0f)
        {
            透明 = false;
            透明度 = 1.0f;
        }

        if (少女祈祷中Renderer != null && 少女祈祷中Renderer.material != null)
        {
            Color color = 少女祈祷中Renderer.material.color;
            color.a = 透明度;
            少女祈祷中Renderer.material.color = color;
        }

        if (计时器 >= 1.6f && 少女祈祷中 != null && 位置3 != null)
        {
            少女祈祷中.transform.position = Vector3.Lerp(
                少女祈祷中.transform.position,
                位置3.transform.position,
                2.0f * Time.deltaTime
            );
        }

        if (计时器 >= 1.8f)
        {
            if (Loading != null && 位置4 != null)
            {
                Loading.transform.position = Vector3.Lerp(
                    Loading.transform.position,
                    位置4.transform.position,
                    2.0f * Time.deltaTime
                );
            }

            if (月亮1 != null)
            {
                月亮1.transform.position += Vector3.down * 5 * Time.deltaTime;
            }

            if (月亮2 != null)
            {
                月亮2.transform.position += Vector3.down * 5 * Time.deltaTime;
            }
        }
    }
}