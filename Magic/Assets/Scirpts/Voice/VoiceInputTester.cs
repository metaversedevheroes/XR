using UnityEngine;
using UnityEngine.UI;

public class VoiceInputTester : MonoBehaviour
{
    [Header("UI Elements")]
    [SerializeField] private InputField voiceInputField;
    [SerializeField] private Button speakButton;
    [SerializeField] private Button clearButton;
    [SerializeField] private Text statusText;
    [SerializeField] private Text historyText;
    
    [Header("Settings")]
    [SerializeField] private bool autoCreateUI = true;
    [SerializeField] private float confidenceOverride = 0.95f;
    
    private VoiceRecognitionManager voiceManager;
    private string inputHistory = "";

    void Start()
    {
        voiceManager = FindFirstObjectByType<VoiceRecognitionManager>();
        
        if (autoCreateUI)
        {
            CreateVoiceInputUI();
        }
        
        SetupUI();
    }

    private void CreateVoiceInputUI()
    {
        // Create Canvas if none exists
        Canvas canvas = FindFirstObjectByType<Canvas>();
        if (canvas == null)
        {
            GameObject canvasObj = new GameObject("VoiceInputCanvas");
            canvas = canvasObj.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvasObj.AddComponent<CanvasScaler>();
            canvasObj.AddComponent<GraphicRaycaster>();
        }

        // Create main panel
        GameObject panel = new GameObject("VoiceInputPanel");
        panel.transform.SetParent(canvas.transform, false);
        
        var image = panel.AddComponent<Image>();
        image.color = new Color(0, 0, 0, 0.8f);
        
        var rectTransform = panel.GetComponent<RectTransform>();
        rectTransform.anchorMin = new Vector2(0.1f, 0.1f);
        rectTransform.anchorMax = new Vector2(0.9f, 0.4f);
        rectTransform.offsetMin = Vector2.zero;
        rectTransform.offsetMax = Vector2.zero;

        // Create title
        CreateTextElement("Title", panel.transform, "Voice Input Tester", new Vector2(0, 80), 20);

        // Create status text
        GameObject statusObj = CreateTextElement("Status", panel.transform, "Ready to speak...", new Vector2(0, 50), 14);
        statusText = statusObj.GetComponent<Text>();
        statusText.color = Color.green;

        // Create input field
        GameObject inputObj = new GameObject("VoiceInput");
        inputObj.transform.SetParent(panel.transform, false);
        
        var inputRect = inputObj.AddComponent<RectTransform>();
        inputRect.anchoredPosition = new Vector2(0, 10);
        inputRect.sizeDelta = new Vector2(500, 30);
        
        var inputImage = inputObj.AddComponent<Image>();
        inputImage.color = Color.white;
        
        voiceInputField = inputObj.AddComponent<InputField>();
        voiceInputField.placeholder = CreateTextElement("Placeholder", inputObj.transform, "Type what you want to say...", Vector2.zero, 14).GetComponent<Text>();
        voiceInputField.placeholder.color = Color.gray;
        voiceInputField.textComponent = CreateTextElement("Text", inputObj.transform, "", Vector2.zero, 14).GetComponent<Text>();
        voiceInputField.textComponent.color = Color.black;

        // Create buttons
        GameObject speakButtonObj = CreateButton("SpeakButton", panel.transform, "SPEAK", new Vector2(-100, -30), () => SimulateVoiceInput());
        speakButton = speakButtonObj.GetComponent<Button>();
        
        GameObject clearButtonObj = CreateButton("ClearButton", panel.transform, "Clear", new Vector2(100, -30), () => ClearInput());
        clearButton = clearButtonObj.GetComponent<Button>();

        // Create history
        GameObject historyObj = CreateTextElement("History", panel.transform, "", new Vector2(0, -70), 12);
        historyText = historyObj.GetComponent<Text>();
        historyText.color = Color.yellow;
    }

    private GameObject CreateTextElement(string name, Transform parent, string text, Vector2 position, int fontSize)
    {
        GameObject textObj = new GameObject(name);
        textObj.transform.SetParent(parent, false);
        
        var rectTransform = textObj.AddComponent<RectTransform>();
        rectTransform.anchoredPosition = position;
        rectTransform.sizeDelta = new Vector2(600, fontSize + 10);
        
        var textComponent = textObj.AddComponent<Text>();
        textComponent.text = text;
        textComponent.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        textComponent.fontSize = fontSize;
        textComponent.color = Color.white;
        textComponent.alignment = TextAnchor.MiddleCenter;
        
        return textObj;
    }

    private GameObject CreateButton(string name, Transform parent, string text, Vector2 position, System.Action onClick)
    {
        GameObject buttonObj = new GameObject(name);
        buttonObj.transform.SetParent(parent, false);
        
        var rectTransform = buttonObj.AddComponent<RectTransform>();
        rectTransform.anchoredPosition = position;
        rectTransform.sizeDelta = new Vector2(120, 40);
        
        var image = buttonObj.AddComponent<Image>();
        image.color = new Color(0.2f, 0.6f, 1f, 0.8f);
        
        var button = buttonObj.AddComponent<Button>();
        button.onClick.AddListener(() => onClick());
        
        // Button text
        GameObject textObj = new GameObject("Text");
        textObj.transform.SetParent(buttonObj.transform, false);
        
        var textRect = textObj.AddComponent<RectTransform>();
        textRect.anchorMin = Vector2.zero;
        textRect.anchorMax = Vector2.one;
        textRect.offsetMin = Vector2.zero;
        textRect.offsetMax = Vector2.zero;
        
        var textComponent = textObj.AddComponent<Text>();
        textComponent.text = text;
        textComponent.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        textComponent.fontSize = 12;
        textComponent.color = Color.white;
        textComponent.alignment = TextAnchor.MiddleCenter;
        
        return buttonObj;
    }

    private void SetupUI()
    {
        if (voiceInputField != null)
        {
            voiceInputField.onEndEdit.AddListener((text) => {
                if (Input.GetKeyDown(KeyCode.Return) || Input.GetKeyDown(KeyCode.KeypadEnter))
                {
                    SimulateVoiceInput();
                }
            });
        }
    }

    private void SimulateVoiceInput()
    {
        if (voiceInputField == null || string.IsNullOrWhiteSpace(voiceInputField.text))
        {
            UpdateStatus("Please enter some text first!", Color.red);
            return;
        }

        string inputText = voiceInputField.text.Trim();
        
        // Simulate voice recognition
        if (voiceManager != null)
        {
            // Trigger the voice recognition event
            VoiceRecognitionEvents.TriggerVoiceRecognized(inputText, confidenceOverride);
            
            // Update UI
            UpdateStatus($"Spoke: \"{inputText}\"", Color.green);
            AddToHistory(inputText);
            
            Debug.Log($"[VoiceInputTester] Simulated voice input: '{inputText}' (Confidence: {confidenceOverride:F2})");
        }
        else
        {
            UpdateStatus("VoiceRecognitionManager not found!", Color.red);
            Debug.LogError("[VoiceInputTester] VoiceRecognitionManager not found!");
        }
        
        // Clear input field
        voiceInputField.text = "";
    }

    private void ClearInput()
    {
        if (voiceInputField != null)
        {
            voiceInputField.text = "";
        }
        
        inputHistory = "";
        if (historyText != null)
        {
            historyText.text = "";
        }
        
        UpdateStatus("Cleared", Color.yellow);
    }

    private void UpdateStatus(string message, Color color)
    {
        if (statusText != null)
        {
            statusText.text = message;
            statusText.color = color;
        }
        
        // Auto clear status after 3 seconds
        Invoke("ClearStatus", 3f);
    }

    private void ClearStatus()
    {
        if (statusText != null)
        {
            statusText.text = "Ready to speak...";
            statusText.color = Color.green;
        }
    }

    private void AddToHistory(string text)
    {
        inputHistory = $"Last: \"{text}\"\n" + inputHistory;
        
        // Keep only last 3 entries
        string[] lines = inputHistory.Split('\n');
        if (lines.Length > 3)
        {
            inputHistory = string.Join("\n", lines, 0, 3);
        }
        
        if (historyText != null)
        {
            historyText.text = inputHistory;
        }
    }

    void Update()
    {
        // Hotkeys
        if (Input.GetKeyDown(KeyCode.F1))
        {
            SimulateVoiceInput();
        }
        
        if (Input.GetKeyDown(KeyCode.F2))
        {
            ClearInput();
        }
    }

    void OnGUI()
    {
        // Quick test buttons
        GUILayout.BeginArea(new Rect(10, 10, 200, 300));
        GUILayout.Label("Quick Voice Tests:");
        
        if (GUILayout.Button("Fire"))
            TestWord("Fire");
        if (GUILayout.Button("Light")) 
            TestWord("Light");
        if (GUILayout.Button("Apple"))
            TestWord("Apple");
        if (GUILayout.Button("Dog"))
            TestWord("Dog");
        if (GUILayout.Button("School"))
            TestWord("School");
        if (GUILayout.Button("Hello Luna"))
            TestWord("Hello Luna");
        if (GUILayout.Button("Yes"))
            TestWord("Yes");
        if (GUILayout.Button("No"))
            TestWord("No");
            
        GUILayout.EndArea();
    }

    private void TestWord(string word)
    {
        if (voiceManager != null)
        {
            VoiceRecognitionEvents.TriggerVoiceRecognized(word, confidenceOverride);
            Debug.Log($"[VoiceInputTester] Quick test: '{word}'");
            
            if (statusText != null)
            {
                UpdateStatus($"🎤 Quick test: \"{word}\"", Color.cyan);
            }
        }
    }
}