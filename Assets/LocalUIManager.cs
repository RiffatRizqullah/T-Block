using System.Collections;
using UnityEngine;

public class LocalUIManager : MonoBehaviour
{
    public static LocalUIManager instance {  get; private set; }
    [SerializeField] GameObject ZoneTitle;
    [SerializeField] Animator ZoneAnim;
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Awake()
    {
        instance = this;
        ZoneAnim = ZoneTitle.GetComponent<Animator>();
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    public void ShowTitle()
    {
        IEnumerator w()
        {
            ZoneAnim.SetTrigger("Play");
            yield return new WaitForSeconds(6);
            ZoneTitle.SetActive(false);
        }
        StartCoroutine(w());
    }
}
