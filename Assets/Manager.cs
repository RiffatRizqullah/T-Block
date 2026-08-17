using DG.Tweening;
using Invector.vCharacterController;
using System.Collections;
using TMPro;
using Unity.Cinemachine;
using Unity.Collections.LowLevel.Unsafe;
using UnityEngine;
using UnityEngine.Playables;
using UnityEngine.SceneManagement;
using UnityEngine.Timeline;
using FMODUnity;

public class Manager : MonoBehaviour
{
    public static Manager instance { get; private set; }
    
    [SerializeField] vThirdPersonCamera VTC;
    [SerializeField] vThirdPersonInput VTI;
    [SerializeField] GameObject[] GameOverUIs;
    [SerializeField] CanvasGroup MainCanvas;
    [SerializeField] CanvasGroup fade;
    [SerializeField] EventReference MenuTheme;



    [SerializeField]
    public string Level;
    public int LivesRemaining;
    public TextMeshProUGUI LivesUI;
    public AudioSource src;
    public StudioEventEmitter pb;
    [SerializeField] float musictime;
    bool waitmusicfadein;
    [SerializeField] int currentCheckpoint;
    [SerializeField] bool[] CheckpointStates;
    [SerializeField] Checkpoint[] checkpoints;
    
    
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Awake()
    {

        if (Manager.instance == null)
        {
            instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
        }
        
        
        LivesUI.text = $"x {LivesRemaining}";
    }
    private void Start()
    {
        checkpoints = LocalManager.instance.Checkpoint;

        CheckpointStates = new bool[checkpoints.Length];
    }
    // Update is called once per frame
    void Update()
    {
        
    }
    public void SetControl(bool state)
    {
        VTI.enabled = state;
        VTC.enabled = false;
        VTC.GetComponent<ThirdPersonCameraController>().enabled = state;
    }
    public void EnableControllers()
    {
        VTI.GetComponent<Rigidbody>().isKinematic = false;
        VTC.lockCamera = false;
        VTI.enabled = true;
    }
    public void prioritize()
    {
        VTC.GetComponent<CinemachineCamera>().Prioritize();
    }
    public void GameOver()
    {
        if (LivesRemaining != 0)
        {
            MainCanvas.blocksRaycasts = true;
            GameOverUIs[0].SetActive(true);
        }
        else if (LivesRemaining == 0)
        {
            fade.DOFade(1, 1).OnComplete(() =>
            {
                SceneManager.LoadScene("GameOver");
            });
        }

    }    

    public void Retry()
    {
        LivesRemaining--;
        LivesUI.text = $"x {LivesRemaining}";
        fade.DOFade(1, 1).OnComplete(() =>
        {
            SceneManager.LoadScene(Level);
            foreach(GameObject ui in GameOverUIs)
            {
                ui.SetActive(false);
            }
            src.volume = 0;
            pb.Stop();
            pb.EventReference = MenuTheme;
            pb.Play();
            //pb.time = musictime;
            waitmusicfadein = true;
        });
        
    }

    public void GiveUp()
    {
        fade.DOFade(1, 1).OnComplete(() =>
        {
            SceneManager.LoadScene("MainMenu");
            Destroy(gameObject);
        });
    }
    private void OnLevelWasLoaded(int level)
    {
        if(SceneManager.GetActiveScene().name == "GameOver")
        {
            fade.alpha = 0;
        }
        if(waitmusicfadein == true)
        {
            IEnumerator WaitUntilFullyLoaded()
            {
                checkpoints = LocalManager.instance.Checkpoint;
                for (int x = 0;  x < checkpoints.Length; x++)
                {
                    if (CheckpointStates[x] == true)
                    {
                        print("true");
                        checkpoints[x].pole.transform.localPosition = new Vector3(0, 1, 0);
                        checkpoints[x].collider.enabled = false;
                    }
                    else if(CheckpointStates[x] == false)
                    {
                        print("false");
                    }
                }
                VTC = vThirdPersonCamera.Instance;
                VTI = vThirdPersonInput.instance;
                if (LocalManager.instance.Checkpoint[currentCheckpoint] != null)
                {
                    
                    VTI.transform.position = LocalManager.instance.Checkpoint[currentCheckpoint].SpawnPoint.transform.position;
                    
                }
                VTC.GetComponent<CinemachineCamera>().Prioritize();
                yield return new WaitForFixedUpdate();
                
                EnableControllers();
                yield return new WaitForSeconds(1.5f);

                waitmusicfadein = false;
                src.DOFade(1, 1);
                fade.DOFade(0, 1).OnComplete(() =>
                {
                    
                });
            }
            StartCoroutine(WaitUntilFullyLoaded());

        }
        
    }
    public void SetCheckpoint(int id)
    {
        currentCheckpoint = id;
        CheckpointStates[id] = true;
    }
    public void SaveMusicTime()
    {
        //musictime = (float)pb.time;
    }
    public void CallLocalUIToShowTitle()
    {
        LocalUIManager.instance.ShowTitle();
    }
}
