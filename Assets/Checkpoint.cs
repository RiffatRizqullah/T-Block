using DG.Tweening;
using UnityEngine;

public class Checkpoint : MonoBehaviour
{
    public Transform pole;
    public Transform SpawnPoint;
    [SerializeField] int CheckpointID;
    [SerializeField] FMODAudioSource src;
    public Collider collider;
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        
    }
    private void OnTriggerEnter(Collider other)
    {
        if(other.gameObject.tag == "Player")
        {
            collider.enabled = false;
            src.PlayOneShot();
            pole.DOLocalMoveY(1, 1);
            Manager.instance.SetCheckpoint(CheckpointID);
        }
    }
}
