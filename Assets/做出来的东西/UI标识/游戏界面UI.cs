using UnityEngine;
using UnityEngine.UI;
public class 游戏界面UI : MonoBehaviour
{
    // Start is called before the first frame update
    public static 游戏界面UI Instance { get; private set; }

    public  static string Difficulty;

    public GameObject Easy;
    public GameObject Normal;
    public GameObject Hard;
    public GameObject Lunatic;
    public Button 悔棋;

    private void Start()
    {

        Difficulty = PlayerPrefs.GetString("Difficulty");
        if (Difficulty == "Easy")
        {
            Easy.SetActive(true);
            Normal.SetActive(false);
            Hard.SetActive(false);
            Lunatic.SetActive(false);
            Debug.Log("Easy");
        }
        else if (Difficulty == "Normal")
        {
            Easy.SetActive(false);
            Normal.SetActive(true);
            Hard.SetActive(false);
            Lunatic.SetActive(false);
            Debug.Log("Normal");
        }
        else if (Difficulty == "Hard")
        {
            Easy.SetActive(false);
            Normal.SetActive(false);
            Hard.SetActive(true);
            Lunatic.SetActive(false);
            Debug.Log("Hard");
        }
        else if (Difficulty == "Lunatic")
        {
            Easy.SetActive(false);
            Normal.SetActive(false);
            Hard.SetActive(false);
            Lunatic.SetActive(true);
            Debug.Log("Lunatic");
        }
        else if (Difficulty == "Double")
        {
            Easy.SetActive(false);
            Normal.SetActive(false);
            Hard.SetActive(false);
            Lunatic.SetActive(false);
        }
        if (悔棋 != null) 悔棋.onClick.AddListener(选择象棋.Instance.UndoMove);

    }

    // Update is called once per frame
    private void Update()
    {

    }
}