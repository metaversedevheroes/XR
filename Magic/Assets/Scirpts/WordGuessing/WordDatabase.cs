using UnityEngine;
using System.Collections.Generic;
using System.Linq;

[System.Serializable]
public class WordEntry
{
    public string word;
    public DifficultyLevel difficulty;
    public string category;
    public string hint;
    
    public WordEntry(string word, DifficultyLevel difficulty, string category = "", string hint = "")
    {
        this.word = word;
        this.difficulty = difficulty;
        this.category = category;
        this.hint = hint;
    }
}

public enum DifficultyLevel
{
    Easy = 1,
    Medium = 2,
    Hard = 3
}

public class WordDatabase : MonoBehaviour
{
    [Header("Word Database Configuration")]
    [SerializeField] private List<WordEntry> wordDatabase = new List<WordEntry>();
    [SerializeField] private DifficultyLevel currentDifficulty = DifficultyLevel.Easy;
    [SerializeField] private bool enableCategoryFiltering = false;
    [SerializeField] private string preferredCategory = "";
    
    [Header("Debug")]
    [SerializeField] private bool debugMode = true;
    [SerializeField] private bool autoGenerateWords = true;
    
    private List<string> usedWords = new List<string>();
    private System.Random random = new System.Random();
    
    private void Awake()
    {
        InitializeDatabase();
    }
    
    private void InitializeDatabase()
    {
        if (autoGenerateWords && wordDatabase.Count == 0)
        {
            GenerateDefaultWords();
        }
        
        if (debugMode)
        {
            Debug.Log($"Word database initialized with {wordDatabase.Count} words");
        }
    }
    
    private void GenerateDefaultWords()
    {
        AddWordsToDatabase(new WordEntry[]
        {
            new WordEntry("apple", DifficultyLevel.Easy, "fruit", "A red or green fruit"),
            new WordEntry("dog", DifficultyLevel.Easy, "animal", "Man's best friend"),
            new WordEntry("car", DifficultyLevel.Easy, "vehicle", "Four-wheeled transportation"),
            new WordEntry("book", DifficultyLevel.Easy, "object", "You read this"),
            new WordEntry("house", DifficultyLevel.Easy, "building", "Where people live"),
            new WordEntry("water", DifficultyLevel.Easy, "liquid", "Clear liquid you drink"),
            new WordEntry("school", DifficultyLevel.Easy, "place", "Where children learn"),
            new WordEntry("happy", DifficultyLevel.Easy, "emotion", "Feeling good"),
            new WordEntry("sun", DifficultyLevel.Easy, "nature", "Bright star in the sky"),
            new WordEntry("tree", DifficultyLevel.Easy, "nature", "Tall plant with leaves"),
            
            new WordEntry("computer", DifficultyLevel.Medium, "technology", "Electronic device for computing"),
            new WordEntry("elephant", DifficultyLevel.Medium, "animal", "Large gray animal with trunk"),
            new WordEntry("mountain", DifficultyLevel.Medium, "nature", "Very tall land formation"),
            new WordEntry("breakfast", DifficultyLevel.Medium, "meal", "First meal of the day"),
            new WordEntry("rainbow", DifficultyLevel.Medium, "nature", "Colorful arc in the sky"),
            new WordEntry("guitar", DifficultyLevel.Medium, "instrument", "Six-stringed musical instrument"),
            new WordEntry("library", DifficultyLevel.Medium, "place", "Place with many books"),
            new WordEntry("butterfly", DifficultyLevel.Medium, "insect", "Colorful flying insect"),
            new WordEntry("ocean", DifficultyLevel.Medium, "nature", "Large body of salt water"),
            new WordEntry("pizza", DifficultyLevel.Medium, "food", "Round flat bread with toppings"),
            
            new WordEntry("telescope", DifficultyLevel.Hard, "science", "Device for viewing distant objects"),
            new WordEntry("philosophy", DifficultyLevel.Hard, "academic", "Study of fundamental questions"),
            new WordEntry("archaeology", DifficultyLevel.Hard, "science", "Study of ancient civilizations"),
            new WordEntry("mysterious", DifficultyLevel.Hard, "adjective", "Difficult to understand"),
            new WordEntry("helicopter", DifficultyLevel.Hard, "vehicle", "Aircraft with rotating blades"),
            new WordEntry("constitution", DifficultyLevel.Hard, "government", "Fundamental law document"),
            new WordEntry("refrigerator", DifficultyLevel.Hard, "appliance", "Appliance that keeps food cold"),
            new WordEntry("magnificent", DifficultyLevel.Hard, "adjective", "Extremely beautiful or impressive"),
            new WordEntry("constellation", DifficultyLevel.Hard, "astronomy", "Group of stars forming a pattern"),
            new WordEntry("university", DifficultyLevel.Hard, "education", "Higher education institution")
        });
        
        if (debugMode)
        {
            Debug.Log($"Generated {wordDatabase.Count} default words");
        }
    }
    
    private void AddWordsToDatabase(WordEntry[] words)
    {
        foreach (WordEntry word in words)
        {
            if (!wordDatabase.Any(w => w.word.Equals(word.word, System.StringComparison.OrdinalIgnoreCase)))
            {
                wordDatabase.Add(word);
            }
        }
    }
    
    public string GetRandomWord()
    {
        List<WordEntry> availableWords = GetAvailableWords();
        
        if (availableWords.Count == 0)
        {
            ResetUsedWords();
            availableWords = GetAvailableWords();
        }
        
        if (availableWords.Count == 0)
        {
            Debug.LogWarning("No words available in database!");
            return "default";
        }
        
        WordEntry selectedEntry = availableWords[random.Next(availableWords.Count)];
        usedWords.Add(selectedEntry.word);
        
        if (debugMode)
        {
            Debug.Log($"Selected word: {selectedEntry.word} (Difficulty: {selectedEntry.difficulty}, Category: {selectedEntry.category})");
        }
        
        return selectedEntry.word;
    }
    
    private List<WordEntry> GetAvailableWords()
    {
        return wordDatabase.Where(entry => 
            !usedWords.Contains(entry.word) &&
            entry.difficulty == currentDifficulty &&
            (!enableCategoryFiltering || string.IsNullOrEmpty(preferredCategory) || entry.category == preferredCategory)
        ).ToList();
    }
    
    public WordEntry GetWordEntry(string word)
    {
        return wordDatabase.FirstOrDefault(entry => 
            entry.word.Equals(word, System.StringComparison.OrdinalIgnoreCase));
    }
    
    public string GetWordHint(string word)
    {
        WordEntry entry = GetWordEntry(word);
        return entry?.hint ?? "No hint available";
    }
    
    public DifficultyLevel GetWordDifficulty(string word)
    {
        WordEntry entry = GetWordEntry(word);
        return entry?.difficulty ?? DifficultyLevel.Easy;
    }
    
    public void SetDifficulty(DifficultyLevel difficulty)
    {
        currentDifficulty = difficulty;
        ResetUsedWords();
        
        if (debugMode)
        {
            Debug.Log($"Difficulty changed to: {difficulty}");
        }
    }
    
    public void SetCategoryFilter(string category)
    {
        preferredCategory = category;
        enableCategoryFiltering = !string.IsNullOrEmpty(category);
        ResetUsedWords();
        
        if (debugMode)
        {
            Debug.Log($"Category filter set to: {category}");
        }
    }
    
    public void ResetUsedWords()
    {
        usedWords.Clear();
        if (debugMode)
        {
            Debug.Log("Used words list reset");
        }
    }
    
    public void AddCustomWord(string word, DifficultyLevel difficulty, string category = "", string hint = "")
    {
        if (!wordDatabase.Any(w => w.word.Equals(word, System.StringComparison.OrdinalIgnoreCase)))
        {
            wordDatabase.Add(new WordEntry(word, difficulty, category, hint));
            
            if (debugMode)
            {
                Debug.Log($"Added custom word: {word}");
            }
        }
    }
    
    public List<string> GetWordsByCategory(string category)
    {
        return wordDatabase
            .Where(entry => entry.category.Equals(category, System.StringComparison.OrdinalIgnoreCase))
            .Select(entry => entry.word)
            .ToList();
    }
    
    public List<string> GetWordsByDifficulty(DifficultyLevel difficulty)
    {
        return wordDatabase
            .Where(entry => entry.difficulty == difficulty)
            .Select(entry => entry.word)
            .ToList();
    }
    
    public List<string> GetAllCategories()
    {
        return wordDatabase
            .Where(entry => !string.IsNullOrEmpty(entry.category))
            .Select(entry => entry.category)
            .Distinct()
            .ToList();
    }
    
    public int GetWordCount()
    {
        return wordDatabase.Count;
    }
    
    public int GetAvailableWordCount()
    {
        return GetAvailableWords().Count;
    }
    
    public DifficultyLevel GetCurrentDifficulty()
    {
        return currentDifficulty;
    }
    
    public string GetCurrentCategory()
    {
        return enableCategoryFiltering ? preferredCategory : "All";
    }
}