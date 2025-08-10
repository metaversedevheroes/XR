using UnityEngine;

#if UNITY_EDITOR
using UnityEditor;
#endif

public class TagManager : MonoBehaviour
{
    [Header("Required Tags")]
    public string[] requiredTags = new string[]
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
    [ContextMenu("Add Missing Tags")]
    public void AddMissingTags()
    {
        foreach (string tag in requiredTags)
        {
            AddTagIfNotExists(tag);
        }
        Debug.Log("[TagManager] All required tags have been added!");
    }

    private void AddTagIfNotExists(string tag)
    {
        // Get the TagManager asset
        SerializedObject tagManager = new SerializedObject(AssetDatabase.LoadAllAssetsAtPath("ProjectSettings/TagManager.asset")[0]);
        SerializedProperty tagsProp = tagManager.FindProperty("tags");

        // Check if tag already exists
        for (int i = 0; i < tagsProp.arraySize; i++)
        {
            SerializedProperty t = tagsProp.GetArrayElementAtIndex(i);
            if (t.stringValue.Equals(tag))
            {
                Debug.Log($"[TagManager] Tag '{tag}' already exists");
                return;
            }
        }

        // Add the tag
        tagsProp.InsertArrayElementAtIndex(tagsProp.arraySize);
        SerializedProperty newTagProp = tagsProp.GetArrayElementAtIndex(tagsProp.arraySize - 1);
        newTagProp.stringValue = tag;

        // Apply changes
        tagManager.ApplyModifiedProperties();
        Debug.Log($"[TagManager] Added tag: {tag}");
    }

    void Start()
    {
        // Auto-add tags on start if in editor
        #if UNITY_EDITOR
        if (Application.isPlaying)
        {
            AddMissingTags();
        }
        #endif
    }
#else
    void Start()
    {
        // Runtime warning for missing tags
        foreach (string tag in requiredTags)
        {
            try
            {
                GameObject.FindGameObjectWithTag(tag);
            }
            catch (UnityException)
            {
                Debug.LogWarning($"[TagManager] Tag '{tag}' is not defined. Please add it in the TagManager.");
            }
        }
    }
#endif
}