using UnityEngine;

public class LocalManager : MonoBehaviour
{
    public static LocalManager instance {  get; private set; }
    public Checkpoint[] Checkpoint;
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Awake()
    {
        instance = this;
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
