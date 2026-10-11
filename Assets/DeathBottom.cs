using UnityEngine;

public class DeathBottom : MonoBehaviour
{
    bool collided;
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        
    }

    // Update is called once per frame
    private void OnTriggerEnter(Collider other)
    {
        if (other.tag == "Player" && collided == false)
        {
            collided = true;
            PlayerManager.instance.TakeDamage(PlayerManager.instance.PlayerHealth,false);
        }
    }
}
