using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Networking;
using System.Collections;
using System.Text;

[RequireComponent(typeof(Rigidbody))]
public class ClickToLaunch : MonoBehaviour
{
    [Header("パワー設定")]
    public float minForce = 200.0f;
    public float maxForce = 1000.0f;
    public float chargeSpeed = 500.0f;

    [Header("回転設定")]
    public float spinSpeed = 500.0f;

    [Header("重力設定")]
    public float extraGravity = 30.0f;

    [Header("落下地点マーカー")]
    public GameObject landingMarkerPrefab;
    public float markerSize = 2.0f;
    private GameObject currentMarker;

    [Header("UI設定")]
    public Text probText;

    [Header("参照")]
    public FollowCamera cameraScript;
    public Slider powerGauge;
    public Fielder[] fielders;

    private string serverUrl = "https://jsonplaceholder.typicode.com/posts";

    [System.Serializable]
    public class TimeRequest
    {
        public float time_elapsed;
        public float win_rate;
    }

    private Rigidbody rb;
    private Camera mainCamera;
    private float currentForce;
    private Vector3 launchDirection;
    private bool isCharging = false;
    private bool isFlying = false;
    private float currentFlightTime = 0f;
    private float predictedFlightTime = 1.0f;
    private float finalWinRate = 0f;
    private Vector3 ballStartPos;
    private Quaternion ballStartRot;

    private bool hasFinished = false;

    void Start()
    {
        rb = GetComponent<Rigidbody>();
        mainCamera = Camera.main;
        currentForce = minForce;
        ballStartPos = transform.position;
        ballStartRot = transform.rotation;

        if (cameraScript == null) cameraScript = FindObjectOfType<FollowCamera>();

        // ★修正：最初は物理演算を止めて固定しておく（これでズレない）
        rb.isKinematic = true;
        rb.useGravity = false;
        rb.velocity = Vector3.zero;
        rb.angularVelocity = Vector3.zero;

        if (powerGauge != null)
        {
            powerGauge.minValue = minForce;
            powerGauge.maxValue = maxForce;
            powerGauge.value = minForce;
        }

        if (landingMarkerPrefab != null)
        {
            currentMarker = Instantiate(landingMarkerPrefab);
            currentMarker.SetActive(false);
        }

        if (probText != null)
        {
            probText.text = "Standby";
            probText.color = Color.white;
        }
    }

    void Update()
    {
        // 右クリックリセット
        if (Input.GetMouseButtonDown(1))
        {
            ResetGame();
            return;
        }

        if (isFlying)
        {
            currentFlightTime += Time.deltaTime;

            if (Fielder.isBallBounced || Fielder.isBallCaught)
            {
                isFlying = false;
                hasFinished = true;

                if (currentMarker != null)
                {
                    currentMarker.SetActive(true);
                    if (Fielder.isBallBounced)
                    {
                        currentMarker.transform.position = transform.position + Vector3.down * 0.4f;
                    }
                }
            }
        }

        if (Input.GetMouseButtonDown(0))
        {
            if (isFlying || hasFinished) return;

            currentForce = minForce;
            isCharging = true;
            Ray ray = mainCamera.ScreenPointToRay(Input.mousePosition);
            launchDirection = ray.direction;

            if (currentMarker != null) currentMarker.SetActive(false);

            if (probText != null)
            {
                probText.text = "解析中";
                probText.color = Color.white;
            }

            if (fielders != null)
            {
                foreach (Fielder fielder in fielders)
                {
                    if (fielder != null) fielder.ShowMarkerOnly();
                }
            }
        }

        if (isCharging && Input.GetMouseButton(0))
        {
            if (currentForce < maxForce)
            {
                currentForce += chargeSpeed * Time.deltaTime;
                if (currentForce > maxForce) currentForce = maxForce;
            }
            if (powerGauge != null) powerGauge.value = currentForce;

            ShowLandingPointAndProb();
        }

        // 発射！
        if (isCharging && Input.GetMouseButtonUp(0))
        {
            Fielder.isBallBounced = false;
            Fielder.isBallCaught = false;
            hasFinished = false;

            // ★修正：投げる瞬間に物理演算をONにする！
            rb.isKinematic = false;
            rb.useGravity = true;

            rb.velocity = Vector3.zero;
            rb.angularVelocity = Vector3.zero;

            rb.AddForce(launchDirection * currentForce, ForceMode.Impulse);
            rb.AddTorque(mainCamera.transform.right * -spinSpeed, ForceMode.Impulse);

            isCharging = false;
            currentForce = minForce;

            if (powerGauge != null) powerGauge.value = minForce;
            if (cameraScript != null) cameraScript.StartFollowing();

            if (fielders != null)
            {
                foreach (Fielder fielder in fielders)
                {
                    if (fielder != null) fielder.StartChasing();
                }
            }

            if (currentMarker != null)
            {
                currentMarker.SetActive(true);
            }

            isFlying = true;
            currentFlightTime = 0f;

            if (probText != null)
            {
                probText.text = "0%";
                probText.color = Color.white;
            }

            StartCoroutine(SendTimeRepeatedly());
        }
    }

    void ResetGame()
    {
        // ★修正：リセット時は物理演算をOFFにして完全に固定する
        rb.isKinematic = true;
        rb.useGravity = false;

        rb.velocity = Vector3.zero;
        rb.angularVelocity = Vector3.zero;

        transform.position = ballStartPos;
        transform.rotation = ballStartRot;

        if (cameraScript != null) cameraScript.ResetCamera();

        if (fielders != null)
        {
            foreach (Fielder fielder in fielders)
            {
                if (fielder != null) fielder.ResetFielder();
            }
        }

        if (currentMarker != null)
        {
            currentMarker.SetActive(false);
            currentMarker.transform.position = new Vector3(0, -100, 0);
        }

        isFlying = false;
        isCharging = false;
        hasFinished = false;
        Fielder.isBallCaught = false;
        Fielder.isBallBounced = false;
        StopAllCoroutines();

        if (probText != null)
        {
            probText.text = "Standby";
            probText.color = Color.white;
        }
    }

    void FixedUpdate()
    {
        // 飛んでるときだけ重力をかける
        if (rb.useGravity)
        {
            rb.AddForce(Vector3.down * extraGravity, ForceMode.Acceleration);
        }
    }

    void ShowLandingPointAndProb()
    {
        if (currentMarker == null) return;
        Vector3 virtualPos = transform.position;
        Vector3 virtualVel = (launchDirection * currentForce) / rb.mass;
        Vector3 totalGravity = Physics.gravity + Vector3.down * extraGravity;
        float timeStep = 0.02f;
        bool hitGround = false;
        float flightTime = 0f;

        for (int i = 0; i < 300; i++)
        {
            virtualVel += totalGravity * timeStep;
            Vector3 nextPos = virtualPos + virtualVel * timeStep;
            if (Physics.Linecast(virtualPos, nextPos, out RaycastHit hit))
            {
                currentMarker.SetActive(true);
                currentMarker.transform.position = hit.point + Vector3.up * 0.02f;
                currentMarker.transform.localScale = new Vector3(markerSize, 0.01f, markerSize);
                hitGround = true;

                flightTime = i * timeStep;
                predictedFlightTime = flightTime;

                if (fielders != null)
                {
                    foreach (Fielder fielder in fielders)
                    {
                        if (fielder != null)
                        {
                            fielder.SetLandingPoint(hit.point);
                        }
                    }
                }

                float currentWinRate = CalculateWinRateLocal(hit.point, flightTime);
                finalWinRate = currentWinRate;

                if (probText != null)
                {
                    probText.text = "解析中";
                    probText.color = Color.white;
                }
                break;
            }
            virtualPos = nextPos;
        }
        if (!hitGround)
        {
            currentMarker.SetActive(false);
            if (probText != null) probText.text = "Unknown";
        }
    }

    float CalculateWinRateLocal(Vector3 landingPoint, float flightTime)
    {
        if (fielders == null || fielders.Length == 0) return 0f;
        float maxProbability = 0f;
        float reactionDelay = 0.5f;
        if (flightTime < 1.0f) return 0f;

        foreach (Fielder fielder in fielders)
        {
            if (fielder == null) continue;
            float dist = Vector3.Distance(fielder.transform.position, landingPoint);
            float requiredDist = Mathf.Max(0, dist - fielder.catchRadius);
            float runTime = (requiredDist / fielder.runSpeed) + reactionDelay;

            float currentProbability = 0f;
            if (runTime <= flightTime) currentProbability = 100f;
            else currentProbability = (flightTime / runTime) * 100f;

            if (currentProbability > maxProbability) maxProbability = currentProbability;
        }
        return maxProbability;
    }

    IEnumerator SendTimeRepeatedly()
    {
        while (isFlying)
        {
            yield return new WaitForSeconds(0.1f);
            if (isFlying)
            {
                StartCoroutine(PostTime(currentFlightTime));
            }
        }
    }

    IEnumerator PostTime(float time)
    {
        float progress = time / predictedFlightTime;
        if (progress > 1.0f) progress = 1.0f;
        float sendingRate = finalWinRate * progress;

        TimeRequest myData = new TimeRequest();
        myData.time_elapsed = time;
        myData.win_rate = sendingRate;

        string json = JsonUtility.ToJson(myData);

        using (UnityWebRequest request = new UnityWebRequest(serverUrl, "POST"))
        {
            byte[] bodyRaw = Encoding.UTF8.GetBytes(json);
            request.uploadHandler = new UploadHandlerRaw(bodyRaw);
            request.downloadHandler = new DownloadHandlerBuffer();
            request.SetRequestHeader("Content-Type", "application/json");

            yield return request.SendWebRequest();

            if (request.result == UnityWebRequest.Result.Success)
            {
                string responseJson = request.downloadHandler.text;
                TimeRequest responseData = JsonUtility.FromJson<TimeRequest>(responseJson);

                if (probText != null)
                {
                    Color textColor = Color.red;

                    if (responseData.win_rate < 30) textColor = new Color(0.6f, 0.2f, 1.0f); // 紫
                    else if (responseData.win_rate < 80) textColor = Color.yellow;

                    probText.color = textColor;
                    probText.text = responseData.win_rate.ToString("F0") + "%";
                }

                if (fielders != null)
                {
                    foreach (Fielder fielder in fielders)
                    {
                        if (fielder != null) fielder.UpdateMarkerSize(time);
                    }
                }
            }
        }
    }
}