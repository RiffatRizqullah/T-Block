using System.Collections;
using UnityEngine;

public class Death : MonoBehaviour
{
    bool death;
    [SerializeField] GameObject Explosion;
    [SerializeField] Transform player;
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    private void OnCollisionEnter(Collision collision)
    {
        if (death == false)
        {

            death = true;
            IEnumerator wait()
            {
                yield return new WaitForSeconds(1.5f);
                Instantiate(Explosion,player.position,Quaternion.identity);
            }
            StartCoroutine(wait());
        }
    } 
}
