using UnityEngine;
using System;
using System.Reflection;

namespace BX.Editor
{
    /// <summary>
    /// Class providing content for the <see cref="SearchDropdownElement"/>.
    /// </summary>
    /// <remarks>
    /// This class exists because setting a property (such as <see cref="GUIContent.text"/>) 
    /// on <see cref="GUIContent"/> allocates some GC memory for something internal which we can't access.
    /// </remarks>
    [Serializable]
    public sealed class SearchDropdownElementContent : IEquatable<SearchDropdownElementContent>, ISearchDropdownElementContent
    {
        public string Text { get; set; } = string.Empty;
        public Texture Image { get; set; }
        public string Tooltip { get; set; } = string.Empty;

        public static readonly SearchDropdownElementContent None = new();

        public SearchDropdownElementContent()
        { }

        // i don't like this. *unswizzles your constructor*
        public SearchDropdownElementContent(string text)
        {
            Text = text;
        }
        public SearchDropdownElementContent(Texture image)
        {
            Image = image;
        }
        public SearchDropdownElementContent(string text, Texture image)
        {
            Text = text;
            Image = image;
        }
        public SearchDropdownElementContent(Texture image, string tooltip)
        {
            Image = image;
            Tooltip = tooltip;
        }
        public SearchDropdownElementContent(string text, string tooltip)
        {
            Text = text;
            Tooltip = tooltip;
        }
        public SearchDropdownElementContent(string text, Texture image, string tooltip)
        {
            Text = text;
            Image = image;
            Tooltip = tooltip;
        }

        private static readonly FieldInfo guiContentText;
        private static readonly FieldInfo guiContentImage;
        private static readonly FieldInfo guiContentTooltip;
        private static readonly FieldInfo guiContentWsText;
        private static readonly GUIContent tempContent;

        static SearchDropdownElementContent()
        {
            try
            {
                tempContent = new GUIContent(); // Don't refer GUIContent.none as that is a different global

                guiContentText = typeof(GUIContent).GetField("m_Text", BindingFlags.NonPublic | BindingFlags.Instance);
                guiContentImage = typeof(GUIContent).GetField("m_Image", BindingFlags.NonPublic | BindingFlags.Instance);
                guiContentTooltip = typeof(GUIContent).GetField("m_Tooltip", BindingFlags.NonPublic | BindingFlags.Instance);
                guiContentWsText = typeof(GUIContent).GetField("m_TextWithWhitespace", BindingFlags.NonPublic | BindingFlags.Instance);
            }
            catch (Exception e)
            {
                Debug.LogException(e);
                Debug.LogError("[SearchDropdownElement+Content::static ctor] This error is unexpected and likely because the unity internals has changed. Fallback will be run");
            }
        }

        /// <summary>
        /// Creates a reserved temporary content from this content.
        /// <br>This method is not thread safe.</br>
        /// </summary>
        public GUIContent ToTempGUIContent()
        {
            if (guiContentText == null || guiContentImage == null || guiContentTooltip == null || guiContentWsText == null)
            {
                tempContent.text = Text;
                tempContent.image = Image;
                tempContent.tooltip = Tooltip;
            }
            else
            {
                guiContentText.SetValue(tempContent, Text);
                guiContentWsText.SetValue(tempContent, Text);
                guiContentImage.SetValue(tempContent, Image);
                guiContentTooltip.SetValue(tempContent, Tooltip);
            }

            return tempContent;
        }

        public static bool operator ==(SearchDropdownElementContent lhs, SearchDropdownElementContent rhs)
        {
            if (lhs is null)
            {
                return rhs is null;
            }

            return lhs.Equals(rhs);
        }
        public static bool operator !=(SearchDropdownElementContent lhs, SearchDropdownElementContent rhs)
        {
            return !(lhs == rhs);
        }

        public override bool Equals(object obj)
        {
            if (obj is SearchDropdownElementContent content)
            {
                return Equals(content);
            }

            return false;
        }
        public bool Equals(SearchDropdownElementContent other)
        {
            return Text == other.Text && Image == other.Image && Tooltip == other.Tooltip;
        }
        public override int GetHashCode()
        {
            return HashCode.Combine(Text, Image, Tooltip);
        }
        public override string ToString()
        {
            return $"Text={Text}, Image={Image}, Tooltip={Tooltip}";
        }
    }
}
