using UnityEngine;

public class FollowCamera : MonoBehaviour
{
    public Transform target;
    public Vector3 offset = new Vector3(0f, 2f, 2f);
    public float smoothSpeed = 0.125f;
    private Vector3 velocity = Vector3.zero;

    public bool isFollowing = false;

    // ★追加：最初の位置
    private Vector3 startPos;
    private Quaternion startRot;

    void Start()
    {
        // 最初の場所を記憶
        startPos = transform.position;
        startRot = transform.rotation;
    }

    void LateUpdate()
    {
        if (target == null || isFollowing == false) return;

        Vector3 desiredPosition = target.position + offset;
        transform.position = Vector3.SmoothDamp(transform.position, desiredPosition, ref velocity, smoothSpeed);
        transform.LookAt(target);
    }

    public void StartFollowing()
    {
        isFollowing = true;
    }

    // ★追加：カメラリセット機能
    public void ResetCamera()
    {
        isFollowing = false;
        // 即座に戻る
        transform.position = startPos;
        transform.rotation = startRot;
        velocity = Vector3.zero;
    }
}