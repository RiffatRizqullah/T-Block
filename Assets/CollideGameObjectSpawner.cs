using UnityEngine;

public class CollideGameObjectSpawner : MonoBehaviour
{
    [SerializeField] GameObject Obj;
    [SerializeField] string Tag;
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
        if (collision.gameObject.tag == Tag) {
            Instantiate(Obj, collision.transform.position, Quaternion.identity);
        }
    }
}
