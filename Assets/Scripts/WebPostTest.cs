using UnityEngine;
using UnityEngine.Networking;
using System.Collections;
using System.Text;

public class WebPostTest : MonoBehaviour
{
    // ★修正：サーバーの要求に合わせて変数名を「time_elapsed」に変更
    [System.Serializable]
    public class TimeRequest
    {
        public float time_elapsed;
    }

    void Start()
    {
        // テスト送信（例：15.5秒）
       // StartCoroutine(SendTime(15.5f));
    }

    IEnumerator SendTime(float time)
    {
        // ★注意：あなたがエラーを出したサーバーのURLをここに書いてください
        // (例: http://127.0.0.1:8000/items/ など)
        string url = "https://jsonplaceholder.typicode.com/posts";

        // 1. データ作成
        TimeRequest myData = new TimeRequest();

        // ★修正：変数名が変わったのでここも合わせる
        myData.time_elapsed = time;

        // 2. JSON変換
        // これで {"time_elapsed": 15.5} になります！
        string json = JsonUtility.ToJson(myData);

        Debug.Log("送信するデータ: " + json);

        // 3. POSTリクエスト作成
        using (UnityWebRequest request = new UnityWebRequest(url, "POST"))
        {
            byte[] bodyRaw = Encoding.UTF8.GetBytes(json);
            request.uploadHandler = new UploadHandlerRaw(bodyRaw);
            request.downloadHandler = new DownloadHandlerBuffer();
            request.SetRequestHeader("Content-Type", "application/json");

            // 4. 送信
            yield return request.SendWebRequest();

            // 5. 結果確認
            if (request.result != UnityWebRequest.Result.Success)
            {
                Debug.LogError("送信失敗...: " + request.error);
                // エラーの詳細理由を表示
                Debug.LogError("詳細な理由: " + request.downloadHandler.text);
            }
            else
            {
                Debug.Log("送信成功！");
                Debug.Log("サーバーからの返事: " + request.downloadHandler.text);
            }
        }
    }
}