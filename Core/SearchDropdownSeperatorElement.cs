using UnityEngine;

namespace BX.Editor
{
    /// <summary>
    /// A seperator element used to draw a line.
    /// <br>Note : This element cannot have children.</br>
    /// </summary>
    public class SearchDropdownSeperatorElement : SearchDropdownElement
    {
        /// <summary>
        /// Color of the seperator drawn.
        /// </summary>
        public Color lineColor = Color.gray;
        /// <summary>
        /// Height of the seperator drawn.
        /// </summary>
        public int lineHeight = 2;
        /// <summary>
        /// Padding of the seperator drawn.
        /// </summary>
        public int lineMargin = 3;

        public override bool Interactable { get => false; set { } }

        /// <summary>
        /// Creates a default dropdown line seperator.
        /// <br>Color is <see cref="Color.gray"/>, height is 2 and padding is 3.</br>
        /// </summary>
        public SearchDropdownSeperatorElement() : base("")
        { }
        /// <summary>
        /// Defines a color to the line.
        /// </summary>
        public SearchDropdownSeperatorElement(Color lineColor) : base("")
        {
            this.lineColor = lineColor;
        }
        /// <summary>
        /// Defines a height and a padding to the line.
        /// </summary>
        public SearchDropdownSeperatorElement(int lineHeight, int lineMargin) : base("")
        {
            this.lineHeight = lineHeight;
            this.lineMargin = lineMargin;
        }
        /// <summary>
        /// Defines all color, height and padding to the line.
        /// </summary>
        public SearchDropdownSeperatorElement(Color lineColor, int lineHeight, int lineMargin) : base("")
        {
            this.lineColor = lineColor;
            this.lineHeight = lineHeight;
            this.lineMargin = lineMargin;
        }

        /// <summary>
        /// Returns <c><see cref="lineHeight"/> + <see cref="lineMargin"/></c>.
        /// </summary>
        public override float GetHeight(float viewWidth)
        {
            return lineHeight + lineMargin;
        }
        /// <summary>
        /// Draws the line.
        /// </summary>
        /// <inheritdoc cref="SearchDropdownElement.OnGUI(Rect, DropdownElementState)"/>
        public override void OnGUI(Rect position, DropdownElementState drawingState)
        {
            Rect drawRect = new(
                position.x - 2, position.y + (lineMargin / 2),
                position.width + 4, lineHeight
            );

            if (Event.current.type == EventType.Repaint)
            {
                Color gColor = GUI.color;
                GUI.color *= lineColor;
                GUI.DrawTexture(drawRect, Texture2D.whiteTexture);
                GUI.color = gColor;
            }
        }

        /// <summary>
        /// Throws <see cref="System.NotSupportedException"/> for this class.
        /// </summary>
        /// <exception cref="System.NotSupportedException"/>
        public override void Add(SearchDropdownElement item)
        {
            throw new System.NotSupportedException("[SearchDropdownSeperatorElement::Add] Given operation is not supported for this type of element.");
        }
        /// <summary>
        /// Throws <see cref="System.NotSupportedException"/> for this class.
        /// </summary>
        /// <exception cref="System.NotSupportedException"/>
        public override void Insert(int index, SearchDropdownElement item)
        {
            throw new System.NotSupportedException("[SearchDropdownSeperatorElement::Insert] Given operation is not supported for this type of element.");
        }
        /// <summary>
        /// Throws <see cref="System.NotSupportedException"/> for this class.
        /// </summary>
        /// <exception cref="System.NotSupportedException"/>
        public override bool Remove(SearchDropdownElement item)
        {
            throw new System.NotSupportedException("[SearchDropdownSeperatorElement::Remove] Given operation is not supported for this type of element.");
        }
        /// <summary>
        /// Throws <see cref="System.NotSupportedException"/> for this class.
        /// </summary>
        /// <exception cref="System.NotSupportedException"/>
        public override void Clear()
        {
            throw new System.NotSupportedException("[SearchDropdownSeperatorElement::Clear] Given operation is not supported for this type of element.");
        }

        // Uh, yes. Sorting this won't keep this on place.
        // So for this element, the sorting will be ignored.
        // TODO : Make elements ignored in their sorting?
        // Or preserve element order? (StableSort?)
        /// <summary>
        /// Always returns equal to the compared values.
        /// </summary>
        public override int CompareTo(SearchDropdownElement other)
        {
            return 0;
        }
    }
}
