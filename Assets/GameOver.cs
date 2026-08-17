using DG.Tweening;
using TMPro;
using UnityEngine;
using UnityEngine.Playables;
using UnityEngine.SceneManagement;

public class GameOver : MonoBehaviour
{
    [SerializeField] float countdown;
    [SerializeField] PlayableDirector pb;
    [SerializeField] TextMeshProUGUI count;
    [SerializeField] CanvasGroup fade;
    bool startcounting;
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        
    }

    // Update is called once per frame
    void FixedUpdate()
    {
        if (startcounting == true)
        {
            countdown -= Time.deltaTime;
            count.text = Mathf.Round(countdown).ToString();
            if (Input.GetKeyDown(KeyCode.Space) || Input.GetKeyDown(KeyCode.Return) || Input.GetMouseButtonDown(0))
            {
                startcounting = false;
                pb.time = 15;
                pb.Resume();
            }
            if (countdown <= 0)
            {
                startcounting = false;
                fade.DOFade(1, 3).OnComplete(() =>
                {
                    if (Manager.instance != null)
                    {
                        Destroy(Manager.instance.gameObject);
                    }
                    SceneManager.LoadScene("MainMenu");
                });
            }
        }
    }
    public void StartCountDown()
    {
        startcounting = true;
        pb.Pause();
        pb.time = 6;
    }
    public void Continue()
    {
        string level = Manager.instance.Level;
        if (Manager.instance != null)
        {
            Destroy(Manager.instance.gameObject);
        }
        SceneManager.LoadScene(level);
    }
}
