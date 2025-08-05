using UnityEngine;
using System.Collections.Generic;
using System;

public class PlayerMessageRelay : MonoBehaviour
{
    public static PlayerMessageRelay Instance { get; private set; }
    
    [System.Serializable]
    public class RelayMessage
    {
        public int senderId;
        public int receiverId;
        public string content;
        public MessageCategory category;
        public float timestamp;
        public bool isDelivered;
        
        public RelayMessage(int sender, int receiver, string msg, MessageCategory cat)
        {
            senderId = sender;
            receiverId = receiver;
            content = msg;
            category = cat;
            timestamp = Time.time;
            isDelivered = false;
        }
    }
    
    public enum MessageCategory
    {
        Voice,
        System,
        Feedback,
        GameState,
        Debug
    }
    
    [Header("Relay Configuration")]
    [SerializeField] private bool enableCrossRoomCommunication = true;
    [SerializeField] private bool logAllMessages = true;
    [SerializeField] private float messageDeliveryDelay = 0.1f;
    
    [Header("Message Filtering")]
    [SerializeField] private bool filterByRole = true;
    [SerializeField] private bool allowSystemMessagesToAll = true;
    [SerializeField] private bool enableDebugMessages = false;
    
    [Header("Message History")]
    [SerializeField] private int maxMessageHistory = 100;
    [SerializeField] private bool persistMessages = true;
    
    [Header("Connected Systems")]
    [SerializeField] private List<MagicBookTextDisplay> magicBookDisplays = new List<MagicBookTextDisplay>();
    
    public static event Action<RelayMessage> OnMessageRelayed;
    public static event Action<int, int, string> OnVoiceMessageRelayed;
    public static event Action<string> OnSystemMessageBroadcast;
    
    private List<RelayMessage> messageHistory = new List<RelayMessage>();
    private Dictionary<int, List<MagicBookTextDisplay>> playerDisplays = new Dictionary<int, List<MagicBookTextDisplay>>();
    
    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
            InitializeRelay();
        }
        else
        {
            Destroy(gameObject);
        }
    }
    
    private void OnEnable()
    {
        WordGuessingVoiceHandler.OnQuestionAsked += HandleVoiceQuestion;
        WordGuessingVoiceHandler.OnGuessAttempted += HandleVoiceGuess;
        WordGuessingGameManager.OnGameMessage += HandleSystemMessage;
        PlayerRoleManager.OnPlayerDataUpdated += HandlePlayerUpdate;
    }
    
    private void OnDisable()
    {
        WordGuessingVoiceHandler.OnQuestionAsked -= HandleVoiceQuestion;
        WordGuessingVoiceHandler.OnGuessAttempted -= HandleVoiceGuess;
        WordGuessingGameManager.OnGameMessage -= HandleSystemMessage;
        PlayerRoleManager.OnPlayerDataUpdated -= HandlePlayerUpdate;
    }
    
    private void InitializeRelay()
    {
        DiscoverMagicBookDisplays();
        
        if (logAllMessages)
        {
            Debug.Log("PlayerMessageRelay initialized");
        }
    }
    
    private void DiscoverMagicBookDisplays()
    {
        magicBookDisplays.Clear();
        playerDisplays.Clear();
        
        MagicBookTextDisplay[] allDisplays = FindObjectsByType<MagicBookTextDisplay>(FindObjectsSortMode.None);
        foreach (var display in allDisplays)
        {
            magicBookDisplays.Add(display);
            
            // Get assigned player ID from the display (you may need to add a method to get this)
            int playerId = GetDisplayPlayerId(display);
            
            if (!playerDisplays.ContainsKey(playerId))
            {
                playerDisplays[playerId] = new List<MagicBookTextDisplay>();
            }
            playerDisplays[playerId].Add(display);
        }
        
        if (logAllMessages)
        {
            Debug.Log($"Discovered {magicBookDisplays.Count} magic book displays");
        }
    }
    
    private int GetDisplayPlayerId(MagicBookTextDisplay display)
    {
        // This would need to be implemented based on how you assign player IDs to displays
        // For now, assuming a naming convention or component property
        return 1; // Placeholder - implement based on your system
    }
    
    private void HandleVoiceQuestion(int senderId, string question)
    {
        RelayVoiceMessage(senderId, question, MessageCategory.Voice);
        
        if (logAllMessages)
        {
            Debug.Log($"Relaying voice question from Player {senderId}: {question}");
        }
    }
    
    private void HandleVoiceGuess(int senderId, string guess)
    {
        RelayVoiceMessage(senderId, $"Guess: {guess}", MessageCategory.Voice);
        
        if (logAllMessages)
        {
            Debug.Log($"Relaying voice guess from Player {senderId}: {guess}");
        }
    }
    
    private void HandleSystemMessage(string message)
    {
        BroadcastSystemMessage(message);
        
        if (logAllMessages)
        {
            Debug.Log($"Broadcasting system message: {message}");
        }
    }
    
    private void HandlePlayerUpdate(PlayerRoleManager.PlayerData playerData)
    {
        string updateMessage = $"Player {playerData.playerId} is now the {playerData.currentRole}";
        RelaySystemMessage(-1, updateMessage, MessageCategory.GameState);
    }
    
    public void RelayVoiceMessage(int senderId, string message, MessageCategory category = MessageCategory.Voice)
    {
        if (!enableCrossRoomCommunication) return;
        
        // Determine receivers based on game rules
        List<int> receivers = GetValidReceivers(senderId, category);
        
        foreach (int receiverId in receivers)
        {
            RelayMessage relayMsg = new RelayMessage(senderId, receiverId, message, category);
            
            if (ShouldRelayMessage(relayMsg))
            {
                StartCoroutine(DeliverMessageWithDelay(relayMsg));
                OnVoiceMessageRelayed?.Invoke(senderId, receiverId, message);
            }
        }
    }
    
    public void RelaySystemMessage(int senderId, string message, MessageCategory category = MessageCategory.System)
    {
        if (allowSystemMessagesToAll)
        {
            BroadcastSystemMessage(message);
        }
        else
        {
            RelayVoiceMessage(senderId, message, category);
        }
    }
    
    public void BroadcastSystemMessage(string message)
    {
        RelayMessage broadcastMsg = new RelayMessage(-1, -1, message, MessageCategory.System);
        AddToHistory(broadcastMsg);
        
        // Send to all connected players
        foreach (var displayPair in playerDisplays)
        {
            foreach (var display in displayPair.Value)
            {
                DeliverToDisplay(display, broadcastMsg);
            }
        }
        
        OnSystemMessageBroadcast?.Invoke(message);
    }
    
    private List<int> GetValidReceivers(int senderId, MessageCategory category)
    {
        List<int> receivers = new List<int>();
        
        if (PlayerRoleManager.Instance == null) return receivers;
        
        // Get all connected players except sender
        var allPlayers = PlayerRoleManager.Instance.GetConnectedPlayers();
        
        foreach (var player in allPlayers)
        {
            if (player.playerId != senderId)
            {
                if (ShouldReceiveMessage(senderId, player.playerId, category))
                {
                    receivers.Add(player.playerId);
                }
            }
        }
        
        return receivers;
    }
    
    private bool ShouldReceiveMessage(int senderId, int receiverId, MessageCategory category)
    {
        if (!filterByRole) return true;
        
        // System messages always go through
        if (category == MessageCategory.System || category == MessageCategory.GameState)
            return true;
        
        // Voice messages should go to the other player in cross-room communication
        if (category == MessageCategory.Voice)
        {
            return senderId != receiverId;
        }
        
        // Debug messages only if enabled
        if (category == MessageCategory.Debug)
            return enableDebugMessages;
        
        return true;
    }
    
    private bool ShouldRelayMessage(RelayMessage message)
    {
        // Check if sender can send messages based on their role
        if (filterByRole && PlayerRoleManager.Instance != null)
        {
            if (message.category == MessageCategory.Voice)
            {
                return PlayerRoleManager.Instance.CanPlayerUseMagicBook(message.senderId);
            }
        }
        
        return true;
    }
    
    private System.Collections.IEnumerator DeliverMessageWithDelay(RelayMessage message)
    {
        yield return new WaitForSeconds(messageDeliveryDelay);
        
        DeliverMessage(message);
    }
    
    private void DeliverMessage(RelayMessage message)
    {
        AddToHistory(message);
        
        // Deliver to specific receiver
        if (playerDisplays.ContainsKey(message.receiverId))
        {
            foreach (var display in playerDisplays[message.receiverId])
            {
                DeliverToDisplay(display, message);
            }
        }
        
        message.isDelivered = true;
        OnMessageRelayed?.Invoke(message);
    }
    
    private void DeliverToDisplay(MagicBookTextDisplay display, RelayMessage message)
    {
        if (display == null) return;
        
        // The display system will handle the actual showing of the message
        // through its event subscriptions
    }
    
    private void AddToHistory(RelayMessage message)
    {
        if (!persistMessages) return;
        
        messageHistory.Add(message);
        
        // Limit history size
        if (messageHistory.Count > maxMessageHistory)
        {
            messageHistory.RemoveAt(0);
        }
    }
    
    public void RegisterMagicBookDisplay(MagicBookTextDisplay display, int playerId)
    {
        if (!magicBookDisplays.Contains(display))
        {
            magicBookDisplays.Add(display);
        }
        
        if (!playerDisplays.ContainsKey(playerId))
        {
            playerDisplays[playerId] = new List<MagicBookTextDisplay>();
        }
        
        if (!playerDisplays[playerId].Contains(display))
        {
            playerDisplays[playerId].Add(display);
        }
        
        if (logAllMessages)
        {
            Debug.Log($"Registered magic book display for Player {playerId}");
        }
    }
    
    public void UnregisterMagicBookDisplay(MagicBookTextDisplay display, int playerId)
    {
        magicBookDisplays.Remove(display);
        
        if (playerDisplays.ContainsKey(playerId))
        {
            playerDisplays[playerId].Remove(display);
            
            if (playerDisplays[playerId].Count == 0)
            {
                playerDisplays.Remove(playerId);
            }
        }
        
        if (logAllMessages)
        {
            Debug.Log($"Unregistered magic book display for Player {playerId}");
        }
    }
    
    public List<RelayMessage> GetMessageHistory()
    {
        return new List<RelayMessage>(messageHistory);
    }
    
    public List<RelayMessage> GetPlayerMessages(int playerId)
    {
        return messageHistory.FindAll(msg => 
            msg.senderId == playerId || msg.receiverId == playerId);
    }
    
    public void ClearMessageHistory()
    {
        messageHistory.Clear();
        
        if (logAllMessages)
        {
            Debug.Log("Message history cleared");
        }
    }
    
    public void SetCrossRoomCommunication(bool enabled)
    {
        enableCrossRoomCommunication = enabled;
        
        if (logAllMessages)
        {
            Debug.Log($"Cross-room communication set to: {enabled}");
        }
    }
    
    public void SetMessageDeliveryDelay(float delay)
    {
        messageDeliveryDelay = Mathf.Max(0f, delay);
        
        if (logAllMessages)
        {
            Debug.Log($"Message delivery delay set to: {messageDeliveryDelay} seconds");
        }
    }
    
    public int GetActiveDisplayCount()
    {
        return magicBookDisplays.Count;
    }
    
    public bool IsPlayerDisplayRegistered(int playerId)
    {
        return playerDisplays.ContainsKey(playerId) && playerDisplays[playerId].Count > 0;
    }
    
    private void OnDestroy()
    {
        if (Instance == this)
        {
            Instance = null;
        }
    }
}