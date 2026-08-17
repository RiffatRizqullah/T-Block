using UnityEngine;

public class AutoRotate : MonoBehaviour
{
    [SerializeField] Vector3 Speed;
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        
    }

    // Update is called once per frame
    void FixedUpdate()
    {
        transform.Rotate(Speed);
    }
}
