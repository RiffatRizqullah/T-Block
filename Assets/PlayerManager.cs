using DG.Tweening;
using Invector.vCharacterController;
using System.Collections;
using Unity.Cinemachine;
using UnityEngine;
using UnityEngine.Playables;
using UnityEngine.Timeline;

public class PlayerManager : MonoBehaviour
{
    public static PlayerManager instance { get; private set; }
    bool death;
    [SerializeField] CinemachineCamera PreDeathCamera;
    [SerializeField] Animator Player;
    [SerializeField] CinemachineCamera PostDeathCamera;
    [SerializeField] PlayableDirector src;
    [SerializeField] TimelineAsset deathclip;
    public int PlayerHealth;
    [SerializeField] int HealthCapacity;
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    private void Awake()
    {
        instance = this;
    }
    void Start()
    {
        //src = Manager.instance.pb;
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    public void TakeDamage(int damage,bool showDamageText)
    {
        PlayerHealth = Mathf.Clamp(PlayerHealth - damage, 0, HealthCapacity);


        if(PlayerHealth == 0 && death == false)
        {
            death = true;
            StartCoroutine(Death());
        }
    }
    IEnumerator Death()
    {
        Player.enabled = false;
        Manager.instance.SetControl(false);
        Manager.instance.SaveMusicTime();
        //dopitch
        PreDeathCamera.transform.SetParent(null, true);
        PreDeathCamera.Prioritize();
        yield return new WaitForSeconds(6);
        if (Manager.instance.LivesRemaining != 0)
        {
            PostDeathCamera.Prioritize();
            yield return new WaitForSeconds(2);

            src.playableAsset = deathclip;
            src.time = 0;
            src.Play();
            Manager.instance.GameOver();
        }
        else
        {
            Manager.instance.GameOver();
        }
    }
}
