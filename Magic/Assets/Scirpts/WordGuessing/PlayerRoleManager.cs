using UnityEngine;
using System.Collections.Generic;
using System;
using System.Linq;

public class PlayerRoleManager : MonoBehaviour
{
    public static PlayerRoleManager Instance { get; private set; }
    
    [System.Serializable]
    public class PlayerData
    {
        public int playerId;
        public string playerName;
        public WordGuessingGameManager.PlayerRole currentRole;
        public bool isConnected;
        public int score;
        public Vector3 roomPosition;
        public bool canUseMagicBook;
        public bool canUseStones;
        public bool canSeePictureFrame;
        
        public PlayerData(int id, string name)
        {
            playerId = id;
            playerName = name;
            currentRole = WordGuessingGameManager.PlayerRole.Guesser;
            isConnected = false;
            score = 0;
            canUseMagicBook = false;
            canUseStones = false;
            canSeePictureFrame = false;
        }
    }
    
    [Header("Player Management")]
    [SerializeField] private List<PlayerData> players = new List<PlayerData>();
    [SerializeField] private int maxPlayers = 2;
    
    [Header("Role Permissions")]
    [SerializeField] private bool strictRoleEnforcement = true;
    [SerializeField] private bool debugMode = true;
    
    [Header("Room Assignments")]
    [SerializeField] private Vector3 room1Position = new Vector3(-10, 0, 0);
    [SerializeField] private Vector3 room2Position = new Vector3(10, 0, 0);
    
    public static event Action<int, WordGuessingGameManager.PlayerRole> OnPlayerRoleChanged;
    public static event Action<int, bool> OnPlayerPermissionsChanged;
    public static event Action<int, bool> OnPlayerConnected;
    public static event Action<PlayerData> OnPlayerDataUpdated;
    
    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
            InitializePlayerSystem();
        }
        else
        {
            Destroy(gameObject);
        }
    }
    
    private void OnEnable()
    {
        WordGuessingGameManager.OnRolesChanged += HandleRolesChanged;
        WordGuessingGameManager.OnScoreUpdated += HandleScoreUpdated;
    }
    
    private void OnDisable()
    {
        WordGuessingGameManager.OnRolesChanged -= HandleRolesChanged;
        WordGuessingGameManager.OnScoreUpdated -= HandleScoreUpdated;
    }
    
    private void InitializePlayerSystem()
    {
        for (int i = 1; i <= maxPlayers; i++)
        {
            PlayerData newPlayer = new PlayerData(i, $"Player{i}");
            newPlayer.roomPosition = i == 1 ? room1Position : room2Position;
            players.Add(newPlayer);
        }
        
        if (players.Count >= 2)
        {
            SetPlayerRole(1, WordGuessingGameManager.PlayerRole.Guesser);
            SetPlayerRole(2, WordGuessingGameManager.PlayerRole.Describer);
        }
        
        if (debugMode)
        {
            Debug.Log($"Player system initialized with {players.Count} players");
        }
    }
    
    public void ConnectPlayer(int playerId, string playerName = "")
    {
        PlayerData player = GetPlayerData(playerId);
        if (player != null)
        {
            player.isConnected = true;
            if (!string.IsNullOrEmpty(playerName))
                player.playerName = playerName;
            
            UpdatePlayerPermissions(player);
            
            OnPlayerConnected?.Invoke(playerId, true);
            OnPlayerDataUpdated?.Invoke(player);
            
            if (debugMode)
            {
                Debug.Log($"Player {playerId} ({player.playerName}) connected");
            }
        }
    }
    
    public void DisconnectPlayer(int playerId)
    {
        PlayerData player = GetPlayerData(playerId);
        if (player != null)
        {
            player.isConnected = false;
            ResetPlayerPermissions(player);
            
            OnPlayerConnected?.Invoke(playerId, false);
            OnPlayerDataUpdated?.Invoke(player);
            
            if (debugMode)
            {
                Debug.Log($"Player {playerId} disconnected");
            }
        }
    }
    
    public void SetPlayerRole(int playerId, WordGuessingGameManager.PlayerRole role)
    {
        PlayerData player = GetPlayerData(playerId);
        if (player != null)
        {
            WordGuessingGameManager.PlayerRole oldRole = player.currentRole;
            player.currentRole = role;
            
            UpdatePlayerPermissions(player);
            
            OnPlayerRoleChanged?.Invoke(playerId, role);
            OnPlayerDataUpdated?.Invoke(player);
            
            if (debugMode)
            {
                Debug.Log($"Player {playerId} role changed from {oldRole} to {role}");
            }
        }
    }
    
    private void UpdatePlayerPermissions(PlayerData player)
    {
        switch (player.currentRole)
        {
            case WordGuessingGameManager.PlayerRole.Guesser:
                player.canUseMagicBook = true;
                player.canUseStones = false;
                player.canSeePictureFrame = false;
                break;
                
            case WordGuessingGameManager.PlayerRole.Describer:
                player.canUseMagicBook = false;
                player.canUseStones = true;
                player.canSeePictureFrame = true;
                break;
        }
        
        OnPlayerPermissionsChanged?.Invoke(player.playerId, player.isConnected);
        
        if (debugMode)
        {
            Debug.Log($"Player {player.playerId} permissions updated: MagicBook={player.canUseMagicBook}, Stones={player.canUseStones}, PictureFrame={player.canSeePictureFrame}");
        }
    }
    
    private void ResetPlayerPermissions(PlayerData player)
    {
        player.canUseMagicBook = false;
        player.canUseStones = false;
        player.canSeePictureFrame = false;
        
        OnPlayerPermissionsChanged?.Invoke(player.playerId, false);
    }
    
    private void HandleRolesChanged(WordGuessingGameManager.PlayerRole player1Role, WordGuessingGameManager.PlayerRole player2Role)
    {
        SetPlayerRole(1, player1Role);
        SetPlayerRole(2, player2Role);
    }
    
    private void HandleScoreUpdated(int player1Score, int player2Score)
    {
        PlayerData player1 = GetPlayerData(1);
        PlayerData player2 = GetPlayerData(2);
        
        if (player1 != null)
        {
            player1.score = player1Score;
            OnPlayerDataUpdated?.Invoke(player1);
        }
        
        if (player2 != null)
        {
            player2.score = player2Score;
            OnPlayerDataUpdated?.Invoke(player2);
        }
    }
    
    public PlayerData GetPlayerData(int playerId)
    {
        return players.Find(p => p.playerId == playerId);
    }
    
    public WordGuessingGameManager.PlayerRole GetPlayerRole(int playerId)
    {
        PlayerData player = GetPlayerData(playerId);
        return player?.currentRole ?? WordGuessingGameManager.PlayerRole.Guesser;
    }
    
    public bool CanPlayerUseMagicBook(int playerId)
    {
        if (!strictRoleEnforcement) return true;
        
        PlayerData player = GetPlayerData(playerId);
        return player != null && player.isConnected && player.canUseMagicBook;
    }
    
    public bool CanPlayerUseStones(int playerId)
    {
        if (!strictRoleEnforcement) return true;
        
        PlayerData player = GetPlayerData(playerId);
        return player != null && player.isConnected && player.canUseStones;
    }
    
    public bool CanPlayerSeePictureFrame(int playerId)
    {
        PlayerData player = GetPlayerData(playerId);
        return player != null && player.isConnected && player.canSeePictureFrame;
    }
    
    public bool IsPlayerConnected(int playerId)
    {
        PlayerData player = GetPlayerData(playerId);
        return player != null && player.isConnected;
    }
    
    public List<PlayerData> GetAllPlayers()
    {
        return new List<PlayerData>(players);
    }
    
    public List<PlayerData> GetConnectedPlayers()
    {
        return players.FindAll(p => p.isConnected);
    }
    
    public int GetConnectedPlayerCount()
    {
        return players.Count(p => p.isConnected);
    }
    
    public bool AreAllPlayersConnected()
    {
        return GetConnectedPlayerCount() == maxPlayers;
    }
    
    public PlayerData GetGuesser()
    {
        return players.Find(p => p.currentRole == WordGuessingGameManager.PlayerRole.Guesser);
    }
    
    public PlayerData GetDescriber()
    {
        return players.Find(p => p.currentRole == WordGuessingGameManager.PlayerRole.Describer);
    }
    
    public Vector3 GetPlayerRoomPosition(int playerId)
    {
        PlayerData player = GetPlayerData(playerId);
        return player?.roomPosition ?? Vector3.zero;
    }
    
    public void SetStrictRoleEnforcement(bool enabled)
    {
        strictRoleEnforcement = enabled;
        
        if (debugMode)
        {
            Debug.Log($"Strict role enforcement set to: {enabled}");
        }
    }
    
    public void ResetAllPlayers()
    {
        foreach (PlayerData player in players)
        {
            player.isConnected = false;
            player.score = 0;
            ResetPlayerPermissions(player);
            OnPlayerDataUpdated?.Invoke(player);
        }
        
        if (debugMode)
        {
            Debug.Log("All players reset");
        }
    }
    
    private void OnDestroy()
    {
        if (Instance == this)
        {
            Instance = null;
        }
    }
}