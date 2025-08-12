using UnityEngine;
using UnityEngine.UI;
using System.Collections;

public class GameStarter : MonoBehaviour
{
    private BossGameManager gameManager;
    private SpeechCombatSystem speechSystem;
    private Text sentenceText;
    private string[] testSentences = {"I am happy", "The cat is sleeping", "She likes apples", "He goes to school"};
    private int currentSentenceIndex = 0;
    
    void Start()
    {
        gameManager = FindFirstObjectByType<BossGameManager>();
        speechSystem = FindFirstObjectByType<SpeechCombatSystem>();
        sentenceText = FindFirstObjectByType<Text>();
        
        // 자동 게임 시작
        StartCoroutine(AutoGameFlow());
    }
    
    IEnumerator AutoGameFlow()
    {
        yield return new WaitForSeconds(2f);
        
        // 게임 시작
        if (gameManager != null)
        {
            Debug.Log("게임 자동 시작!");
            gameManager.StartBossGame();
        }
        
        yield return new WaitForSeconds(1f);
        
        // 자동으로 문장 표시 및 공격 테스트
        while (true)
        {
            // 문장 표시
            if (sentenceText != null)
            {
                string currentSentence = testSentences[currentSentenceIndex];
                sentenceText.text = currentSentence;
                Debug.Log("문장 표시: " + currentSentence);
            }
            
            yield return new WaitForSeconds(3f);
            
            // 자동 공격
            if (speechSystem != null)
            {
                string attackSentence = testSentences[currentSentenceIndex];
                Debug.Log("자동 공격: " + attackSentence);
                speechSystem.ProcessSpeechInput(attackSentence);
            }
            
            currentSentenceIndex = (currentSentenceIndex + 1) % testSentences.Length;
            yield return new WaitForSeconds(2f);
        }
    }
    
    void OnGUI()
    {
        GUI.Label(new Rect(10, 50, 500, 20), "자동 테스트 실행 중 - Input System 충돌 없음");
        GUI.Label(new Rect(10, 70, 500, 20), "현재 문장: " + (sentenceText ? sentenceText.text : "없음"));
    }
}