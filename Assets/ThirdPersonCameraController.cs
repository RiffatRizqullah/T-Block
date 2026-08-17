using UnityEngine;
using Invector.vCharacterController;
using DG.Tweening;
public class ThirdPersonCameraController : MonoBehaviour
{
    public bool EnableZooming;
    [SerializeField] vThirdPersonCamera VTC;
    [SerializeField] vThirdPersonController VTCon;
    [SerializeField] Vector2 ZoomRange;
    [SerializeField] float DistanceWhileFocusing;
    [SerializeField] Vector2 RightOffsetState;
    float CurrentDistance;
    
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        if(EnableZooming == true)
        {
            VTC.defaultDistance = Mathf.Clamp(VTC.defaultDistance + -Input.GetAxis("Mouse ScrollWheel"), ZoomRange.x, ZoomRange.y);
        }

        if (Input.GetMouseButtonDown(1))
        {

            EnableZooming = false;
            VTCon.strafeSpeed.rotateWithCamera = true;
            VTCon.freeSpeed.rotateWithCamera = true;
            VTCon.isStrafing = true;
            CurrentDistance = VTC.defaultDistance;
            VTC.defaultDistance = DistanceWhileFocusing;
            DOVirtual.Float(VTC.rightOffset, RightOffsetState.y, .5f, v =>
            {
                VTC.rightOffset = v;
            }).SetEase(Ease.OutCubic);
        }
        else if (Input.GetMouseButtonUp(1))
        {
            VTCon.strafeSpeed.rotateWithCamera = false;
            VTCon.freeSpeed.rotateWithCamera = false;
            VTCon.isStrafing = false;
            VTC.defaultDistance = CurrentDistance;
            DOVirtual.Float(VTC.rightOffset, RightOffsetState.x, .5f, v =>
            {
                VTC.rightOffset = v;
            }).SetEase(Ease.OutCubic);

            EnableZooming = true;
        }
    }
}
