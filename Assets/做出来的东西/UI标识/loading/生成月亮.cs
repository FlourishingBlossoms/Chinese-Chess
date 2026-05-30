using UnityEngine;

public class 生成月亮 : MonoBehaviour
{
    public bool 蓝月亮;
    public bool 红月亮;
    public GameObject _蓝月亮;
    public GameObject _红月亮;
    public float 生成时间;
    public float 计时器;

    // Start is called before the first frame update
    private void Start()
    {
        计时器 = 0;
    }

    // Update is called once per frame
    private void Update()
    {
        计时器 += Time.deltaTime;
        if (计时器 >= 生成时间)
        {
            if (蓝月亮)
            {
                Instantiate(_蓝月亮, transform.position, Quaternion.identity);
                计时器 = 0;
            }
            if (红月亮)
            {
                Instantiate(_红月亮, transform.position, Quaternion.identity);
                计时器 = 0;
            }
        }
    }
}