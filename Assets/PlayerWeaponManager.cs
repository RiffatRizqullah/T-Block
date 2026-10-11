using DG.Tweening;
using Invector.vCharacterController;
using UnityEngine;
using UnityEngine.VFX;

public class PlayerWeaponManager : MonoBehaviour
{
    [SerializeField] Renderer ArmedMaterial;
    [SerializeField] Renderer UnArmedMaterial;
    [SerializeField] vThirdPersonController VTC;
    [SerializeField] float time;
    [SerializeField] VisualEffect BasicAttackVFX;
    bool combatstate;
        // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        
    }
    public void PlayEffect()
    {
        BasicAttackVFX.Play();
    }
    // Update is called once per frame
    void Update()
    {
        if(combatstate != VTC.isCombat)
        {
            combatstate = VTC.isCombat;

            if (combatstate == true)
            {
                DOVirtual.Float(1, -1, time, v =>
                {
                    ArmedMaterial.material.SetFloat("_Visibility", v);
                });
                DOVirtual.Float(-1,1, time, v =>
                {
                    UnArmedMaterial.material.SetFloat("_Visibility", v);
                });
            }
            else if (combatstate == false)
            {
                DOVirtual.Float(1, -1, time, v =>
                {
                    UnArmedMaterial.material.SetFloat("_Visibility", v);
                });
                DOVirtual.Float(-1, 1, time, v =>
                {
                    ArmedMaterial.material.SetFloat("_Visibility", v);
                });
            }
        }
    }
}
