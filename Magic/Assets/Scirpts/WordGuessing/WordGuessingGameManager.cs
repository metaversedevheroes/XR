using UnityEngine;
using System.Collections.Generic;
using System;

public class WordGuessingGameManager : MonoBehaviour
{
    public static WordGuessingGameManager Instance { get; private set; }
    
    [System.Serializable]
    public class GameState
    {
        public PlayerRole player1Role = PlayerRole.Guesser;
        public PlayerRole player2Role = PlayerRole.Describer;
        public int player1Score = 0;
        public int player2Score = 0;
        public int currentRound = 1;
        public GamePhase currentPhase = GamePhase.WaitingForPlayers;
        public string currentWord = "";
        public bool isGameActive = false;
        public int wordsGuessedThisStage = 0;
        public int requiredWordsPerStage = 4;
    }
    
    public enum PlayerRole
    {
        Guesser,
        Describer
    }
    
    public enum GamePhase
    {
        WaitingForPlayers,
        RoundStart,
        QuestionPhase,
        FeedbackPhase,
        GuessPhase,
        RoundEnd,
        StageComplete
    }
    
    [Header("Game Configuration")]
    [SerializeField] private GameState gameState = new GameState();
    [SerializeField] private bool debugMode = true;
    
    public static event Action<GamePhase> OnGamePhaseChanged;
    public static event Action<PlayerRole, PlayerRole> OnRolesChanged;
    public static event Action<string> OnNewWordSelected;
    public static event Action<int, int> OnScoreUpdated;
    public static event Action OnStageComplete;
    public static event Action<string> OnGameMessage;
    
    private WordDatabase wordDatabase;
    private AnswerValidator answerValidator;
    
    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
            InitializeComponents();
        }
        else
        {
            Destroy(gameObject);
        }
    }
    
    private void InitializeComponents()
    {
        wordDatabase = GetComponent<WordDatabase>();
        answerValidator = GetComponent<AnswerValidator>();
        
        if (wordDatabase == null)
            wordDatabase = gameObject.AddComponent<WordDatabase>();
        if (answerValidator == null)
            answerValidator = gameObject.AddComponent<AnswerValidator>();
    }
    
    public void StartGame()
    {
        if (debugMode) Debug.Log("Starting Word Guessing Game");
        
        gameState.isGameActive = true;
        gameState.currentRound = 1;
        gameState.player1Score = 0;
        gameState.player2Score = 0;
        gameState.wordsGuessedThisStage = 0;
        
        ChangeGamePhase(GamePhase.RoundStart);
        OnGameMessage?.Invoke("Game Started! Get ready for the first round.");
    }
    
    public void StartNewRound()
    {
        if (!gameState.isGameActive) return;
        
        string newWord = wordDatabase.GetRandomWord();
        gameState.currentWord = newWord;
        
        if (debugMode) Debug.Log($"New round started. Word: {newWord}");
        
        OnNewWordSelected?.Invoke(newWord);
        ChangeGamePhase(GamePhase.QuestionPhase);
        OnGameMessage?.Invoke($"Round {gameState.currentRound} started! Guesser can now ask questions.");
    }
    
    public void ProcessGuess(string guess)
    {
        if (gameState.currentPhase != GamePhase.GuessPhase) return;
        
        bool isCorrect = answerValidator.ValidateAnswer(guess, gameState.currentWord);
        
        if (isCorrect)
        {
            HandleCorrectGuess();
        }
        else
        {
            OnGameMessage?.Invoke("Incorrect guess. Keep trying!");
            ChangeGamePhase(GamePhase.QuestionPhase);
        }
    }
    
    private void HandleCorrectGuess()
    {
        gameState.wordsGuessedThisStage++;
        
        if (GetCurrentGuesser() == PlayerRole.Guesser)
            gameState.player1Score++;
        else
            gameState.player2Score++;
        
        OnScoreUpdated?.Invoke(gameState.player1Score, gameState.player2Score);
        OnGameMessage?.Invoke($"Correct! Word was '{gameState.currentWord}'");
        
        if (gameState.wordsGuessedThisStage >= gameState.requiredWordsPerStage)
        {
            CompleteStage();
        }
        else
        {
            SwitchRoles();
            gameState.currentRound++;
            ChangeGamePhase(GamePhase.RoundStart);
        }
    }
    
    public void SwitchRoles()
    {
        PlayerRole temp = gameState.player1Role;
        gameState.player1Role = gameState.player2Role;
        gameState.player2Role = temp;
        
        if (debugMode) Debug.Log($"Roles switched - Player1: {gameState.player1Role}, Player2: {gameState.player2Role}");
        
        OnRolesChanged?.Invoke(gameState.player1Role, gameState.player2Role);
        OnGameMessage?.Invoke("Roles have been switched for the next round!");
    }
    
    private void CompleteStage()
    {
        ChangeGamePhase(GamePhase.StageComplete);
        OnStageComplete?.Invoke();
        OnGameMessage?.Invoke("Stage Complete! Both players successfully guessed their words.");
        
        if (debugMode) Debug.Log("Stage completed successfully");
    }
    
    private void ChangeGamePhase(GamePhase newPhase)
    {
        gameState.currentPhase = newPhase;
        OnGamePhaseChanged?.Invoke(newPhase);
        
        if (debugMode) Debug.Log($"Game phase changed to: {newPhase}");
    }
    
    public void SetGamePhase(GamePhase phase)
    {
        ChangeGamePhase(phase);
    }
    
    public PlayerRole GetCurrentGuesser()
    {
        return gameState.player1Role;
    }
    
    public PlayerRole GetCurrentDescriber()
    {
        return gameState.player2Role;
    }
    
    public PlayerRole GetPlayerRole(int playerId)
    {
        return playerId == 1 ? gameState.player1Role : gameState.player2Role;
    }
    
    public string GetCurrentWord()
    {
        return gameState.currentWord;
    }
    
    public GamePhase GetCurrentPhase()
    {
        return gameState.currentPhase;
    }
    
    public GameState GetGameState()
    {
        return gameState;
    }
    
    public bool IsGameActive()
    {
        return gameState.isGameActive;
    }
    
    public void EndGame()
    {
        gameState.isGameActive = false;
        ChangeGamePhase(GamePhase.WaitingForPlayers);
        OnGameMessage?.Invoke("Game ended.");
        
        if (debugMode) Debug.Log("Game ended");
    }
    
    private void OnDestroy()
    {
        if (Instance == this)
        {
            Instance = null;
        }
    }
}