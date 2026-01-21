using UnityEngine;

public class Ballmarker : MonoBehaviour
{
    [Header("影の設定")]
    public GameObject shadowPrefab; // 影のプレハブ
    public float shadowSize = 1.0f; // 影の大きさ

    private GameObject myShadow;

    void Start()
    {
        // 影を生成する
        if (shadowPrefab != null)
        {
            myShadow = Instantiate(shadowPrefab);
            // 最初は見えないようにしておく
            myShadow.SetActive(false);
        }
    }

    void Update()
    {
        if (myShadow == null) return;

        // ボールから「真下」に向かってレイ（見えない光線）を飛ばす
        Ray ray = new Ray(transform.position, Vector3.down);
        RaycastHit hit;

        // 地面までの距離が 100m 以内なら影を出す
        if (Physics.Raycast(ray, out hit, 100.0f))
        {
            // 何かに当たった（地面がある）ので表示ON
            myShadow.SetActive(true);

            // 1. 位置合わせ
            // 当たった場所(hit.point)の「ほんの少し上(0.01f)」に置く
            myShadow.transform.position = hit.point + Vector3.up * 0.01f;

            // 2. 大きさ調整
            myShadow.transform.localScale = new Vector3(shadowSize, 0.01f, shadowSize);
        }
        else
        {
            // 地面がない（場外や高すぎる）時は隠す
            myShadow.SetActive(false);
        }
    }

    // ボールが消滅する時に、影も一緒に消す
    void OnDestroy()
    {
        if (myShadow != null)
        {
            Destroy(myShadow);
        }
    }
}