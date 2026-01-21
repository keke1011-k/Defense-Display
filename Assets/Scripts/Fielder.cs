using UnityEngine;

public class Fielder : MonoBehaviour
{
    public enum Role { Catcher, Cover }
    [Header("役割設定")]
    public Role myRole = Role.Catcher;

    [Header("動きの設定")]
    public Transform targetBall;
    public float runSpeed = 8.0f;
    public float coverDistance = 5.0f;

    [Header("守備範囲")]
    public float catchRadius = 3.0f;       // 移動できる範囲（リード）
    public float realCatchDistance = 1.0f; // 捕球判定距離
    public float maxCatchHeight = 3.0f;

    [Header("マーカーの見た目設定")]
    public GameObject rangeMarker;
    public float groundOffset = 0.1f;

    public static bool isBallCaught = false;
    public static bool isBallBounced = false;

    private bool isRunning = false;
    private GameObject myMarker;
    private Renderer myRenderer;

    private Vector3 startPos;
    private Quaternion startRot;

    // 赤マーカーの中心座標
    private Vector3 landingPoint;

    void Start()
    {
        isBallBounced = false;
        isBallCaught = false;
        startPos = transform.position;
        startRot = transform.rotation;

        // 最初は自分の場所を目的地にしておく
        landingPoint = transform.position;

        myRenderer = GetComponentInChildren<Renderer>();

        if (rangeMarker != null)
        {
            myMarker = Instantiate(rangeMarker);
            UpdateMarkerPosition();
            float diameter = catchRadius * 2.0f;
            myMarker.transform.localScale = new Vector3(diameter, 0.05f, diameter);
            myMarker.SetActive(false);
        }
    }

    // ClickToLaunchから「赤マーカーの位置」を受け取る
    public void SetLandingPoint(Vector3 pos)
    {
        landingPoint = pos;
    }

    void Update()
    {
        if (isBallBounced && myMarker != null)
        {
            myMarker.SetActive(false);
        }

        if (!isRunning || targetBall == null || isBallCaught) return;

        // --- 1. 目標地点を決める（赤マーカーの中心！） ---
        Vector3 targetPosFlat = new Vector3(landingPoint.x, 0, landingPoint.z);
        Vector3 destination = targetPosFlat;

        // カバー役なら、中心から少しずらす（必要なければ myRole を Catcher にしてください）
        if (myRole == Role.Cover)
        {
            Vector3 direction = (targetPosFlat - new Vector3(startPos.x, 0, startPos.z)).normalized;
            destination = targetPosFlat + (direction * coverDistance);
        }

        // --- 2. 移動制限（リード）の適用 ---
        // 「赤マーカーの中心に行きたいけど、自分の守備範囲(catchRadius)以上は離れられない！」という計算
        Vector3 offset = destination - new Vector3(startPos.x, 0, startPos.z);

        // 距離が半径を超えていたら、強制的に半径の長さに縮める（円周上で止まる）
        offset = Vector3.ClampMagnitude(offset, catchRadius);

        // 最終的な移動先
        destination = new Vector3(startPos.x, 0, startPos.z) + offset;

        Vector3 finalTarget = new Vector3(destination.x, transform.position.y, destination.z);

        // 体を向ける
        transform.LookAt(finalTarget);

        // --- 3. 壁抜け防止 ---
        Vector3 moveDir = (finalTarget - transform.position).normalized;
        float moveDist = runSpeed * Time.deltaTime;
        Ray checkRay = new Ray(transform.position + Vector3.up * 1.0f, moveDir);

        if (Physics.Raycast(checkRay, out RaycastHit hit, moveDist + 0.5f))
        {
            // ボールや地面以外にぶつかりそうなら止まる
            if (hit.collider.gameObject.name != "Sphere" && hit.collider.gameObject.name != "Ground")
            {
                CheckCatch();
                return;
            }
        }

        // --- 4. 移動実行 ---
        transform.position = Vector3.MoveTowards(transform.position, finalTarget, moveDist);

        // --- 5. キャッチ判定 ---
        CheckCatch();
    }

    void CheckCatch()
    {
        // キャッチ判定は「実際のボール」との距離で見ます
        Vector3 ballPosFlat = new Vector3(targetBall.position.x, 0, targetBall.position.z);
        Vector3 myPosFlat = new Vector3(transform.position.x, 0, transform.position.z);

        float flatDistance = Vector3.Distance(myPosFlat, ballPosFlat);
        float heightDiff = Mathf.Abs(targetBall.position.y - transform.position.y);

        if (flatDistance < realCatchDistance && heightDiff < maxCatchHeight)
        {
            CatchBall();
        }
    }

    void UpdateMarkerPosition()
    {
        if (myMarker != null)
        {
            Ray ray = new Ray(startPos + Vector3.up * 1.0f, Vector3.down);
            RaycastHit hit;

            if (Physics.Raycast(ray, out hit, 10.0f))
            {
                myMarker.transform.position = hit.point + (hit.normal * groundOffset);
                myMarker.transform.rotation = Quaternion.FromToRotation(Vector3.up, hit.normal);
            }
            else
            {
                myMarker.transform.position = new Vector3(startPos.x, groundOffset, startPos.z);
                myMarker.transform.rotation = Quaternion.identity;
            }
        }
    }

    public void UpdateMarkerSize(float serverTime)
    {
        if (myMarker == null || isBallBounced || isBallCaught) return;
        float diameter = catchRadius * 2.0f;
        myMarker.transform.localScale = new Vector3(diameter, 0.05f, diameter);
    }

    void CatchBall()
    {
        isBallCaught = true;
        //if (myMarker != null) myMarker.SetActive(false);
        Rigidbody ballRb = targetBall.GetComponent<Rigidbody>();
        if (ballRb != null)
        {
            ballRb.velocity = Vector3.zero;
            ballRb.angularVelocity = Vector3.zero;
            ballRb.isKinematic = true;
        }
        Debug.Log(gameObject.name + " がナイスキャッチ！");
    }

    public void StartChasing()
    {
        isBallCaught = false;
        isBallBounced = false;
        isRunning = true;

        if (myMarker != null)
        {
            myMarker.SetActive(true);
            float diameter = catchRadius * 2.0f;
            myMarker.transform.localScale = new Vector3(diameter, 0.05f, diameter);
        }
    }

    public void ResetFielder()
    {
        isRunning = false;
        isBallCaught = false;
        isBallBounced = false;
        transform.position = startPos;
        transform.rotation = startRot;
        if (myMarker != null) myMarker.SetActive(false);

        // リセット時はターゲットを自分に戻す
        landingPoint = startPos;
    }

    public void ShowMarkerOnly()
    {
        if (myMarker != null)
        {
            UpdateMarkerPosition();
            myMarker.SetActive(true);
            float diameter = catchRadius * 2.0f;
            myMarker.transform.localScale = new Vector3(diameter, 0.05f, diameter);
        }
    }
}