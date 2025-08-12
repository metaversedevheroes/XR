using UnityEngine;
using System.Collections.Generic;
using System.Linq;

[System.Serializable]
public class EnglishSentence
{
    public string completeSentence;
    public string sentenceWithBlanks;
    public string[] correctAnswers;
    public string[] alternativeAnswers;
    public GrammarType grammarType;
    public int difficulty;
    public string hint;
    
    public EnglishSentence(string complete, string withBlanks, string[] correct, GrammarType type, int diff = 1, string hintText = "", string[] alternatives = null)
    {
        completeSentence = complete;
        sentenceWithBlanks = withBlanks;
        correctAnswers = correct;
        alternativeAnswers = alternatives ?? new string[0];
        grammarType = type;
        difficulty = diff;
        hint = hintText;
    }
}

public enum GrammarType
{
    PresentSimple,
    PastSimple,
    PresentContinuous,
    PastContinuous,
    PresentPerfect,
    FutureSimple,
    Comparative,
    Superlative,
    Modal,
    Conditional,
    Passive,
    Preposition
}

public class EnglishSentenceDatabase : MonoBehaviour
{
    [Header("Database Configuration")]
    [SerializeField] private bool debugMode = true;
    [SerializeField] private int minDifficulty = 1;
    [SerializeField] private int maxDifficulty = 3;
    
    private List<EnglishSentence> sentences = new List<EnglishSentence>();
    private List<string> vocabularyWords = new List<string>();
    
    private void Awake()
    {
        InitializeSentenceDatabase();
        InitializeVocabularyDatabase();
    }
    
    private void InitializeSentenceDatabase()
    {
        sentences.Clear();
        
        // Present Simple
        sentences.Add(new EnglishSentence(
            "I eat breakfast every morning",
            "I _____ breakfast every morning",
            new string[] { "eat" },
            GrammarType.PresentSimple, 1,
            "Use present tense for daily routines"
        ));
        
        sentences.Add(new EnglishSentence(
            "She goes to school by bus",
            "She _____ to school by bus",
            new string[] { "goes" },
            GrammarType.PresentSimple, 1,
            "Third person singular"
        ));
        
        sentences.Add(new EnglishSentence(
            "They play soccer on weekends",
            "They _____ soccer on weekends",
            new string[] { "play" },
            GrammarType.PresentSimple, 1,
            "Use base form with they"
        ));
        
        // Past Simple
        sentences.Add(new EnglishSentence(
            "I watched a movie yesterday",
            "I _____ a movie yesterday",
            new string[] { "watched" },
            GrammarType.PastSimple, 1,
            "Past tense of watch"
        ));
        
        sentences.Add(new EnglishSentence(
            "He went to the library last week",
            "He _____ to the library last week",
            new string[] { "went" },
            GrammarType.PastSimple, 2,
            "Irregular past tense of go"
        ));
        
        sentences.Add(new EnglishSentence(
            "We had lunch at twelve o'clock",
            "We _____ lunch at twelve o'clock",
            new string[] { "had" },
            GrammarType.PastSimple, 1,
            "Past tense of have"
        ));
        
        // Present Continuous
        sentences.Add(new EnglishSentence(
            "I am reading a book right now",
            "I _____ reading a book right now",
            new string[] { "am" },
            GrammarType.PresentContinuous, 2,
            "Use am with I"
        ));
        
        sentences.Add(new EnglishSentence(
            "She is cooking dinner in the kitchen",
            "She _____ cooking dinner in the kitchen",
            new string[] { "is" },
            GrammarType.PresentContinuous, 2,
            "Use is with she/he/it"
        ));
        
        sentences.Add(new EnglishSentence(
            "They are playing basketball outside",
            "They _____ playing basketball outside",
            new string[] { "are" },
            GrammarType.PresentContinuous, 2,
            "Use are with they/we/you"
        ));
        
        // Future Simple
        sentences.Add(new EnglishSentence(
            "I will visit my grandmother tomorrow",
            "I _____ visit my grandmother tomorrow",
            new string[] { "will" },
            GrammarType.FutureSimple, 2,
            "Use will for future plans"
        ));
        
        sentences.Add(new EnglishSentence(
            "It will rain this afternoon",
            "It _____ rain this afternoon",
            new string[] { "will" },
            GrammarType.FutureSimple, 2,
            "Future prediction"
        ));
        
        // Comparative & Superlative
        sentences.Add(new EnglishSentence(
            "This book is more interesting than that one",
            "This book is _____ interesting than that one",
            new string[] { "more" },
            GrammarType.Comparative, 2,
            "Use more with long adjectives"
        ));
        
        sentences.Add(new EnglishSentence(
            "She is the tallest student in our class",
            "She is the _____ student in our class",
            new string[] { "tallest" },
            GrammarType.Superlative, 2,
            "Add -est to short adjectives"
        ));
        
        sentences.Add(new EnglishSentence(
            "This is the most beautiful flower in the garden",
            "This is the _____ beautiful flower in the garden",
            new string[] { "most" },
            GrammarType.Superlative, 3,
            "Use most with long adjectives"
        ));
        
        // Modal verbs
        sentences.Add(new EnglishSentence(
            "You should study hard for the test",
            "You _____ study hard for the test",
            new string[] { "should" },
            GrammarType.Modal, 2,
            "Modal for advice"
        ));
        
        sentences.Add(new EnglishSentence(
            "I can speak English and Korean",
            "I _____ speak English and Korean",
            new string[] { "can" },
            GrammarType.Modal, 1,
            "Modal for ability"
        ));
        
        sentences.Add(new EnglishSentence(
            "May I go to the bathroom",
            "_____ I go to the bathroom",
            new string[] { "May" },
            GrammarType.Modal, 2,
            "Modal for permission"
        ));
        
        // Prepositions
        sentences.Add(new EnglishSentence(
            "The cat is sleeping on the sofa",
            "The cat is sleeping _____ the sofa",
            new string[] { "on" },
            GrammarType.Preposition, 1,
            "Preposition of place"
        ));
        
        sentences.Add(new EnglishSentence(
            "I have been waiting for you since morning",
            "I have been waiting _____ you since morning",
            new string[] { "for" },
            GrammarType.Preposition, 2,
            "Preposition with waiting"
        ));
        
        sentences.Add(new EnglishSentence(
            "The meeting starts at three o'clock",
            "The meeting starts _____ three o'clock",
            new string[] { "at" },
            GrammarType.Preposition, 1,
            "Preposition of time"
        ));
        
        // More complex sentences
        sentences.Add(new EnglishSentence(
            "If I were you I would study more",
            "If I _____ you, I would study more",
            new string[] { "were" },
            GrammarType.Conditional, 3,
            "Second conditional"
        ));
        
        sentences.Add(new EnglishSentence(
            "The homework was completed by all students",
            "The homework was _____ by all students",
            new string[] { "completed" },
            GrammarType.Passive, 3,
            "Passive voice"
        ));
        
        if (debugMode) Debug.Log($"Initialized {sentences.Count} sentences in database");
    }
    
    private void InitializeVocabularyDatabase()
    {
        vocabularyWords.Clear();
        
        // Common middle school vocabulary
        vocabularyWords.AddRange(new string[]
        {
            // Animals
            "cat", "dog", "bird", "fish", "elephant", "tiger", "lion", "bear",
            
            // Food
            "apple", "banana", "bread", "milk", "water", "rice", "chicken", "pizza",
            
            // School
            "book", "pen", "pencil", "desk", "chair", "teacher", "student", "classroom",
            
            // Family
            "mother", "father", "brother", "sister", "grandmother", "grandfather",
            
            // Colors
            "red", "blue", "green", "yellow", "black", "white", "orange", "purple",
            
            // Numbers
            "one", "two", "three", "four", "five", "six", "seven", "eight", "nine", "ten",
            
            // Actions
            "run", "walk", "jump", "swim", "read", "write", "sing", "dance", "cook", "clean",
            
            // Weather
            "sunny", "rainy", "cloudy", "windy", "hot", "cold", "warm", "cool",
            
            // Time
            "morning", "afternoon", "evening", "night", "today", "tomorrow", "yesterday"
        });
        
        if (debugMode) Debug.Log($"Initialized {vocabularyWords.Count} vocabulary words");
    }
    
    public EnglishSentence GetRandomSentence()
    {
        var availableSentences = sentences.Where(s => s.difficulty >= minDifficulty && s.difficulty <= maxDifficulty).ToList();
        
        if (availableSentences.Count == 0)
        {
            availableSentences = sentences;
        }
        
        int randomIndex = Random.Range(0, availableSentences.Count);
        return availableSentences[randomIndex];
    }
    
    public EnglishSentence GetSentenceByGrammarType(GrammarType grammarType)
    {
        var filteredSentences = sentences.Where(s => s.grammarType == grammarType).ToList();
        
        if (filteredSentences.Count == 0)
        {
            return GetRandomSentence();
        }
        
        int randomIndex = Random.Range(0, filteredSentences.Count);
        return filteredSentences[randomIndex];
    }
    
    public List<string> GetVocabularySequence(int count = 3)
    {
        List<string> sequence = new List<string>();
        List<string> availableWords = new List<string>(vocabularyWords);
        
        for (int i = 0; i < count && availableWords.Count > 0; i++)
        {
            int randomIndex = Random.Range(0, availableWords.Count);
            sequence.Add(availableWords[randomIndex]);
            availableWords.RemoveAt(randomIndex);
        }
        
        return sequence;
    }
    
    public bool ValidateAnswer(string userAnswer, EnglishSentence sentence)
    {
        if (sentence == null || string.IsNullOrEmpty(userAnswer))
        {
            return false;
        }
        
        string cleanAnswer = userAnswer.Trim().ToLower();
        
        // Check against correct answers
        foreach (string correct in sentence.correctAnswers)
        {
            if (cleanAnswer.Equals(correct.ToLower()))
            {
                return true;
            }
        }
        
        // Check against alternative answers
        foreach (string alternative in sentence.alternativeAnswers)
        {
            if (cleanAnswer.Equals(alternative.ToLower()))
            {
                return true;
            }
        }
        
        return false;
    }
    
    public bool ValidateCompleteSentence(string userSentence, EnglishSentence sentence)
    {
        if (sentence == null || string.IsNullOrEmpty(userSentence))
        {
            return false;
        }
        
        string cleanUserSentence = userSentence.Trim().ToLower().Replace(".", "").Replace(",", "");
        string cleanCorrectSentence = sentence.completeSentence.ToLower().Replace(".", "").Replace(",", "");
        
        // Check for exact match
        if (cleanUserSentence.Equals(cleanCorrectSentence))
        {
            return true;
        }
        
        // Check if user sentence contains all the correct words
        string[] userWords = cleanUserSentence.Split(' ');
        string[] correctWords = cleanCorrectSentence.Split(' ');
        
        foreach (string correctWord in correctWords)
        {
            if (!userWords.Contains(correctWord))
            {
                return false;
            }
        }
        
        return true;
    }
    
    public void SetDifficultyRange(int min, int max)
    {
        minDifficulty = Mathf.Clamp(min, 1, 3);
        maxDifficulty = Mathf.Clamp(max, 1, 3);
        
        if (debugMode) Debug.Log($"Difficulty range set to {minDifficulty}-{maxDifficulty}");
    }
    
    public List<EnglishSentence> GetAllSentences()
    {
        return new List<EnglishSentence>(sentences);
    }
    
    public int GetSentenceCount()
    {
        return sentences.Count;
    }
    
    public int GetVocabularyCount()
    {
        return vocabularyWords.Count;
    }
}