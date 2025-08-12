using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.UI;

public class LunaGameSetup : MonoBehaviour
{
    [System.Serializable]
    public class SetupConfiguration
    {
        [Header("Auto Setup")]
        public bool autoSetupOnStart = true;
        public bool createMissingComponents = true;
        public bool configureForMetaQuest = true;

        [Header("Luna Configuration")]
        public bool enableLunaAI = true;
        public string llamaModelPath = "AI/llama-3.2-1b.gguf";
        public float lunaResponseDelay = 1.5f;
        public bool lunaDebugMode = true;

        [Header("Room Setup")]
        public bool preserveTwoRoomStructure = true;
        public Vector3 room1Position = new Vector3(-5, 0, 0);
        public Vector3 room2Position = new Vector3(5, 0, 0);
        public float roomSeparationDistance = 10f;

        [Header("Performance Optimization")]
        public bool enableMetaQuestOptimizations = true;
        public int maxConcurrentRequests = 1;
        public bool useResponseCaching = true;
        public int maxCacheSize = 50;

        [Header("Voice Integration")]
        public bool integrateWithExistingVoice = true;
        public float voiceTimeout = 15f;
        public bool enableKeyboardFallback = true;

        [Header("UI Configuration")]
        public bool createDebugUI = true;
        public bool showLunaStatus = true;
        public bool enableScoreDisplay = true;
    }

    [Header("Setup Configuration")]
    [SerializeField] private SetupConfiguration config = new SetupConfiguration();

    [Header("Prefab References")]
    [SerializeField] private GameObject magicBookPrefab;
    [SerializeField] private GameObject stonePrefab;
    [SerializeField] private GameObject pictureFramePrefab;
    [SerializeField] private GameObject lunaAvatarPrefab;

    [Header("Manual References")]
    [SerializeField] private VoiceRecognitionManager voiceRecognitionManager;
    [SerializeField] private Canvas gameUI;
    [SerializeField] private Camera mainCamera;

    [Header("Debug")]
    [SerializeField] private bool debugMode = true;

    private Dictionary<string, GameObject> createdObjects = new Dictionary<string, GameObject>();
    private bool setupComplete = false;

    private void Start()
    {
        if (config.autoSetupOnStart)
        {
            StartCoroutine(AutoSetup());
        }
    }

    public IEnumerator AutoSetup()
    {
        if (debugMode) Debug.Log("[LunaSetup] Starting automatic Luna game setup");

        yield return StartCoroutine(EnsureRequiredTags());
        yield return StartCoroutine(SetupCoreManagers());
        yield return StartCoroutine(SetupRoomStructure());
        yield return StartCoroutine(SetupLunaSystem());
        yield return StartCoroutine(SetupVoiceIntegration());
        yield return StartCoroutine(SetupUI());
        yield return StartCoroutine(FinalizeSetup());

        setupComplete = true;

        if (debugMode) Debug.Log("[LunaSetup] Setup complete! Ready to play with Luna.");
    }

    private IEnumerator EnsureRequiredTags()
    {
        if (debugMode) Debug.Log("[LunaSetup] Ensuring required tags exist");

        string[] requiredTags = new string[]
        {
            "Room1MagicBook",
            "Room1TextDisplay",
            "Room1Feedback",
            "Room2BlueStone",
            "Room2RedStone",
            "Room2PictureFrame",
            "Room2Feedback"
        };

#if UNITY_EDITOR
        // In editor, we can add tags programmatically
        UnityEditor.SerializedObject tagManager = new UnityEditor.SerializedObject(UnityEditor.AssetDatabase.LoadAllAssetsAtPath("ProjectSettings/TagManager.asset")[0]);
        UnityEditor.SerializedProperty tagsProp = tagManager.FindProperty("tags");

        foreach (string tag in requiredTags)
        {
            bool tagExists = false;
            for (int i = 0; i < tagsProp.arraySize; i++)
            {
                UnityEditor.SerializedProperty t = tagsProp.GetArrayElementAtIndex(i);
                if (t.stringValue.Equals(tag))
                {
                    tagExists = true;
                    break;
                }
            }

            if (!tagExists)
            {
                tagsProp.InsertArrayElementAtIndex(tagsProp.arraySize);
                UnityEditor.SerializedProperty newTagProp = tagsProp.GetArrayElementAtIndex(tagsProp.arraySize - 1);
                newTagProp.stringValue = tag;
                if (debugMode) Debug.Log($"[LunaSetup] Added tag: {tag}");
            }
        }

        tagManager.ApplyModifiedProperties();
#else
        // In build, just warn about missing tags
        foreach (string tag in requiredTags)
        {
            try
            {
                GameObject.FindGameObjectWithTag(tag);
            }
            catch (UnityException)
            {
                Debug.LogWarning($"[LunaSetup] Tag '{tag}' is not defined. Game may not work correctly.");
            }
        }
#endif

        yield return null;
    }

    private IEnumerator SetupCoreManagers()
    {
        if (debugMode) Debug.Log("[LunaSetup] Setting up core managers");

        // Create SinglePlayerWordGuessingManager
        GameObject managerObj = CreateOrFind("SinglePlayerWordGuessingManager");
        var gameManager = managerObj.GetComponent<SinglePlayerWordGuessingManager>();
        if (gameManager == null)
        {
            gameManager = managerObj.AddComponent<SinglePlayerWordGuessingManager>();
        }

        // Create WordDatabase
        GameObject databaseObj = CreateOrFind("WordDatabase");
        var wordDatabase = databaseObj.GetComponent<WordDatabase>();
        if (wordDatabase == null)
        {
            wordDatabase = databaseObj.AddComponent<WordDatabase>();
        }

        // Create AnswerValidator
        GameObject validatorObj = CreateOrFind("AnswerValidator");
        var answerValidator = validatorObj.GetComponent<AnswerValidator>();
        if (answerValidator == null)
        {
            answerValidator = validatorObj.AddComponent<AnswerValidator>();
        }

        // UI functionality is handled by Unity Canvas components directly

        yield return new WaitForSeconds(0.1f);
    }

    private IEnumerator SetupRoomStructure()
    {
        if (!config.preserveTwoRoomStructure)
        {
            yield break;
        }

        if (debugMode) Debug.Log("[LunaSetup] Setting up two-room structure");

        // Room 1 - Player's Room (Guesser)
        yield return StartCoroutine(SetupRoom1());

        // Room 2 - Luna's Room (Describer)
        yield return StartCoroutine(SetupRoom2());

        yield return new WaitForSeconds(0.1f);
    }

    private IEnumerator SetupRoom1()
    {
        // Create Room 1 container
        GameObject room1 = CreateOrFind("Room1_PlayerRoom");
        room1.transform.position = config.room1Position;

        // Magic Book for questions (simplified)
        GameObject magicBook = CreateInteractableObject("Room1_MagicBook", room1.transform, Vector3.zero);
        SafeSetTag(magicBook, "Room1MagicBook");

        // Magic book already has XRGrabInteractable from CreateInteractableObject
        // Remove any conflicting XRSimpleInteractable if it exists
        var simpleInteractable = magicBook.GetComponent<UnityEngine.XR.Interaction.Toolkit.Interactables.XRSimpleInteractable>();
        if (simpleInteractable != null)
        {
            DestroyImmediate(simpleInteractable);
        }

        // Text display for the magic book (use Unity UI Text)
        GameObject textDisplay = CreateOrFind("Room1_TextDisplay", room1.transform);
        textDisplay.transform.localPosition = new Vector3(0, 1, 0);
        SafeSetTag(textDisplay, "Room1TextDisplay");

        // Add Canvas and Text components
        Canvas canvas = textDisplay.GetComponent<Canvas>();
        if (canvas == null)
        {
            canvas = textDisplay.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.WorldSpace;
        }

        // Room lighting for feedback
        GameObject roomFeedback = CreateOrFind("Room1_Feedback", room1.transform);
        SafeSetTag(roomFeedback, "Room1Feedback");

        // Setup lighting for feedback
        Light roomLight = roomFeedback.GetComponentInChildren<Light>();
        if (roomLight == null)
        {
            GameObject lightObj = new GameObject("RoomLight");
            lightObj.transform.SetParent(roomFeedback.transform);
            lightObj.transform.localPosition = Vector3.up * 3;
            roomLight = lightObj.AddComponent<Light>();
            roomLight.type = LightType.Point;
            roomLight.range = 10f;
            roomLight.intensity = 1f;
        }

        yield return null;
    }
    private IEnumerator SetupRoom2()
    {
        // Create Room 2 container
        GameObject room2 = CreateOrFind("Room2_LunaRoom");
        room2.transform.position = config.room2Position;

        // Blue stone (Yes)
        GameObject blueStone = CreateInteractableObject("Room2_BlueStone", room2.transform, new Vector3(-1, 0.5f, 0));
        SafeSetTag(blueStone, "Room2BlueStone");

        var blueStoneInteraction = blueStone.GetComponent<FeedbackStoneInteraction>();
        if (blueStoneInteraction == null)
        {
            blueStoneInteraction = blueStone.AddComponent<FeedbackStoneInteraction>();
        }

        // Configure blue stone
        blueStoneInteraction.SetStoneType(FeedbackStoneInteraction.StoneType.Blue);

        // Get renderer from child mesh object if not on parent
        Renderer blueRenderer = blueStone.GetComponent<Renderer>();
        if (blueRenderer == null)
        {
            blueRenderer = blueStone.GetComponentInChildren<Renderer>();
        }
        if (blueRenderer != null)
        {
            blueRenderer.material.color = Color.blue;
        }
        else
        {
            Debug.LogWarning("[LunaSetup] No renderer found for blue stone");
        }

        // Red stone (No)
        GameObject redStone = CreateInteractableObject("Room2_RedStone", room2.transform, new Vector3(1, 0.5f, 0));
        SafeSetTag(redStone, "Room2RedStone");

        var redStoneInteraction = redStone.GetComponent<FeedbackStoneInteraction>();
        if (redStoneInteraction == null)
        {
            redStoneInteraction = redStone.AddComponent<FeedbackStoneInteraction>();
        }

        // Configure red stone
        redStoneInteraction.SetStoneType(FeedbackStoneInteraction.StoneType.Red);

        // Get renderer from child mesh object if not on parent
        Renderer redRenderer = redStone.GetComponent<Renderer>();
        if (redRenderer == null)
        {
            redRenderer = redStone.GetComponentInChildren<Renderer>();
        }
        if (redRenderer != null)
        {
            redRenderer.material.color = Color.red;
        }
        else
        {
            Debug.LogWarning("[LunaSetup] No renderer found for red stone");
        }

        // Picture frame for word display
        GameObject pictureFrame = CreateOrFind("Room2_PictureFrame", room2.transform);
        pictureFrame.transform.localPosition = new Vector3(0, 1.5f, 0);
        SafeSetTag(pictureFrame, "Room2PictureFrame");

        // Picture frame uses standard Unity Image components for display
        var imageComponent = pictureFrame.GetComponent<UnityEngine.UI.Image>();
        if (imageComponent == null && pictureFrame.GetComponent<Canvas>() == null)
        {
            Canvas canvas = pictureFrame.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.WorldSpace;
            imageComponent = pictureFrame.AddComponent<UnityEngine.UI.Image>();
        }

        // Room 2 feedback controller
        GameObject room2Feedback = CreateOrFind("Room2_Feedback", room2.transform);
        SafeSetTag(room2Feedback, "Room2Feedback");

        // Setup room lighting for stone feedback
        Light[] room2Lights = room2Feedback.GetComponentsInChildren<Light>();
        if (room2Lights.Length == 0)
        {
            GameObject lightObj = new GameObject("Room2Light");
            lightObj.transform.SetParent(room2Feedback.transform);
            lightObj.transform.localPosition = Vector3.up * 3;
            Light roomLight = lightObj.AddComponent<Light>();
            roomLight.type = LightType.Point;
            roomLight.range = 10f;
            roomLight.intensity = 1f;
            room2Lights = new Light[] { roomLight };
        }

        // Link stones to room lighting
        blueStoneInteraction.SetRoomLights(room2Lights);
        redStoneInteraction.SetRoomLights(room2Lights);

        yield return null;
    }

    private IEnumerator SetupLunaSystem()
    {
        if (!config.enableLunaAI)
        {
            yield break;
        }

        if (debugMode) Debug.Log("[LunaSetup] Setting up Luna AI system");

        // Create Luna NPC Controller
        GameObject lunaObj = CreateOrFind("LunaNPCController");
        var lunaController = lunaObj.GetComponent<LunaNPCController>();
        if (lunaController == null)
        {
            lunaController = lunaObj.AddComponent<LunaNPCController>();
        }

        // Position Luna in Room 2
        if (config.preserveTwoRoomStructure)
        {
            lunaObj.transform.position = config.room2Position + Vector3.up;
        }

        // Create Luna avatar if prefab is provided
        if (lunaAvatarPrefab != null)
        {
            GameObject avatar = Instantiate(lunaAvatarPrefab, lunaObj.transform);
            avatar.name = "LunaAvatar";
        }
        else
        {
            // Create simple avatar representation
            GameObject avatar = CreateSimpleLunaAvatar(lunaObj.transform);
        }

        // Ensure StreamingAssets folder exists for AI model
        CreateStreamingAssetsStructure();

        yield return new WaitForSeconds(0.5f);
    }

    private GameObject CreateSimpleLunaAvatar(Transform parent)
    {
        GameObject avatar = new GameObject("LunaAvatar");
        avatar.transform.SetParent(parent);
        avatar.transform.localPosition = Vector3.zero;

        // Create simple capsule representation
        GameObject body = GameObject.CreatePrimitive(PrimitiveType.Capsule);
        body.transform.SetParent(avatar.transform);
        body.transform.localPosition = Vector3.zero;
        body.transform.localScale = new Vector3(0.5f, 1f, 0.5f);
        body.name = "LunaBody";

        // Add glowing effect
        var renderer = body.GetComponent<Renderer>();
        renderer.material.color = new Color(0.8f, 0.9f, 1f, 0.8f);
        renderer.material.SetFloat("_Mode", 3); // Transparent mode
        renderer.material.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
        renderer.material.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);

        // Add thinking indicator
        GameObject thinkingIndicator = new GameObject("ThinkingIndicator");
        thinkingIndicator.transform.SetParent(avatar.transform);
        thinkingIndicator.transform.localPosition = Vector3.up * 2f;

        // Add floating animation
        var floatingScript = avatar.AddComponent<SimpleFloatingAnimation>();

        return avatar;
    }

    private void CreateStreamingAssetsStructure()
    {
        string streamingAssetsPath = Application.streamingAssetsPath;
        string aiPath = System.IO.Path.Combine(streamingAssetsPath, "AI");

        if (!System.IO.Directory.Exists(streamingAssetsPath))
        {
            System.IO.Directory.CreateDirectory(streamingAssetsPath);
        }

        if (!System.IO.Directory.Exists(aiPath))
        {
            System.IO.Directory.CreateDirectory(aiPath);

            // Create a readme file with instructions
            string readmePath = System.IO.Path.Combine(aiPath, "README.txt");
            string readmeContent = "Place your llama-3.2-1b.gguf model file in this folder.\n" +
                                 "Download from: https://huggingface.co/microsoft/Llama-3.2-1B-Instruct-GGUF\n" +
                                 "The system will work with fallback responses if the model is not present.";

            System.IO.File.WriteAllText(readmePath, readmeContent);
        }
    }

    private IEnumerator SetupVoiceIntegration()
    {
        if (!config.integrateWithExistingVoice)
        {
            yield break;
        }

        if (debugMode) Debug.Log("[LunaSetup] Setting up voice integration");

        // Connect to existing voice recognition manager or create one
        if (voiceRecognitionManager == null)
        {
            voiceRecognitionManager = FindFirstObjectByType<VoiceRecognitionManager>();
        }

        if (voiceRecognitionManager != null)
        {
            if (debugMode) Debug.Log("[LunaSetup] Voice recognition manager found - voice integration ready");
        }
        else
        {
            if (debugMode) Debug.Log("[LunaSetup] Voice recognition manager not found - creating one");

            // Create a VoiceRecognitionManager
            GameObject voiceManager = new GameObject("VoiceRecognitionManager");
            voiceRecognitionManager = voiceManager.AddComponent<VoiceRecognitionManager>();

            if (debugMode) Debug.Log("[LunaSetup] Voice recognition manager created successfully");
        }

        yield return null;
    }

    private IEnumerator SetupUI()
    {
        if (!config.createDebugUI)
        {
            yield break;
        }

        if (debugMode) Debug.Log("[LunaSetup] Setting up UI");

        // Find or create main canvas
        if (gameUI == null)
        {
            gameUI = FindFirstObjectByType<Canvas>();
        }

        if (gameUI == null)
        {
            GameObject canvasObj = new GameObject("GameCanvas");
            gameUI = canvasObj.AddComponent<Canvas>();
            gameUI.renderMode = RenderMode.WorldSpace;

            // Position UI in Room 1
            canvasObj.transform.position = config.room1Position + Vector3.forward * 2;
            canvasObj.transform.localScale = Vector3.one * 0.01f;

            // Add CanvasScaler and GraphicRaycaster
            canvasObj.AddComponent<CanvasScaler>();
            canvasObj.AddComponent<GraphicRaycaster>();
        }

        // Create debug panel
        CreateDebugUI();

        yield return null;
    }

    private void CreateDebugUI()
    {
        GameObject debugPanel = new GameObject("DebugPanel");
        debugPanel.transform.SetParent(gameUI.transform, false);

        var rectTransform = debugPanel.AddComponent<RectTransform>();
        rectTransform.anchorMin = new Vector2(0, 0.7f);
        rectTransform.anchorMax = new Vector2(0.3f, 1f);
        rectTransform.offsetMin = Vector2.zero;
        rectTransform.offsetMax = Vector2.zero;

        var image = debugPanel.AddComponent<Image>();
        image.color = new Color(0, 0, 0, 0.7f);

        // Add text components for status display
        CreateStatusText("Luna Status: Initializing...", debugPanel.transform, Vector2.zero);
        CreateStatusText("Game Phase: Waiting", debugPanel.transform, new Vector2(0, -30));
        CreateStatusText("Score: 0", debugPanel.transform, new Vector2(0, -60));

        if (config.enableKeyboardFallback)
        {
            CreateStatusText("Keyboard: 1,2,3 for test questions", debugPanel.transform, new Vector2(0, -90));
        }
    }

    private void CreateStatusText(string text, Transform parent, Vector2 position)
    {
        GameObject textObj = new GameObject("StatusText");
        textObj.transform.SetParent(parent, false);

        var rectTransform = textObj.AddComponent<RectTransform>();
        rectTransform.anchoredPosition = position;
        rectTransform.sizeDelta = new Vector2(200, 25);

        var textComponent = textObj.AddComponent<Text>();
        textComponent.text = text;
        textComponent.color = Color.white;
        textComponent.fontSize = 12;
        textComponent.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
    }

    private IEnumerator FinalizeSetup()
    {
        if (debugMode) Debug.Log("[LunaSetup] Finalizing setup");

        // Apply Meta Quest optimizations
        if (config.enableMetaQuestOptimizations)
        {
            ApplyMetaQuestOptimizations();
        }

        // Validate all components are properly connected
        ValidateSetup();

        yield return new WaitForSeconds(0.5f);

        // Start the game if everything is ready
        var gameManager = FindFirstObjectByType<SinglePlayerWordGuessingManager>();
        if (gameManager != null)
        {
            gameManager.StartNewGame();
        }
    }

    private void ApplyMetaQuestOptimizations()
    {
        // Reduce rendering quality for performance
        QualitySettings.SetQualityLevel(2, true); // Medium quality

        // Optimize physics settings
        Physics.defaultContactOffset = 0.01f;
        Physics.sleepThreshold = 0.005f;

        // Set target frame rate
        Application.targetFrameRate = 72; // Meta Quest 2 refresh rate

        if (debugMode) Debug.Log("[LunaSetup] Meta Quest optimizations applied");
    }

    private void ValidateSetup()
    {
        // Check all required components exist
        var gameManager = FindFirstObjectByType<SinglePlayerWordGuessingManager>();
        var lunaController = FindFirstObjectByType<LunaNPCController>();
        var wordDatabase = FindFirstObjectByType<WordDatabase>();

        bool valid = gameManager != null && lunaController != null && wordDatabase != null;

        if (valid)
        {
            if (debugMode) Debug.Log("[LunaSetup] Setup validation passed");
        }
        else
        {
            Debug.LogError("[LunaSetup] Setup validation failed - missing components");
        }
    }

    private GameObject CreateOrFind(string name, Transform parent = null)
    {
        GameObject obj = GameObject.Find(name);
        if (obj == null)
        {
            obj = new GameObject(name);
            if (parent != null)
            {
                obj.transform.SetParent(parent);
            }
            createdObjects[name] = obj;
        }
        return obj;
    }

    private GameObject CreateInteractableObject(string name, Transform parent, Vector3 localPosition)
    {
        GameObject obj = CreateOrFind(name, parent);
        obj.transform.localPosition = localPosition;

        // Add primitive shape if no mesh exists
        if (obj.GetComponent<MeshRenderer>() == null)
        {
            var primitive = GameObject.CreatePrimitive(PrimitiveType.Cube);
            primitive.transform.SetParent(obj.transform);
            primitive.transform.localPosition = Vector3.zero;
            primitive.transform.localScale = Vector3.one * 0.5f;
            primitive.name = name + "_Mesh";
        }

        // Ensure collider exists
        if (obj.GetComponent<Collider>() == null)
        {
            obj.AddComponent<BoxCollider>();
        }

        // Add XR grab interactable
        if (obj.GetComponent<UnityEngine.XR.Interaction.Toolkit.Interactables.XRGrabInteractable>() == null)
        {
            obj.AddComponent<UnityEngine.XR.Interaction.Toolkit.Interactables.XRGrabInteractable>();
        }

        return obj;
    }

    private void SafeSetTag(GameObject obj, string tagName)
    {
        try
        {
            obj.tag = tagName;
        }
        catch (UnityException)
        {
            Debug.LogWarning($"[LunaSetup] Tag '{tagName}' is not defined. Please add it in the TagManager or Unity's Tag Manager.");
        }
    }

    [ContextMenu("Setup Luna Game")]
    public void ManualSetup()
    {
        StartCoroutine(AutoSetup());
    }

    [ContextMenu("Reset Setup")]
    public void ResetSetup()
    {
        foreach (var kvp in createdObjects)
        {
            if (kvp.Value != null)
            {
                DestroyImmediate(kvp.Value);
            }
        }

        createdObjects.Clear();
        setupComplete = false;

        if (debugMode) Debug.Log("[LunaSetup] Setup reset");
    }

    public bool IsSetupComplete()
    {
        return setupComplete;
    }

    public SetupConfiguration GetConfiguration()
    {
        return config;
    }
    private void ProtectCameraSettings()
    {
        var mainCamera = Camera.main;
        if (mainCamera != null)
        {
            // 원하는 배경색으로 강제 설정
            mainCamera.clearFlags = CameraClearFlags.SolidColor;
            mainCamera.backgroundColor = new Color(0.1f, 0.1f, 0.44f, 1f); // 어두운 파란색
            
            if (debugMode) 
                Debug.Log("[LunaSetup] Camera background protected");
        }
    }
}

// Simple floating animation component for Luna avatar
public class SimpleFloatingAnimation : MonoBehaviour
{
    public float amplitude = 0.2f;
    public float frequency = 1f;

    private Vector3 originalPosition;

    private void Start()
    {
        originalPosition = transform.localPosition;
    }

    private void Update()
    {
        float newY = originalPosition.y + Mathf.Sin(Time.time * frequency) * amplitude;
        transform.localPosition = new Vector3(originalPosition.x, newY, originalPosition.z);
    }
}
