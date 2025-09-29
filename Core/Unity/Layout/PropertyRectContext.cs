using UnityEngine;
using UnityEditor;

namespace BX.Editor.Utility
{
    /// <summary>
    /// A simpler way to get <see cref="PropertyDrawer"/>'s rects seperated with given height and padding (similar to <see cref="GUILayout"/> without using it)
    /// </summary>
    internal struct PropertyRectContext
    {
        /// <summary>
        /// The current Y elapsed for this rect context.
        /// <br>Can be reset to zero using <see cref="Reset"/> or be used for tallying the height (not recommended).</br>
        /// </summary>
        public float CurrentY => m_CurrentY;
        /// <inheritdoc cref="CurrentY"/>
        private float m_CurrentY;

        /// <summary>
        /// The given singular Y axis margin.
        /// </summary>
        public float YMargin { get; set; }

        /// <summary>
        /// Returns the <paramref name="property"/>'s rect.
        /// (by getting the height with <see cref="EditorGUI.GetPropertyHeight(SerializedProperty)"/>)
        /// </summary>
        public Rect GetPropertyRect(Rect baseRect, SerializedProperty property)
        {
            return GetPropertyRect(baseRect, EditorGUI.GetPropertyHeight(property));
        }
        /// <summary>
        /// Returns the base target rect.
        /// </summary>
        public Rect GetPropertyRect(Rect baseRect, float height)
        {
            baseRect.height = height;                  // set to target height
            baseRect.y += m_CurrentY + (YMargin / 2f); // offset by Y
            m_CurrentY += height + YMargin;            // add Y offset

            return baseRect;
        }

        /// <summary>
        /// Returns the next target rect for <paramref name="property"/> that is going to have it's height pushed.
        /// <br>This DOES NOT move the <see cref="CurrentY"/> in any way, use <see cref="GetPropertyRect(Rect, float)"/>.</br>
        /// </summary>
        public Rect PeekPropertyRect(Rect baseRect, SerializedProperty property)
        {
            return PeekPropertyRect(baseRect, EditorGUI.GetPropertyHeight(property));
        }
        /// <summary>
        /// Returns the next target rect that is going to have it's height pushed.
        /// <br>This DOES NOT move the <see cref="CurrentY"/> in any way, use <see cref="GetPropertyRect(Rect, float)"/>.</br>
        /// </summary>
        public Rect PeekPropertyRect(Rect baseRect, float height)
        {
            baseRect.height = height;                  // set to target height
            baseRect.y += m_CurrentY + (YMargin / 2f); // offset by Y
            // don't offset Y as this is a peek.

            return baseRect;
        }

        /// <summary>
        /// Resets the context's current Y positioning.
        /// <br>Can be used when the context is to be used for reserving new rects.</br>
        /// <br>Always call this before starting new contexts to not have the positions shift forever.</br>
        /// </summary>
        public void Reset()
        {
            m_CurrentY = 0f;
        }

        // /// <summary>
        // /// Creates a PropertyRectContext where the <see cref="YMargin"/> is 2f.
        // /// </summary>
        // public PropertyRectContext()
        // { }
        /// <summary>
        /// Creates a PropertyRectContext where the <see cref="YMargin"/> is the given parameter <paramref name="margin"/>.
        /// </summary>
        public PropertyRectContext(float margin)
        {
            m_CurrentY = 0f;
            YMargin = margin;
        }

        /// <summary>
        /// Converts the '<see cref="PropertyRectContext"/>' into information string.
        /// <br>May throw exceptions if <see cref="YMargin"/> was overriden and could throw an exception on it's getter.</br>
        /// </summary>
        public override string ToString()
        {
            return $"PropertyRectContext | CurrentY={m_CurrentY}, Margin={YMargin}";
        }
    }
}
