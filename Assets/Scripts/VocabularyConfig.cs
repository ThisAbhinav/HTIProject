using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Configuration for vocabulary and phrases to teach
/// Can be expanded to include specific word lists, difficulty levels, etc.
/// </summary>
[CreateAssetMenu(fileName = "VocabularyConfig", menuName = "Language Learning/Vocabulary Config")]
public class VocabularyConfig : ScriptableObject
{
    [System.Serializable]
    public class VocabularyWord
    {
        public string french;
        public string english;
        public string pronunciation; // Phonetic or simplified pronunciation
        public string exampleSentence;
        public WordDifficulty difficulty;
    }

    [System.Serializable]
    public class TopicVocabulary
    {
        public LearningTopic topic;
        public List<VocabularyWord> words = new List<VocabularyWord>();
        public List<string> commonPhrases = new List<string>();
    }

    public enum WordDifficulty
    {
        Beginner,
        Intermediate,
        Advanced
    }

    [Header("Vocabulary Lists")]
    public List<TopicVocabulary> topicVocabularies = new List<TopicVocabulary>();

    [Header("General Learning Settings")]
    public int wordsPerSession = 5;
    public bool enableProgressTracking = true;
    public bool repeatDifficultWords = true;

    // Helper method to get vocabulary for a specific topic
    public TopicVocabulary GetVocabularyForTopic(LearningTopic topic)
    {
        return topicVocabularies.Find(v => v.topic == topic);
    }

    // Helper method to get words by difficulty
    public List<VocabularyWord> GetWordsByDifficulty(LearningTopic topic, WordDifficulty difficulty)
    {
        var topicVocab = GetVocabularyForTopic(topic);
        if (topicVocab == null) return new List<VocabularyWord>();

        return topicVocab.words.FindAll(w => w.difficulty == difficulty);
    }

    // Initialize with default COFFEE SHOP ORDERING vocabulary
    public void InitializeDefaultCoffeeShop()
    {
        var coffeeVocab = new TopicVocabulary
        {
            topic = LearningTopic.CoffeeShop // <-- Ensure this exists in your enum
        };

        // Add coffee shop words
        coffeeVocab.words.AddRange(new List<VocabularyWord>
        {
            // Beginner
            new VocabularyWord { french = "le café", english = "coffee", pronunciation = "luh kah-fay", difficulty = WordDifficulty.Beginner },
            new VocabularyWord { french = "le thé", english = "tea", pronunciation = "luh tay", difficulty = WordDifficulty.Beginner },
            new VocabularyWord { french = "le menu", english = "menu", pronunciation = "luh muh-nyu", difficulty = WordDifficulty.Beginner },
            new VocabularyWord { french = "la tasse", english = "cup", pronunciation = "lah tass", difficulty = WordDifficulty.Beginner },
            new VocabularyWord { french = "commander", english = "to order", pronunciation = "koh-mahn-day", difficulty = WordDifficulty.Beginner },
            new VocabularyWord { french = "boire", english = "to drink", pronunciation = "bwar", difficulty = WordDifficulty.Beginner },
            new VocabularyWord { french = "le sucre", english = "sugar", pronunciation = "luh syu-kruh", difficulty = WordDifficulty.Beginner },
            new VocabularyWord { french = "le lait", english = "milk", pronunciation = "luh lay", difficulty = WordDifficulty.Beginner },

            // Intermediate
            new VocabularyWord { french = "l’addition", english = "the bill/check", pronunciation = "lah-dee-syohn", difficulty = WordDifficulty.Intermediate },
            new VocabularyWord { french = "à emporter", english = "to take away / to go", pronunciation = "ah ahm-por-tay", difficulty = WordDifficulty.Intermediate },
            new VocabularyWord { french = "sur place", english = "for here", pronunciation = "syur plass", difficulty = WordDifficulty.Intermediate },
            new VocabularyWord { french = "décaféiné", english = "decaf", pronunciation = "day-kah-fay-nay", difficulty = WordDifficulty.Intermediate },
            new VocabularyWord { french = "sans sucre", english = "without sugar", pronunciation = "sohn syu-kruh", difficulty = WordDifficulty.Intermediate },
            new VocabularyWord { french = "avec", english = "with", pronunciation = "ah-vek", difficulty = WordDifficulty.Intermediate },
            new VocabularyWord { french = "la glace", english = "ice", pronunciation = "lah glass", difficulty = WordDifficulty.Intermediate },

            // Advanced / barista terms (still useful for ordering)
            new VocabularyWord { french = "un espresso", english = "an espresso", pronunciation = "uhn ess-press-oh", difficulty = WordDifficulty.Advanced },
            new VocabularyWord { french = "un cappuccino", english = "a cappuccino", pronunciation = "uhn kah-poo-chee-no", difficulty = WordDifficulty.Advanced },
            new VocabularyWord { french = "un café au lait", english = "coffee with milk / café au lait", pronunciation = "uhn kah-fay oh lay", difficulty = WordDifficulty.Advanced },
            new VocabularyWord { french = "la mousse de lait", english = "milk foam", pronunciation = "lah mooss duh lay", difficulty = WordDifficulty.Advanced },
            new VocabularyWord { french = "la mouture", english = "grind (coffee)", pronunciation = "lah moo-tyur", difficulty = WordDifficulty.Advanced },
            new VocabularyWord { french = "le torréfacteur", english = "roaster", pronunciation = "luh toh-ray-fak-tur", difficulty = WordDifficulty.Advanced }
        });

        // Add common phrases (French – English)
        coffeeVocab.commonPhrases.AddRange(new List<string>
        {
            "Je voudrais un café, s’il vous plaît. - I’d like a coffee, please.",
            "Un cappuccino à emporter. - A cappuccino to go.",
            "Sur place ou à emporter ? - For here or to go?",
            "Avec du lait ou sans sucre ? - With milk or without sugar?",
            "Un café décaféiné, s’il vous plaît. - A decaf coffee, please.",
            "Quelle taille ? petite, moyenne ou grande ? - What size? small, medium or large?",
            "L’addition, s’il vous plaît. - The bill, please.",
            "Vous prenez la carte ? - Do you take cards?",
            "Un latte au lait d’avoine. - An oat-milk latte.",
            "Un espresso double, s’il vous plaît. - A double espresso, please."
        });

        topicVocabularies.Add(coffeeVocab);
    }

    // Initialize with default cooking vocabulary (kept as-is)
    public void InitializeDefaultCooking()
    {
        var cookingVocab = new TopicVocabulary
        {
            topic = LearningTopic.Cooking
        };

        cookingVocab.words.AddRange(new List<VocabularyWord>
        {
            new VocabularyWord { french = "la cuisine", english = "cooking/kitchen", pronunciation = "lah kwee-zeen", difficulty = WordDifficulty.Beginner },
            new VocabularyWord { french = "cuisiner", english = "to cook", pronunciation = "kwee-zee-nay", difficulty = WordDifficulty.Beginner },
            new VocabularyWord { french = "la recette", english = "recipe", pronunciation = "lah ruh-set", difficulty = WordDifficulty.Beginner },
            new VocabularyWord { french = "couper", english = "to cut", pronunciation = "koo-pay", difficulty = WordDifficulty.Beginner },
            new VocabularyWord { french = "mélanger", english = "to mix", pronunciation = "may-lon-zhay", difficulty = WordDifficulty.Beginner },
            new VocabularyWord { french = "cuire", english = "to cook", pronunciation = "kweer", difficulty = WordDifficulty.Beginner },
            new VocabularyWord { french = "le four", english = "oven", pronunciation = "luh foor", difficulty = WordDifficulty.Beginner },
            new VocabularyWord { french = "la casserole", english = "pot/pan", pronunciation = "lah kass-rol", difficulty = WordDifficulty.Beginner },
            new VocabularyWord { french = "le couteau", english = "knife", pronunciation = "luh koo-toh", difficulty = WordDifficulty.Beginner },
            new VocabularyWord { french = "les légumes", english = "vegetables", pronunciation = "lay lay-goom", difficulty = WordDifficulty.Beginner },
            new VocabularyWord { french = "ajouter", english = "to add", pronunciation = "ah-zhoo-tay", difficulty = WordDifficulty.Intermediate },
            new VocabularyWord { french = "assaisonner", english = "to season", pronunciation = "ah-say-zo-nay", difficulty = WordDifficulty.Intermediate }
        });

        cookingVocab.commonPhrases.AddRange(new List<string>
        {
            "Je fais la cuisine - I'm cooking",
            "Ajoute du sel - Add salt",
            "C'est délicieux - It's delicious",
            "Bon appétit - Enjoy your meal",
            "Coupe les légumes - Cut the vegetables"
        });

        topicVocabularies.Add(cookingVocab);
    }

#if UNITY_EDITOR
    // Helper method to initialize both topics (only in editor)
    [ContextMenu("Initialize All Default Vocabularies")]
    public void InitializeAllDefaults()
    {
        topicVocabularies.Clear();
        InitializeDefaultCoffeeShop();  // <-- uses Coffee Shop instead of Basketball
        InitializeDefaultCooking();
        UnityEditor.EditorUtility.SetDirty(this);
        Debug.Log("Initialized all default vocabularies!");
    }
#endif
}
