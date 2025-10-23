using System.Text.RegularExpressions;
using UnityEngine;

/// <summary>
/// Utility class for cleaning text for Text-to-Speech output
/// Removes markdown, emojis, and formats text for natural speech
/// </summary>
public static class TextCleanerUtility
{
    /// <summary>
    /// Comprehensive text cleaning for TTS
    /// </summary>
    public static string CleanForSpeech(string text)
    {
        if (string.IsNullOrEmpty(text)) return text;

        // Store original for debugging
        string original = text;

        // 1. Remove markdown formatting
        text = RemoveMarkdown(text);

        // 2. Remove emojis
        text = RemoveEmojis(text);

        // 3. Handle special punctuation
        text = NormalizePunctuation(text);

        // 4. Remove special characters that TTS might spell out
        text = RemoveProblematicCharacters(text);

        // 5. Clean up spacing
        text = NormalizeWhitespace(text);

        Debug.Log($"[TextCleaner] Original: {original.Substring(0, Mathf.Min(50, original.Length))}...");
        Debug.Log($"[TextCleaner] Cleaned: {text.Substring(0, Mathf.Min(50, text.Length))}...");

        return text;
    }

    /// <summary>
    /// Remove all markdown formatting
    /// </summary>
    private static string RemoveMarkdown(string text)
    {
        // Bold: **text** or __text__
        text = Regex.Replace(text, @"\*\*(.+?)\*\*", "$1");
        text = Regex.Replace(text, @"__(.+?)__", "$1");

        // Italic: *text* or _text_
        text = Regex.Replace(text, @"\*(.+?)\*", "$1");
        text = Regex.Replace(text, @"_(.+?)_", "$1");

        // Strikethrough: ~~text~~
        text = Regex.Replace(text, @"~~(.+?)~~", "$1");

        // Headers: # ## ### etc
        text = Regex.Replace(text, @"^#{1,6}\s*", "", RegexOptions.Multiline);

        // Links: [text](url) -> text
        text = Regex.Replace(text, @"\[([^\]]+)\]\([^\)]+\)", "$1");

        // Images: ![alt](url) -> remove entirely
        text = Regex.Replace(text, @"!\[([^\]]*)\]\([^\)]+\)", "");

        // Code blocks: ```code```
        text = Regex.Replace(text, @"```[\s\S]*?```", "");

        // Inline code: `code`
        text = Regex.Replace(text, @"`(.+?)`", "$1");

        // Blockquotes: > text
        text = Regex.Replace(text, @"^>\s*", "", RegexOptions.Multiline);

        // Horizontal rules: --- or ***
        text = Regex.Replace(text, @"^[-*_]{3,}\s*$", "", RegexOptions.Multiline);

        // Bullet points and list markers
        text = Regex.Replace(text, @"^\s*[-*+•]\s*", "", RegexOptions.Multiline);
        text = Regex.Replace(text, @"^\s*\d+\.\s*", "", RegexOptions.Multiline);

        return text;
    }

    /// <summary>
    /// Remove emoji characters using character-by-character filtering
    /// </summary>
    private static string RemoveEmojis(string text)
    {
        if (string.IsNullOrEmpty(text)) return text;

        System.Text.StringBuilder result = new System.Text.StringBuilder();
        
        for (int i = 0; i < text.Length; i++)
        {
            char c = text[i];
            int codePoint = char.ConvertToUtf32(text, i);
            
            // Check if this is a high surrogate (part of a surrogate pair)
            if (char.IsHighSurrogate(c))
            {
                // Skip this character and the next (low surrogate) if it's an emoji
                if (IsEmoji(codePoint))
                {
                    i++; // Skip the low surrogate too
                    continue;
                }
            }
            
            // Check single characters
            if (!IsEmoji(codePoint))
            {
                result.Append(c);
            }
        }
        
        return result.ToString();
    }

    /// <summary>
    /// Check if a Unicode code point is an emoji
    /// </summary>
    private static bool IsEmoji(int codePoint)
    {
        return (codePoint >= 0x1F600 && codePoint <= 0x1F64F) || // Emoticons
               (codePoint >= 0x1F300 && codePoint <= 0x1F5FF) || // Misc Symbols and Pictographs
               (codePoint >= 0x1F680 && codePoint <= 0x1F6FF) || // Transport and Map
               (codePoint >= 0x1F1E0 && codePoint <= 0x1F1FF) || // Flags
               (codePoint >= 0x2600 && codePoint <= 0x26FF) ||   // Misc symbols
               (codePoint >= 0x2700 && codePoint <= 0x27BF) ||   // Dingbats
               (codePoint >= 0x1F900 && codePoint <= 0x1F9FF) || // Supplemental Symbols and Pictographs
               (codePoint >= 0x1FA00 && codePoint <= 0x1FA6F) || // Chess Symbols
               (codePoint >= 0x1FA70 && codePoint <= 0x1FAFF) || // Symbols and Pictographs Extended-A
               (codePoint >= 0x2300 && codePoint <= 0x23FF) ||   // Misc Technical
               (codePoint >= 0xFE00 && codePoint <= 0xFE0F) ||   // Variation Selectors
               (codePoint >= 0x1F000 && codePoint <= 0x1F02F) || // Mahjong Tiles
               (codePoint >= 0x1F0A0 && codePoint <= 0x1F0FF);   // Playing Cards
    }

    /// <summary>
    /// Normalize punctuation for better TTS output
    /// </summary>
    private static string NormalizePunctuation(string text)
    {
        // Replace ellipsis with period
        text = text.Replace("...", ".");
        text = Regex.Replace(text, @"\.{2,}", ".");

        // Replace multiple exclamation marks
        text = Regex.Replace(text, @"!{2,}", "!");

        // Replace multiple question marks
        text = Regex.Replace(text, @"\?{2,}", "?");

        // Remove interrobang and replace with question mark
        text = text.Replace("?", "?");

        // Replace em dash and en dash with comma or space
        text = text.Replace("—", ", ");
        text = text.Replace("–", ", ");
        text = text.Replace("?", ", ");

        // Remove quotation marks that might be read aloud
        // text = text.Replace(""", "\"");
        // text = text.Replace(""", "\"");
        text = text.Replace("'", "'");
        text = text.Replace("'", "'");

        // Remove parentheses content if it's just punctuation/formatting
        text = Regex.Replace(text, @"\([^\w\s]*\)", "");

        return text;
    }

    /// <summary>
    /// Remove characters that TTS might spell out
    /// </summary>
    private static string RemoveProblematicCharacters(string text)
    {
        // Remove hashtags (but keep the text after)
        text = Regex.Replace(text, @"#(\w+)", "$1");

        // Remove @ mentions (but keep the name)
        text = Regex.Replace(text, @"@(\w+)", "$1");

        // Remove asterisks that might remain
        text = text.Replace("*", "");

        // Remove underscores
        text = text.Replace("_", " ");

        // Remove backslashes
        text = text.Replace("\\", "");

        // Remove vertical bars
        text = text.Replace("|", "");

        // Remove carets
        text = text.Replace("^", "");

        // Remove tildes
        text = text.Replace("~", "");

        // Remove angle brackets
        text = text.Replace("<", "");
        text = text.Replace(">", "");

        // Remove curly braces
        text = text.Replace("{", "");
        text = text.Replace("}", "");

        // Remove square brackets
        text = text.Replace("[", "");
        text = text.Replace("]", "");

        return text;
    }

    /// <summary>
    /// Normalize whitespace and line breaks
    /// </summary>
    private static string NormalizeWhitespace(string text)
    {
        // Replace multiple line breaks with single space
        text = Regex.Replace(text, @"[\r\n]+", " ");

        // Replace multiple spaces with single space
        text = Regex.Replace(text, @"\s+", " ");

        // Trim leading and trailing whitespace
        text = text.Trim();

        return text;
    }

    /// <summary>
    /// Clean text but preserve basic formatting for display
    /// </summary>
    public static string CleanForDisplay(string text)
    {
        if (string.IsNullOrEmpty(text)) return text;

        // Only remove emojis and normalize excessive punctuation
        text = RemoveEmojis(text);
        text = Regex.Replace(text, @"!{3,}", "!!");
        text = Regex.Replace(text, @"\?{3,}", "??");
        text = Regex.Replace(text, @"\.{4,}", "...");

        return text;
    }
}