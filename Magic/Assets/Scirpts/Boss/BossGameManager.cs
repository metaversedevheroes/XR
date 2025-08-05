using UnityEngine;
using System.Collections;
using System;

public class BossGameManager : MonoBehaviour
{
    public static BossGameManager Instance { get; private set; }
    
    [System.Serializable]
    public class BossGameState
    {
        public bool isGameActive = false;
        public bool playerInBossRoom = false;
        public int playerHealth = 100;
        public int maxPlayerHealth = 100;
        public int bossHealth = 500;
        public int maxBossHealth = 500;
        public float timeLimit = 30f;
        public float currentTime = 0f;
        public int correctAnswersInRow = 0;
        public GamePhase currentPhase = GamePhase.WaitingToStart;
    }
    
    public enum GamePhase
    {
        WaitingToStart,
        BossIntroduction,
        Combat,
        PlayerDamaged,
        BossDefeated,
        GameOver
    }
    
    [Header("Game Configuration")]
    [SerializeField] private BossGameState gameState = new BossGameState();
    [SerializeField] private bool debugMode = true;
    [SerializeField] private float sentenceTimeLimit = 15f;
    [SerializeField] private int damagePerCorrectAnswer = 25;
    [SerializeField] private int bossDamageToPlayer = 20;
    [SerializeField] private int comboMultiplier = 2;
    
    [Header("References")]
    [SerializeField] private Transform playerSpawnPoint;
    [SerializeField] private Transform bossSpawnPoint;
    [SerializeField] private GameObject bossPrefab;
    [SerializeField] private GameObject npcCompanionPrefab;
    
    private BossMonster bossMonster;
    private NPCCompanion npcCompanion;
    private SpeechCombatSystem speechCombatSystem;
    private EnglishSentenceDatabase sentenceDatabase;
    private PlayerHealthSystem playerHealthSystem;
    
    public static event Action<GamePhase> OnGamePhaseChanged;
    public static event Action<int, int> OnPlayerHealthChanged;
    public static event Action<int, int> OnBossHealthChanged;
    public static event Action<string> OnGameMessage;
    public static event Action OnPlayerDeath;
    public static event Action OnBossDefeated;
    
    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            InitializeComponents();
        }
        else
        {
            Destroy(gameObject);
        }
    }
    
    private void InitializeComponents()
    {
        speechCombatSystem = GetComponent<SpeechCombatSystem>();
        sentenceDatabase = GetComponent<EnglishSentenceDatabase>();
        playerHealthSystem = GetComponent<PlayerHealthSystem>();
        
        if (speechCombatSystem == null)
            speechCombatSystem = gameObject.AddComponent<SpeechCombatSystem>();
        if (sentenceDatabase == null)
            sentenceDatabase = gameObject.AddComponent<EnglishSentenceDatabase>();
        if (playerHealthSystem == null)
            playerHealthSystem = gameObject.AddComponent<PlayerHealthSystem>();
    }
    
    private void Start()
    {
        SubscribeToEvents();
        InitializeGameState();
    }
    
    private void SubscribeToEvents()
    {
        if (speechCombatSystem != null)
        {
            speechCombatSystem.OnCorrectAnswer += HandleCorrectAnswer;
            speechCombatSystem.OnIncorrectAnswer += HandleIncorrectAnswer;
            speechCombatSystem.OnTimeExpired += HandleTimeExpired;
        }
        
        if (playerHealthSystem != null)
        {
            playerHealthSystem.OnPlayerDeath += HandlePlayerDeath;
        }
    }
    
    private void InitializeGameState()
    {
        gameState.playerHealth = gameState.maxPlayerHealth;
        gameState.bossHealth = gameState.maxBossHealth;
        gameState.currentTime = 0f;
        gameState.correctAnswersInRow = 0;
        gameState.timeLimit = sentenceTimeLimit;
        
        OnPlayerHealthChanged?.Invoke(gameState.playerHealth, gameState.maxPlayerHealth);
        OnBossHealthChanged?.Invoke(gameState.bossHealth, gameState.maxBossHealth);
    }
    
    public void StartBossGame()
    {
        if (debugMode) Debug.Log("Starting Boss Game");
        
        gameState.isGameActive = true;
        gameState.playerInBossRoom = true;
        
        StartCoroutine(BossGameSequence());
    }
    
    private IEnumerator BossGameSequence()
    {
        ChangeGamePhase(GamePhase.BossIntroduction);
        yield return StartCoroutine(BossIntroduction());
        
        ChangeGamePhase(GamePhase.Combat);
        StartCombatPhase();
    }
    
    private IEnumerator BossIntroduction()
    {
        OnGameMessage?.Invoke("A mighty dragon appears!");
        
        if (bossPrefab != null && bossSpawnPoint != null)
        {
            GameObject bossObj = Instantiate(bossPrefab, bossSpawnPoint.position, bossSpawnPoint.rotation);
            bossMonster = bossObj.GetComponent<BossMonster>();
            if (bossMonster != null)
            {
                bossMonster.Initialize(gameState.maxBossHealth);
            }
        }
        
        if (npcCompanionPrefab != null)
        {
            GameObject companionObj = Instantiate(npcCompanionPrefab, playerSpawnPoint.position + Vector3.right * 2f, Quaternion.identity);
            npcCompanion = companionObj.GetComponent<NPCCompanion>();
            if (npcCompanion != null)
            {
                npcCompanion.Initialize(bossMonster);
            }
        }
        
        yield return new WaitForSeconds(3f);
        
        OnGameMessage?.Invoke("Speak English sentences correctly to damage the dragon!");
        yield return new WaitForSeconds(2f);
    }
    
    private void StartCombatPhase()
    {
        if (speechCombatSystem != null)
        {
            speechCombatSystem.StartListening();
        }
        
        if (npcCompanion != null)
        {
            npcCompanion.StartAttacking();
        }
        
        PresentNewSentence();
        StartCoroutine(CombatTimer());
    }
    
    private void PresentNewSentence()
    {
        if (sentenceDatabase != null)
        {
            var sentence = sentenceDatabase.GetRandomSentence();
            speechCombatSystem.SetCurrentSentence(sentence);
            gameState.currentTime = 0f;
            
            if (debugMode) Debug.Log($"New sentence presented: {sentence.completeSentence}");
        }
    }
    
    private IEnumerator CombatTimer()
    {
        while (gameState.isGameActive && gameState.currentPhase == GamePhase.Combat)
        {
            gameState.currentTime += Time.deltaTime;
            
            if (gameState.currentTime >= gameState.timeLimit)
            {
                HandleTimeExpired();
                yield break;
            }
            
            yield return null;
        }
    }
    
    private void HandleCorrectAnswer(int damage)
    {
        gameState.correctAnswersInRow++;
        
        int totalDamage = damage;
        if (gameState.correctAnswersInRow > 1)
        {
            totalDamage *= (gameState.correctAnswersInRow * comboMultiplier);
            OnGameMessage?.Invoke($"Combo x{gameState.correctAnswersInRow}! Extra damage!");
        }
        
        DealDamageToBoss(totalDamage);
        
        if (gameState.bossHealth <= 0)
        {
            HandleBossDefeated();
        }
        else
        {
            PresentNewSentence();
            StartCoroutine(CombatTimer());
        }
    }
    
    private void HandleIncorrectAnswer()
    {
        gameState.correctAnswersInRow = 0;
        OnGameMessage?.Invoke("Wrong answer! The dragon attacks!");
        
        HandleBossAttack();
    }
    
    private void HandleTimeExpired()
    {
        gameState.correctAnswersInRow = 0;
        OnGameMessage?.Invoke("Time's up! The dragon attacks!");
        
        HandleBossAttack();
    }
    
    private void HandleBossAttack()
    {
        ChangeGamePhase(GamePhase.PlayerDamaged);
        
        if (bossMonster != null)
        {
            bossMonster.PerformAttack();
        }
        
        DealDamageToPlayer(bossDamageToPlayer);
        
        StartCoroutine(ReturnToCombat());
    }
    
    private IEnumerator ReturnToCombat()
    {
        yield return new WaitForSeconds(2f);
        
        if (gameState.playerHealth > 0)
        {
            ChangeGamePhase(GamePhase.Combat);
            PresentNewSentence();
            StartCoroutine(CombatTimer());
        }
    }
    
    private void DealDamageToBoss(int damage)
    {
        gameState.bossHealth = Mathf.Max(0, gameState.bossHealth - damage);
        OnBossHealthChanged?.Invoke(gameState.bossHealth, gameState.maxBossHealth);
        
        if (bossMonster != null)
        {
            bossMonster.TakeDamage(damage);
        }
        
        OnGameMessage?.Invoke($"Dragon takes {damage} damage!");
        
        if (debugMode) Debug.Log($"Boss health: {gameState.bossHealth}/{gameState.maxBossHealth}");
    }
    
    private void DealDamageToPlayer(int damage)
    {
        gameState.playerHealth = Mathf.Max(0, gameState.playerHealth - damage);
        OnPlayerHealthChanged?.Invoke(gameState.playerHealth, gameState.maxPlayerHealth);
        
        if (playerHealthSystem != null)
        {
            playerHealthSystem.TakeDamage(damage);
        }
        
        OnGameMessage?.Invoke($"You take {damage} damage!");
        
        if (gameState.playerHealth <= 0)
        {
            HandlePlayerDeath();
        }
        
        if (debugMode) Debug.Log($"Player health: {gameState.playerHealth}/{gameState.maxPlayerHealth}");
    }
    
    private void HandlePlayerDeath()
    {
        ChangeGamePhase(GamePhase.GameOver);
        gameState.isGameActive = false;
        
        OnPlayerDeath?.Invoke();
        OnGameMessage?.Invoke("You have been defeated! Restarting boss battle...");
        
        StartCoroutine(RestartGame());
    }
    
    private void HandleBossDefeated()
    {
        ChangeGamePhase(GamePhase.BossDefeated);
        gameState.isGameActive = false;
        
        OnBossDefeated?.Invoke();
        OnGameMessage?.Invoke("Congratulations! You have defeated the dragon!");
        
        if (bossMonster != null)
        {
            bossMonster.PlayDefeatedAnimation();
        }
        
        if (npcCompanion != null)
        {
            npcCompanion.StopAttacking();
            npcCompanion.PlayVictoryAnimation();
        }
    }
    
    private IEnumerator RestartGame()
    {
        yield return new WaitForSeconds(3f);
        
        ResetGame();
        StartBossGame();
    }
    
    private void ResetGame()
    {
        if (bossMonster != null)
        {
            Destroy(bossMonster.gameObject);
        }
        
        if (npcCompanion != null)
        {
            Destroy(npcCompanion.gameObject);
        }
        
        InitializeGameState();
        ChangeGamePhase(GamePhase.WaitingToStart);
        
        if (debugMode) Debug.Log("Game reset completed");
    }
    
    private void ChangeGamePhase(GamePhase newPhase)
    {
        gameState.currentPhase = newPhase;
        OnGamePhaseChanged?.Invoke(newPhase);
        
        if (debugMode) Debug.Log($"Game phase changed to: {newPhase}");
    }
    
    public void PlayerEnteredBossRoom()
    {
        if (!gameState.isGameActive)
        {
            StartBossGame();
        }
    }
    
    public void PlayerExitedBossRoom()
    {
        if (gameState.isGameActive)
        {
            ResetGame();
        }
    }
    
    public BossGameState GetGameState()
    {
        return gameState;
    }
    
    public bool IsGameActive()
    {
        return gameState.isGameActive;
    }
    
    private void OnDestroy()
    {
        if (speechCombatSystem != null)
        {
            speechCombatSystem.OnCorrectAnswer -= HandleCorrectAnswer;
            speechCombatSystem.OnIncorrectAnswer -= HandleIncorrectAnswer;
            speechCombatSystem.OnTimeExpired -= HandleTimeExpired;
        }
        
        if (playerHealthSystem != null)
        {
            playerHealthSystem.OnPlayerDeath -= HandlePlayerDeath;
        }
        
        if (Instance == this)
        {
            Instance = null;
        }
    }
}