using UnityEngine;
using System.IO;
using System.Collections.Generic;

public class ProjectAnalyzer : MonoBehaviour
{
    [System.Serializable]
    public class ProjectInfo
    {
        public List<string> sttScripts = new List<string>();
        public List<string> ttsScripts = new List<string>();
        public List<string> networkScripts = new List<string>();
        public List<string> vrScripts = new List<string>();
        public List<string> gameManagerScripts = new List<string>();
        public List<string> playerScripts = new List<string>();
    }
    
    [Header("분석 결과")]
    [SerializeField] private ProjectInfo projectInfo = new ProjectInfo();
    
    [ContextMenu("프로젝트 분석")]
    public void AnalyzeProject()
    {
        ClearResults();
        
        // Scripts 폴더의 모든 C# 파일 검색
        string[] scriptFiles = Directory.GetFiles(Application.dataPath, "*.cs", SearchOption.AllDirectories);
        
        foreach (string filePath in scriptFiles)
        {
            string fileName = Path.GetFileNameWithoutExtension(filePath);
            string fileContent = File.ReadAllText(filePath);
            
            // STT 관련 스크립트 찾기
            if (ContainsKeywords(fileContent, new string[] { "STT", "SpeechToText", "VoiceRecognition", "microphone", "speech" }))
            {
                projectInfo.sttScripts.Add(fileName);
            }
            
            // TTS 관련 스크립트 찾기
            if (ContainsKeywords(fileContent, new string[] { "TTS", "TextToSpeech", "VoiceOutput", "AudioSource" }))
            {
                projectInfo.ttsScripts.Add(fileName);
            }
            
            // 네트워크 관련 스크립트 찾기
            if (ContainsKeywords(fileContent, new string[] { "Photon", "Mirror", "NetworkManager", "RPC", "multiplayer" }))
            {
                projectInfo.networkScripts.Add(fileName);
            }
            
            // VR 관련 스크립트 찾기
            if (ContainsKeywords(fileContent, new string[] { "XR", "VR", "Oculus", "OpenXR", "GrabInteractable" }))
            {
                projectInfo.vrScripts.Add(fileName);
            }
            
            // GameManager 관련 스크립트 찾기
            if (ContainsKeywords(fileContent, new string[] { "GameManager", "GameController", "LevelManager" }))
            {
                projectInfo.gameManagerScripts.Add(fileName);
            }
            
            // Player 관련 스크립트 찾기
            if (ContainsKeywords(fileContent, new string[] { "Player", "Character", "VRPlayer" }))
            {
                projectInfo.playerScripts.Add(fileName);
            }
        }
        
        // 결과 출력
        PrintResults();
    }
    
    private bool ContainsKeywords(string content, string[] keywords)
    {
        foreach (string keyword in keywords)
        {
            if (content.Contains(keyword))
                return true;
        }
        return false;
    }
    
    private void ClearResults()
    {
        projectInfo.sttScripts.Clear();
        projectInfo.ttsScripts.Clear();
        projectInfo.networkScripts.Clear();
        projectInfo.vrScripts.Clear();
        projectInfo.gameManagerScripts.Clear();
        projectInfo.playerScripts.Clear();
    }
    
    private void PrintResults()
    {
        Debug.Log("=== 프로젝트 분석 결과 ===");
        
        Debug.Log("STT 관련 스크립트:");
        foreach (string script in projectInfo.sttScripts)
            Debug.Log("  - " + script);
        
        Debug.Log("TTS 관련 스크립트:");
        foreach (string script in projectInfo.ttsScripts)
            Debug.Log("  - " + script);
        
        Debug.Log("네트워크 관련 스크립트:");
        foreach (string script in projectInfo.networkScripts)
            Debug.Log("  - " + script);
        
        Debug.Log("VR 관련 스크립트:");
        foreach (string script in projectInfo.vrScripts)
            Debug.Log("  - " + script);
        
        Debug.Log("GameManager 관련 스크립트:");
        foreach (string script in projectInfo.gameManagerScripts)
            Debug.Log("  - " + script);
        
        Debug.Log("Player 관련 스크립트:");
        foreach (string script in projectInfo.playerScripts)
            Debug.Log("  - " + script);
    }
}