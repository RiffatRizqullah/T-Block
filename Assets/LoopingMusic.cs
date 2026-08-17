using UnityEngine;
using UnityEngine.Playables;

public class LoopingMusic : MonoBehaviour
{
    [SerializeField] PlayableDirector pb;
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        
    }

    // Update is called once per frame
    public void Loop(float time)
    {
        pb.time = time;
    }
}
