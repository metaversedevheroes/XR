using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;
using System.Collections;

public class MagicBookTextDisplay : MonoBehaviour
{
    [System.Serializable]
    public class MessageEntry
    {
        public string message;
        public int senderId;
        public float timestamp;
        public MessageType type;
        
        public MessageEntry(string msg, int sender, MessageType msgType)
        {
            message = msg;
            senderId = sender;
            timestamp = Time.time;
            type = msgType;
        }
    }
    
    public enum MessageType
    {
        Question,
        Guess,
        System,
        Feedback
    }
    
    [Header("UI Components")]
    [SerializeField] private TextMeshProUGUI mainDisplayText;
    [SerializeField] private ScrollRect scrollRect;
    [SerializeField] private Transform messageContainer;
    [SerializeField] private GameObject messageTemplate;
    
    [Header("Text Animation")]
    [SerializeField] private bool useTypewriterEffect = true;
    [SerializeField] private float typewriterSpeed = 0.05f;
    [SerializeField] private bool autoScrollToBottom = true;
    
    [Header("Message History")]
    [SerializeField] private int maxMessages = 50;
    [SerializeField] private bool showTimestamps = false;
    [SerializeField] private bool showSenderNames = true;
    
    [Header("Visual Styling")]
    [SerializeField] private Color questionColor = Color.white;
    [SerializeField] private Color guessColor = Color.yellow;
    [SerializeField] private Color systemColor = Color.cyan;
    [SerializeField] private Color feedbackColor = Color.green;
    
    [Header("Player Assignment")]
    [SerializeField] private int assignedPlayerId = 1;
    [SerializeField] private bool receiveAllMessages = true;
    
    [Header("Debug")]
    [SerializeField] private bool debugMode = true;
    
    private List<MessageEntry> messageHistory = new List<MessageEntry>();
    private Coroutine typewriterCoroutine;
    private bool isDisplayingMessage = false;
    
    private void Awake()
    {
        InitializeComponents();
    }
    
    private void OnEnable()
    {
        WordGuessingVoiceHandler.OnQuestionAsked += HandleQuestionAsked;
        WordGuessingVoiceHandler.OnGuessAttempted += HandleGuessAttempted;
        WordGuessingGameManager.OnGameMessage += HandleSystemMessage;
        PlayerRoleManager.OnPlayerDataUpdated += HandlePlayerDataUpdated;
    }
    
    private void OnDisable()
    {
        WordGuessingVoiceHandler.OnQuestionAsked -= HandleQuestionAsked;
        WordGuessingVoiceHandler.OnGuessAttempted -= HandleGuessAttempted;
        WordGuessingGameManager.OnGameMessage -= HandleSystemMessage;
        PlayerRoleManager.OnPlayerDataUpdated -= HandlePlayerDataUpdated;
    }
    
    private void InitializeComponents()
    {
        if (mainDisplayText == null)
            mainDisplayText = GetComponentInChildren<TextMeshProUGUI>();
        
        if (scrollRect == null)
            scrollRect = GetComponentInChildren<ScrollRect>();
        
        if (messageContainer == null && scrollRect != null)
            messageContainer = scrollRect.content;
        
        if (messageTemplate != null)
            messageTemplate.SetActive(false);
        
        if (mainDisplayText != null)
        {
            mainDisplayText.text = "Magic Book ready for communication...";
        }
        
        if (debugMode)
        {
            Debug.Log($"MagicBookTextDisplay initialized for Player {assignedPlayerId}");
        }
    }
    
    private void HandleQuestionAsked(int playerId, string question)
    {
        if (!ShouldReceiveMessage(playerId)) return;
        
        string formattedMessage = FormatMessage(playerId, question, MessageType.Question);
        AddMessage(formattedMessage, playerId, MessageType.Question);
        
        if (debugMode)
        {
            Debug.Log($"Question displayed on Player {assignedPlayerId}'s magic book: {question}");
        }
    }
    
    private void HandleGuessAttempted(int playerId, string guess)
    {
        if (!ShouldReceiveMessage(playerId)) return;
        
        string formattedMessage = FormatMessage(playerId, $"Guess: {guess}", MessageType.Guess);
        AddMessage(formattedMessage, playerId, MessageType.Guess);
        
        if (debugMode)
        {
            Debug.Log($"Guess displayed on Player {assignedPlayerId}'s magic book: {guess}");
        }
    }
    
    private void HandleSystemMessage(string message)
    {
        AddMessage(message, -1, MessageType.System);
        
        if (debugMode)
        {
            Debug.Log($"System message displayed: {message}");
        }
    }
    
    private void HandlePlayerDataUpdated(PlayerRoleManager.PlayerData playerData)
    {
        // Update display based on player role changes if needed
        if (playerData.playerId == assignedPlayerId)
        {
            UpdateDisplayForRole(playerData.currentRole);
        }
    }
    
    private bool ShouldReceiveMessage(int senderId)
    {
        if (receiveAllMessages) return true;
        
        // Only receive messages from other players (cross-room communication)
        return senderId != assignedPlayerId;
    }
    
    private string FormatMessage(int senderId, string message, MessageType type)
    {
        string senderName = GetPlayerName(senderId);
        string prefix = "";
        
        if (showSenderNames && senderId != -1)
        {
            prefix = $"[{senderName}] ";
        }
        
        if (showTimestamps)
        {
            string timestamp = System.DateTime.Now.ToString("HH:mm:ss");
            prefix = $"[{timestamp}] " + prefix;
        }
        
        return prefix + message;
    }
    
    private string GetPlayerName(int playerId)
    {
        if (PlayerRoleManager.Instance != null)
        {
            var playerData = PlayerRoleManager.Instance.GetPlayerData(playerId);
            if (playerData != null)
                return playerData.playerName;
        }
        
        return $"Player {playerId}";
    }
    
    private void AddMessage(string message, int senderId, MessageType type)
    {
        MessageEntry entry = new MessageEntry(message, senderId, type);
        messageHistory.Add(entry);
        
        // Limit message history
        if (messageHistory.Count > maxMessages)
        {
            messageHistory.RemoveAt(0);
        }
        
        DisplayMessage(message, type);
        
        if (messageContainer != null && messageTemplate != null)
        {
            CreateMessageUI(entry);
        }
    }
    
    private void DisplayMessage(string message, MessageType type)
    {
        if (mainDisplayText == null) return;
        
        if (typewriterCoroutine != null)
        {
            StopCoroutine(typewriterCoroutine);
        }
        
        if (useTypewriterEffect)
        {
            typewriterCoroutine = StartCoroutine(TypewriterEffect(message, type));
        }
        else
        {
            mainDisplayText.text = message;
            mainDisplayText.color = GetColorForMessageType(type);
        }
    }
    
    private IEnumerator TypewriterEffect(string message, MessageType type)
    {
        isDisplayingMessage = true;
        mainDisplayText.color = GetColorForMessageType(type);
        mainDisplayText.text = "";
        
        for (int i = 0; i <= message.Length; i++)
        {
            mainDisplayText.text = message.Substring(0, i);
            yield return new WaitForSeconds(typewriterSpeed);
        }
        
        isDisplayingMessage = false;
        typewriterCoroutine = null;
    }
    
    private Color GetColorForMessageType(MessageType type)
    {
        switch (type)
        {
            case MessageType.Question:
                return questionColor;
            case MessageType.Guess:
                return guessColor;
            case MessageType.System:
                return systemColor;
            case MessageType.Feedback:
                return feedbackColor;
            default:
                return Color.white;
        }
    }
    
    private void CreateMessageUI(MessageEntry entry)
    {
        if (messageTemplate == null || messageContainer == null) return;
        
        GameObject messageObj = Instantiate(messageTemplate, messageContainer);
        messageObj.SetActive(true);
        
        TextMeshProUGUI messageText = messageObj.GetComponentInChildren<TextMeshProUGUI>();
        if (messageText != null)
        {
            messageText.text = entry.message;
            messageText.color = GetColorForMessageType(entry.type);
        }
        
        if (autoScrollToBottom && scrollRect != null)
        {
            StartCoroutine(ScrollToBottomNextFrame());
        }
    }
    
    private IEnumerator ScrollToBottomNextFrame()
    {
        yield return new WaitForEndOfFrame();
        if (scrollRect != null)
        {
            scrollRect.verticalNormalizedPosition = 0f;
        }
    }
    
    private void UpdateDisplayForRole(WordGuessingGameManager.PlayerRole role)
    {
        string roleMessage = role == WordGuessingGameManager.PlayerRole.Guesser 
            ? "You are the Guesser. Ask questions to guess the word!"
            : "You are the Describer. Use stones to give yes/no feedback!";
        
        AddMessage(roleMessage, -1, MessageType.System);
    }
    
    public void ClearDisplay()
    {
        if (mainDisplayText != null)
        {
            mainDisplayText.text = "Magic Book cleared...";
        }
        
        messageHistory.Clear();
        
        if (messageContainer != null)
        {
            foreach (Transform child in messageContainer)
            {
                if (child.gameObject != messageTemplate)
                {
                    Destroy(child.gameObject);
                }
            }
        }
        
        if (debugMode)
        {
            Debug.Log("Magic Book display cleared");
        }
    }
    
    public void SetAssignedPlayer(int playerId)
    {
        assignedPlayerId = playerId;
        
        if (debugMode)
        {
            Debug.Log($"Magic Book assigned to Player {playerId}");
        }
    }
    
    public void SetReceiveAllMessages(bool receiveAll)
    {
        receiveAllMessages = receiveAll;
        
        if (debugMode)
        {
            Debug.Log($"Receive all messages set to: {receiveAll}");
        }
    }
    
    public void ShowTestMessage(string message)
    {
        AddMessage($"[TEST] {message}", assignedPlayerId, MessageType.System);
    }
    
    public List<MessageEntry> GetMessageHistory()
    {
        return new List<MessageEntry>(messageHistory);
    }
    
    public bool IsDisplayingMessage()
    {
        return isDisplayingMessage;
    }
    
    public void SetTypewriterSpeed(float speed)
    {
        typewriterSpeed = Mathf.Max(0.01f, speed);
        
        if (debugMode)
        {
            Debug.Log($"Typewriter speed set to: {typewriterSpeed}");
        }
    }
}