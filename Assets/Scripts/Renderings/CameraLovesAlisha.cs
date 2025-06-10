using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CameraLovesAlisha : MonoBehaviour
{
    // Start is called before the first frame update
    public Transform target;
    public Vector3 offset;
    public float smoothSpeed = 0.125f; // 부드러움 정도
    public static CameraLovesAlisha Instance;

    void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
        }
        else
        {
            Destroy(gameObject);
        }

    }
    void LateUpdate() // 카메라는 보통 LateUpdate에서 움직임 처리
    {
        if (target != null)
        {
            Vector3 desiredPosition = target.position + offset;

            // Z값은 현재 카메라의 Z값 유지
            desiredPosition.z = transform.position.z;

            Vector3 smoothedPosition = Vector3.Lerp(transform.position, desiredPosition, smoothSpeed);
            transform.position = smoothedPosition;
        }
    }
}
