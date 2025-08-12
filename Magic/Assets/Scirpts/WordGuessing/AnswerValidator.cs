using UnityEngine;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;

public class AnswerValidator : MonoBehaviour
{
    [Header("Validation Settings")]
    [SerializeField] private float similarityThreshold = 0.8f;
    [SerializeField] private bool allowTypos = true;
    [SerializeField] private bool caseSensitive = false;
    [SerializeField] private bool allowPartialMatches = false;
    [SerializeField] private int maxTypoDistance = 2;
    
    [Header("Text Processing")]
    [SerializeField] private bool removeArticles = true;
    [SerializeField] private bool removePunctuation = true;
    [SerializeField] private bool normalizeWhitespace = true;
    
    [Header("Debug")]
    [SerializeField] private bool debugMode = true;
    [SerializeField] private bool logValidationDetails = false;
    
    private readonly string[] articles = { "a", "an", "the" };
    private readonly string[] commonFillers = { "um", "uh", "er", "hmm", "well" };
    
    public bool ValidateAnswer(string userInput, string correctAnswer)
    {
        if (string.IsNullOrEmpty(userInput) || string.IsNullOrEmpty(correctAnswer))
        {
            if (debugMode) Debug.Log("Empty input or answer provided");
            return false;
        }
        
        string processedInput = ProcessText(userInput);
        string processedAnswer = ProcessText(correctAnswer);
        
        if (logValidationDetails)
        {
            Debug.Log($"Original Input: '{userInput}' -> Processed: '{processedInput}'");
            Debug.Log($"Original Answer: '{correctAnswer}' -> Processed: '{processedAnswer}'");
        }
        
        bool isValid = PerformValidation(processedInput, processedAnswer, userInput, correctAnswer);
        
        if (debugMode)
        {
            Debug.Log($"Validation result: {isValid} ('{userInput}' vs '{correctAnswer}')");
        }
        
        return isValid;
    }
    
    private bool PerformValidation(string processedInput, string processedAnswer, string originalInput, string originalAnswer)
    {
        if (IsExactMatch(processedInput, processedAnswer))
        {
            if (logValidationDetails) Debug.Log("Exact match found");
            return true;
        }
        
        if (allowPartialMatches && IsPartialMatch(processedInput, processedAnswer))
        {
            if (logValidationDetails) Debug.Log("Partial match found");
            return true;
        }
        
        if (allowTypos && IsTypoMatch(processedInput, processedAnswer))
        {
            if (logValidationDetails) Debug.Log("Typo match found");
            return true;
        }
        
        if (IsSimilarityMatch(processedInput, processedAnswer))
        {
            if (logValidationDetails) Debug.Log("Similarity match found");
            return true;
        }
        
        if (IsPhoneticMatch(processedInput, processedAnswer))
        {
            if (logValidationDetails) Debug.Log("Phonetic match found");
            return true;
        }
        
        return false;
    }
    
    private string ProcessText(string text)
    {
        if (string.IsNullOrEmpty(text)) return "";
        
        string processed = text;
        
        if (!caseSensitive)
            processed = processed.ToLowerInvariant();
        
        if (removePunctuation)
            processed = Regex.Replace(processed, @"[^\w\s]", "");
        
        if (normalizeWhitespace)
            processed = Regex.Replace(processed, @"\s+", " ").Trim();
        
        if (removeArticles)
            processed = RemoveArticles(processed);
        
        processed = RemoveFillers(processed);
        
        return processed;
    }
    
    private string RemoveArticles(string text)
    {
        string[] words = text.Split(' ');
        List<string> filteredWords = new List<string>();
        
        foreach (string word in words)
        {
            if (!articles.Contains(word.ToLowerInvariant()))
            {
                filteredWords.Add(word);
            }
        }
        
        return string.Join(" ", filteredWords);
    }
    
    private string RemoveFillers(string text)
    {
        string[] words = text.Split(' ');
        List<string> filteredWords = new List<string>();
        
        foreach (string word in words)
        {
            if (!commonFillers.Contains(word.ToLowerInvariant()))
            {
                filteredWords.Add(word);
            }
        }
        
        return string.Join(" ", filteredWords);
    }
    
    private bool IsExactMatch(string input, string answer)
    {
        return string.Equals(input, answer, caseSensitive ? System.StringComparison.Ordinal : System.StringComparison.OrdinalIgnoreCase);
    }
    
    private bool IsPartialMatch(string input, string answer)
    {
        return input.Contains(answer) || answer.Contains(input);
    }
    
    private bool IsTypoMatch(string input, string answer)
    {
        int distance = CalculateLevenshteinDistance(input, answer);
        return distance <= maxTypoDistance && distance < Mathf.Max(input.Length, answer.Length) * 0.3f;
    }
    
    private bool IsSimilarityMatch(string input, string answer)
    {
        float similarity = CalculateSimilarity(input, answer);
        return similarity >= similarityThreshold;
    }
    
    private bool IsPhoneticMatch(string input, string answer)
    {
        string phoneticInput = GetPhoneticCode(input);
        string phoneticAnswer = GetPhoneticCode(answer);
        return phoneticInput == phoneticAnswer;
    }
    
    private int CalculateLevenshteinDistance(string s1, string s2)
    {
        if (string.IsNullOrEmpty(s1)) return s2?.Length ?? 0;
        if (string.IsNullOrEmpty(s2)) return s1.Length;
        
        int[,] matrix = new int[s1.Length + 1, s2.Length + 1];
        
        for (int i = 0; i <= s1.Length; i++)
            matrix[i, 0] = i;
        for (int j = 0; j <= s2.Length; j++)
            matrix[0, j] = j;
        
        for (int i = 1; i <= s1.Length; i++)
        {
            for (int j = 1; j <= s2.Length; j++)
            {
                int cost = s1[i - 1] == s2[j - 1] ? 0 : 1;
                matrix[i, j] = Mathf.Min(
                    Mathf.Min(matrix[i - 1, j] + 1, matrix[i, j - 1] + 1),
                    matrix[i - 1, j - 1] + cost
                );
            }
        }
        
        return matrix[s1.Length, s2.Length];
    }
    
    private float CalculateSimilarity(string s1, string s2)
    {
        if (string.IsNullOrEmpty(s1) && string.IsNullOrEmpty(s2)) return 1.0f;
        if (string.IsNullOrEmpty(s1) || string.IsNullOrEmpty(s2)) return 0.0f;
        
        int distance = CalculateLevenshteinDistance(s1, s2);
        int maxLength = Mathf.Max(s1.Length, s2.Length);
        return 1.0f - (float)distance / maxLength;
    }
    
    private string GetPhoneticCode(string word)
    {
        if (string.IsNullOrEmpty(word)) return "";
        
        word = word.ToLowerInvariant();
        string code = "";
        
        foreach (char c in word)
        {
            switch (c)
            {
                case 'c':
                case 'k':
                case 'q':
                    code += "K";
                    break;
                case 'f':
                case 'v':
                    code += "F";
                    break;
                case 'g':
                case 'j':
                    code += "G";
                    break;
                case 's':
                case 'z':
                    code += "S";
                    break;
                case 'b':
                case 'p':
                    code += "B";
                    break;
                case 'd':
                case 't':
                    code += "T";
                    break;
                default:
                    if (char.IsLetter(c))
                        code += char.ToUpper(c);
                    break;
            }
        }
        
        return code;
    }
    
    public bool IsOnlyTargetWord(string input, string targetWord)
    {
        string processedInput = ProcessText(input);
        string processedTarget = ProcessText(targetWord);
        
        string[] inputWords = processedInput.Split(new char[] { ' ' }, System.StringSplitOptions.RemoveEmptyEntries);
        
        if (inputWords.Length == 1)
        {
            return ValidateAnswer(inputWords[0], processedTarget);
        }
        
        return false;
    }
    
    public float GetAnswerConfidence(string userInput, string correctAnswer)
    {
        string processedInput = ProcessText(userInput);
        string processedAnswer = ProcessText(correctAnswer);
        
        if (IsExactMatch(processedInput, processedAnswer))
            return 1.0f;
        
        float similarity = CalculateSimilarity(processedInput, processedAnswer);
        
        if (allowTypos && IsTypoMatch(processedInput, processedAnswer))
            similarity = Mathf.Max(similarity, 0.9f);
        
        if (IsPhoneticMatch(processedInput, processedAnswer))
            similarity = Mathf.Max(similarity, 0.85f);
        
        return similarity;
    }
    
    public void SetSimilarityThreshold(float threshold)
    {
        similarityThreshold = Mathf.Clamp01(threshold);
        if (debugMode) Debug.Log($"Similarity threshold set to: {similarityThreshold}");
    }
    
    public void SetTypoTolerance(bool allow, int maxDistance = 2)
    {
        allowTypos = allow;
        maxTypoDistance = maxDistance;
        if (debugMode) Debug.Log($"Typo tolerance: {allow}, Max distance: {maxDistance}");
    }
    
    public void SetCaseSensitivity(bool caseSensitive)
    {
        this.caseSensitive = caseSensitive;
        if (debugMode) Debug.Log($"Case sensitivity set to: {caseSensitive}");
    }
    
    public bool ValidateMultipleChoiceAnswer(string userInput, string[] possibleAnswers)
    {
        foreach (string answer in possibleAnswers)
        {
            if (ValidateAnswer(userInput, answer))
                return true;
        }
        return false;
    }
    
    public string GetBestMatchFromList(string userInput, string[] possibleAnswers)
    {
        float bestConfidence = 0f;
        string bestMatch = null;
        
        foreach (string answer in possibleAnswers)
        {
            float confidence = GetAnswerConfidence(userInput, answer);
            if (confidence > bestConfidence)
            {
                bestConfidence = confidence;
                bestMatch = answer;
            }
        }
        
        return bestConfidence >= similarityThreshold ? bestMatch : null;
    }
}