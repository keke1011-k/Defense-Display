using UnityEngine;

public class BallBounce : MonoBehaviour
{
    void OnCollisionEnter(Collision collision)
    {
        // まだ発射前（重力OFF）なら無視
        if (GetComponent<Rigidbody>().useGravity == false) return;

        // 地面、または壁に当たったら「バウンドした（終了）」と判定
        // 名前が "Ground" じゃなくても、床や壁なら何でも反応するようにします
        if (collision.gameObject.name != "Player") // プレイヤー以外に当たったら
        {
            Debug.Log("着地！");
            Fielder.isBallBounced = true;
        }
    }
}