using UnityEngine;
using System.Collections;

public class NoLoopGame : MonoBehaviour
{
    [Header("무한루프 없는 게임")]
    public bool 안전모드 = true;
    
    private bool testComplete = false;
    
    void Start()
    {
        if (안전모드)
        {
            Debug.Log("🆕 무한루프 없는 안전한 게임 시작");
            StartCoroutine(OneTimeSetup());
        }
    }
    
    IEnumerator OneTimeSetup()
    {
        Debug.Log("1. WordDatabase 생성 중...");
        CreateWordDatabase();
        yield return new WaitForSeconds(0.5f);
        
        Debug.Log("2. 돌 생성 중...");
        CreateStones();
        yield return new WaitForSeconds(0.5f);
        
        Debug.Log("3. 설정 완료!");
        testComplete = true;
        
        Debug.Log("✅ 모든 설정 완료 - 무한루프 없음!");
        Debug.Log("1키: 파란돌 테스트, 2키: 빨간돌 테스트");
        
        // 코루틴 종료 - 더 이상 반복하지 않음
    }
    
    void CreateWordDatabase()
    {
        GameObject wordDB = new GameObject("WordDatabase");
        var db = wordDB.AddComponent<WordDatabase>();
        Debug.Log("✅ WordDatabase 생성 완료");
    }
    
    void CreateStones()
    {
        // 파란 돌
        GameObject blue = GameObject.CreatePrimitive(PrimitiveType.Cube);
        blue.name = "BlueStone";
        blue.transform.position = new Vector3(-2, 1, 0);
        blue.GetComponent<Renderer>().material.color = Color.blue;
        
        // 빨간 돌
        GameObject red = GameObject.CreatePrimitive(PrimitiveType.Cube);
        red.name = "RedStone";
        red.transform.position = new Vector3(2, 1, 0);
        red.GetComponent<Renderer>().material.color = Color.red;
        
        Debug.Log("✅ 돌 생성 완료");
    }
    
    void Update()
    {
        if (!testComplete) return;
        
        // 한 번만 실행되는 테스트
        if (Input.GetKeyDown(KeyCode.Alpha1))
        {
            Debug.Log("🔵 파란 돌 한 번 테스트");
            TestStoneOnce("BlueStone");
        }
        
        if (Input.GetKeyDown(KeyCode.Alpha2))
        {
            Debug.Log("🔴 빨간 돌 한 번 테스트");
            TestStoneOnce("RedStone");
        }
    }
    
    void TestStoneOnce(string stoneName)
    {
        GameObject stone = GameObject.Find(stoneName);
        if (stone != null)
        {
            // 단순한 크기 변화 - 루프 없음
            stone.transform.localScale = Vector3.one * 1.2f;
            
            // 0.5초 후 원래 크기로 - 한 번만 실행
            Invoke("ResetStone", 0.5f);
            
            Debug.Log($"✅ {stoneName} 테스트 완료 (한 번만)");
        }
    }
    
    void ResetStone()
    {
        // 모든 돌을 원래 크기로
        GameObject blue = GameObject.Find("BlueStone");
        GameObject red = GameObject.Find("RedStone");
        
        if (blue != null) blue.transform.localScale = Vector3.one;
        if (red != null) red.transform.localScale = Vector3.one;
        
        Debug.Log("✅ 돌 크기 리셋 완료 (한 번만)");
    }
}