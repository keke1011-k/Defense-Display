using UnityEngine;
using UnityEngine.Networking; 
using System.Collections;

public class WebReqTest : MonoBehaviour
{
    void Start()
    {
        // コルーチンを開始
        StartCoroutine(GetData());
    }

    IEnumerator GetData()
    {
        // テスト用のURL（ダミーデータを返してくれるサイトです）
        string url = "https://jsonplaceholder.typicode.com/todos/1";

        Debug.Log("通信開始...");

        // GETリクエストを作成
        using (UnityWebRequest webRequest = UnityWebRequest.Get(url))
        {
            // 送信して、終わるまで待機 (yield return)
            yield return webRequest.SendWebRequest();

            // エラーチェック
            if (webRequest.result != UnityWebRequest.Result.Success)
            {
                Debug.LogError("エラー発生: " + webRequest.error);
            }
            else
            {
                // 文字列(JSON)を取得
                string jsonText = webRequest.downloadHandler.text;

                // ★ここで変換！ JSON -> C#のクラス
                MyData data = JsonUtility.FromJson<MyData>(jsonText);

                // これで変数として使えるようになります！
                Debug.Log("変換成功！");
                Debug.Log("タイトル: " + data.title); // タイトルだけ取り出せる
                Debug.Log("ID: " + data.id);          // IDだけ取り出せる
            }
        }
    }
}

// 受け取るデータの形に合わせたクラス
[System.Serializable] // ★これ重要！
public class MyData
{
    public int userId;
    public int id;
    public string title;
    public bool completed;
}