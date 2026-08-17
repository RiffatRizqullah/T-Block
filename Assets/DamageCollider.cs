using UnityEngine;

public class DamageCollider : MonoBehaviour
{
    [SerializeField] int Damage;
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
        if(collision.gameObject.tag == "Player")
        {
            PlayerManager.instance.TakeDamage(Damage, false);
        }
    }
}
