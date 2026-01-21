using UnityEngine;

public class TopDownCamera : MonoBehaviour
{
    [Header("追跡対象")]
    public Transform target; // ボールを入れる

    [Header("カメラの高さ")]
    public float height = 50.0f; // 上空50mから見る

    void LateUpdate()
    {
        if (target == null) return;

        // ボールの真上に移動する（高さは固定）
        Vector3 newPos = new Vector3(target.position.x, height, target.position.z);
        transform.position = newPos;
    }
}