using UnityEditor;
using UnityEngine;
using System;
using System.Collections;
using System.Collections.Generic;
using BX.Editor.Utility;

namespace BX.Editor
{
    // TODO:
    // * Create ISearchDropdownElement as each element controls/draws the GUI.

    /// <summary>
    /// GUI drawing state for the element.
    /// <br><see cref="Default"/> : The default drawing behaviour.</br>
    /// <br><see cref="Selected"/> : Selected element drawing behaviour.</br>
    /// <br><see cref="Hover"/> : Hovered element drawing behaviour.</br>
    /// <br><see cref="Clicked"/> : Clicked/Pressed drawing behaviour.</br>
    /// </summary>
    public enum DropdownElementState
    {
        Default,
        Selected,
        Hover,
        Clicked,
    }

    /// <summary>
    /// A search dropdown element.
    /// <br>Can define it's own GUI, etc.</br>
    /// </summary>
    public class SearchDropdownElement : ICollection<SearchDropdownElement>, IComparable<SearchDropdownElement>, IEquatable<SearchDropdownElement>
    {
        /// <summary>
        /// The content that this element contains.
        /// </summary>
        public SearchDropdownElementContent content = SearchDropdownElementContent.None;
        /// <summary>
        /// Whether if this element is selected.
        /// <br/>
        /// <br>Setting this will effect the GUI on the default <see cref="OnGUI(Rect, DropdownElementState)"/>.</br>
        /// </summary>
        public bool Selected { get; set; } = false;
        /// <summary>
        /// Whether if this element can be interacted with.
        /// <br>Setting this <see langword="false"/> will set the state constantly to Default and make this element unselectable.</br>
        /// </summary>
        public virtual bool Interactable { get; set; } = true;
        /// <summary>
        /// Whether if this element requests a repaint.
        /// <br>Useful for constantly updating elements.</br>
        /// </summary>
        public virtual bool RequestsRepaint { get; set; } = false;
        /// <summary>
        /// Extra data for queries containing the index for ordering.
        /// <br>Default value is <see cref="int.MaxValue"/> for unordered values.</br>
        /// <br/>
        /// <br>This value should be unset (but isn't required to be unset) when query is cleared.</br>
        /// </summary>
        public int IndexOfQuery { get; set; } = int.MaxValue;
        /// <summary>
        /// Extra data for queries, to supply the size of the query if the query is matched.
        /// <br>Default vaule is 0.</br>
        /// <br/>
        /// <br>This value should be unset when query is cleared.</br>
        /// </summary>
        public int SizeOfQuery { get; set; } = 0;

        /// <summary>
        /// The rectangle reserving context.
        /// <br>The values can be overriden/changed.</br>
        /// </summary>
        internal readonly PropertyRectContext ctx = new(2f);

        /// <summary>
        /// Internal contained children.
        /// </summary>
        private List<SearchDropdownElement> m_Children;
        private bool m_BuiltWithoutChildren = false;
        /// <summary>
        /// Checks <see cref="m_Children"/> (and <see cref="m_BuiltWithoutChildren"/>. This field exists 
        /// to avoid unnecessary heap allocation for elements w/o children)
        /// </summary>
        /// <param name="required">
        /// Whether if the action you are doing requires the presence of the children list.
        /// If this is <see langword="true"/> and children list doesn't exist, this method will 
        /// always return <see langword="true"/> and the children will always be available for the next code.
        /// </param>
        /// <returns><see langword="true"/> if the <see cref="m_Children"/> is available, otherwise <see langword="false"/></returns>
        protected bool CheckChildren(bool required)
        {
            if (m_BuiltWithoutChildren)
            {
                if (required)
                {
                    m_Children = new();
                    m_BuiltWithoutChildren = false;

                    return true;
                }

                return false;
            }

            return true;
        }
        /// <summary>
        /// Whether if this element has children.
        /// </summary>
        public bool HasChildren => Count > 0;
        /// <summary>
        /// Size of the children contained.
        /// </summary>
        public int Count
        {
            get
            {
                if (!CheckChildren(required: false))
                {
                    return 0;
                }

                return m_Children.Count;
            }
        }

        /// <summary>
        /// Capacity reserved for the children.
        /// <br>Changing this allocates more memory for the internal children array.</br>
        /// </summary>
        public int ChildCapacity
        {
            get
            {
                if (!CheckChildren(required: false))
                {
                    return 0;
                }

                return m_Children.Capacity;
            }
            set
            {
                CheckChildren(required: true);

                m_Children.Capacity = Mathf.Clamp(value, 0, int.MaxValue);
            }
        }
        public bool IsReadOnly => false;
        /// <summary>
        /// An indiced access operator for this element.
        /// </summary>
        public SearchDropdownElement this[int index] => m_Children[index];

        /// <inheritdoc cref="SearchDropdownElement(SearchDropdownElementContent)"/>
        public SearchDropdownElement(string label)
            : this(new SearchDropdownElementContent(label))
        { }
        /// <inheritdoc cref="SearchDropdownElement(SearchDropdownElementContent)"/>
        public SearchDropdownElement(string label, string tooltip)
            : this(new SearchDropdownElementContent(label, tooltip))
        { }

        /// <inheritdoc cref="SearchDropdownElement(SearchDropdownElementContent, int)"/>
        public SearchDropdownElement(string label, int childrenCapacity)
            : this(new SearchDropdownElementContent(label), childrenCapacity)
        { }
        /// <inheritdoc cref="SearchDropdownElement(SearchDropdownElementContent, int)"/>
        public SearchDropdownElement(string label, string tooltip, int childrenCapacity)
            : this(new SearchDropdownElementContent(label, tooltip), childrenCapacity)
        { }

        /// <summary>
        /// Creates an <see cref="SearchDropdownElement"/> with content assigned.
        /// </summary>
        public SearchDropdownElement(SearchDropdownElementContent content)
        {
            this.content = content;
            // It is better to reserve without children. Only create children list when access is required.
            m_BuiltWithoutChildren = true;
        }
        /// <summary>
        /// Creates an <see cref="SearchDropdownElement"/> with content assigned.
        /// <br>The child capacity can be defined for chunking/memory optimization.</br>
        /// </summary>
        public SearchDropdownElement(SearchDropdownElementContent content, int childrenCapacity)
        {
            this.content = content;
            m_Children = new List<SearchDropdownElement>(childrenCapacity);
        }

        // -- This approach is fine as we will only draw few elements at once, not all of them
        // Only the 'GetHeight' may get called too many times sometimes
        /// <summary>
        /// Returns the height of the default element.
        /// <br>The default height is <see cref="EditorGUIUtility.singleLineHeight"/> + <see cref="ctx"/>.Padding</br>
        /// <br/>
        /// <br>
        /// <b>Warning</b> : This method affects performance vastly (for the time being). Override it carefully, ensure that this is not intensive. 
        /// (this method will be slow until the heights of non-visible is cached).
        /// </br>
        /// </summary>
        /// <param name="viewWidth">Width of the draw area allocated for this element.</param>
        public virtual float GetHeight(float viewWidth)
        {
            // Calling 'GUI.skin.label.CalcHeight' like 10000 times is most likely very not good and laggy
            // This is the _"only"_ performance bottleneck; we can compute and spawn an giant rect.
            // When we scroll lower, while the giant rect is visible recompute GUI for the objects that were supposedly visible, checked in a virtual list.
            // Or cache the Height in some way. Fortunately, this returns a constant "in most cases" and is fast if no GUI computations are done.
            return EditorGUIUtility.singleLineHeight + ctx.YMargin;
        }
        /// <summary>
        /// Draws the GUI of the default element.
        /// <br>The default element has the following : An icon on the left and the description on the right.</br>
        /// </summary>
        /// <param name="position">Position to draw the GUI on.</param>
        /// <param name="drawingState">
        /// The element state, depending on the cursor interaction.
        /// Elements can ignore this all together and use the <see cref="Event.current"/> but the default behaviour doesn't.
        /// <br>This is just a more convenient way of receiveing events.</br>
        /// <br>
        /// <b>Warning : </b> Check the <see cref="Event.current"/> for this state's correctness.
        /// This event is usually only correct in <see cref="EventType.Repaint"/>.
        /// </br>
        /// </param>
        public virtual void OnGUI(Rect position, DropdownElementState drawingState)
        {
            // * Left           : Reserve a Icon drawing rect
            // * Left <-> Right : The text drawing rect
            // * Right          : Arrow to display if the menu has children.
            // -- Icon rect width is EditorGUIUtility.singleLineHeight, same as the right arrow
            // -- Horizontal padding is 5f
            ctx.Reset();
            Rect contextRect = ctx.GetPropertyRect(position, EditorGUIUtility.singleLineHeight);

            Rect iconRect = new Rect(contextRect)
            {
                width = EditorGUIUtility.singleLineHeight
            };
            Rect textRect = new Rect(contextRect)
            {
                x = contextRect.x + EditorGUIUtility.singleLineHeight + 5f,
                width = contextRect.width - (iconRect.width + EditorGUIUtility.singleLineHeight)
            };

            // -- Background box tint
            Color stateColor = new Color(0.2f, 0.2f, 0.2f);
            switch (drawingState)
            {
                case DropdownElementState.Selected:
                    stateColor = new Color(0.15f, 0.35f, 0.39f);
                    break;
                case DropdownElementState.Hover:
                    stateColor = new Color(0.15f, 0.15f, 0.15f);
                    break;
                case DropdownElementState.Clicked:
                    stateColor = new Color(0.1f, 0.1f, 0.1f);
                    break;

                default:
                    break;
            }
            EditorGUI.DrawRect(position, stateColor);
            // -- Elements
            if (content.Image != null)
            {
                GUI.DrawTexture(iconRect, content.Image, ScaleMode.ScaleToFit);
            }
            // This also sets tooltips, etc.
            // Since we can't do this in a nice way, let's just call "GUIContent.Temp()", which nicely sets our values for us without GC.
            GUI.Label(textRect, content.Text, UGUISearchDropdownWindow.StyleList.LabelStyle);
        }

        /// <summary>
        /// Adds a child element to this element.
        /// </summary>
        /// <param name="item">The item to add. This mustn't be null. The overriding method should assert this unless null is suitable.</param>
        public virtual void Add(SearchDropdownElement item)
        {
            if (item == null)
            {
                throw new ArgumentNullException(nameof(item), "[SearchDropdownElement::Add] Given argument was null.");
            }

            CheckChildren(required: true);
            m_Children.Add(item);
        }
        /// <summary>
        /// Insert a <paramref name="item"/> in given <paramref name="index"/>.
        /// </summary>
        /// <exception cref="ArgumentNullException"></exception>
        public virtual void Insert(int index, SearchDropdownElement item)
        {
            if (item == null)
            {
                throw new ArgumentNullException(nameof(item), "[SearchDropdownElement::Insert] Given argument was null.");
            }

            CheckChildren(required: true);
            m_Children.Insert(index, item);
        }
        /// <summary>
        /// Clears all children inside this element.
        /// </summary>
        public virtual void Clear()
        {
            // If children doesn't exist, there is nothing to clear.
            if (!CheckChildren(required: false))
            {
                return;
            }

            m_Children.Clear();
        }
        /// <summary>
        /// Returns whether if this element contains child <paramref name="item"/>.
        /// </summary>
        public bool Contains(SearchDropdownElement item)
        {
            if (!CheckChildren(required: false))
            {
                return false;
            }

            return m_Children.Contains(item);
        }
        /// <summary>
        /// Copies self into the <paramref name="array"/>.
        /// </summary>
        public void CopyTo(SearchDropdownElement[] array, int arrayIndex)
        {
            if (!CheckChildren(required: false))
            {
                return;
            }

            m_Children.CopyTo(array, arrayIndex);
        }
        /// <summary>
        /// Removes a child element if it exists.
        /// </summary>
        /// <returns><see langword="true"/> if the element was removed, <see langword="false"/> otherwise.</returns>
        public virtual bool Remove(SearchDropdownElement item)
        {
            if (!CheckChildren(required: false))
            {
                return false;
            }

            return m_Children.Remove(item);
        }
        /// <summary>
        /// Sorts the children of this element. Does not include other elements with children inside this element.
        /// </summary>
        public void Sort()
        {
            if (!CheckChildren(required: false))
            {
                return;
            }

            m_Children.Sort();
        }
        /// <inheritdoc cref="Sort"/>
        public void Sort(IComparer<SearchDropdownElement> comparer)
        {
            if (!CheckChildren(required: false))
            {
                return;
            }

            m_Children.Sort(comparer);
        }
        /// <inheritdoc cref="Sort"/>
        public void Sort(Comparison<SearchDropdownElement> comparison)
        {
            if (!CheckChildren(required: false))
            {
                return;
            }

            m_Children.Sort(comparison);
        }

        /// <summary>
        /// Sorts all children recursively.
        /// </summary>
        public void SortAll()
        {
            if (!CheckChildren(required: false))
            {
                return;
            }

            Sort();
            // This will iterate until the last child without the child
            foreach (SearchDropdownElement child in m_Children)
            {
                child.SortAll();
            }
        }
        /// <inheritdoc cref="SortAll"/>
        public void SortAll(IComparer<SearchDropdownElement> comparer)
        {
            if (!CheckChildren(required: false))
            {
                return;
            }

            Sort(comparer);
            foreach (SearchDropdownElement child in m_Children)
            {
                child.SortAll(comparer);
            }
        }
        /// <inheritdoc cref="SortAll"/>
        public void SortAll(Comparison<SearchDropdownElement> comparison)
        {
            if (!CheckChildren(required: false))
            {
                return;
            }

            Sort(comparison);
            foreach (SearchDropdownElement child in m_Children)
            {
                child.SortAll(comparison);
            }
        }

        public IEnumerator<SearchDropdownElement> GetEnumerator()
        {
            if (!CheckChildren(required: false))
            {
                yield break;
            }

            foreach (SearchDropdownElement elem in m_Children)
            {
                yield return elem;
            }
        }
        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }

        /// <summary>
        /// Used for sortability.
        /// <br>This can be overridden to change the default item value.</br>
        /// </summary>
        public virtual int CompareTo(SearchDropdownElement other)
        {
            // Always larger than null element
            if (other == null)
            {
                return 1;
            }
            if (other.content == null)
            {
                return content == null ? 0 : 1;
            }

            // Strip 'content.text' from rich text if parent allows rich text?
            // TODO : Either determine that using more references (yay, more code and bloat) or allow the user to manually override this
            // For the time being just do normal comparisons as if the rich texted stuff is colored similarly the comparison shouldn't be hurt much.
            return content.Text.CompareTo(other.content.Text);
        }

        /// <summary>
        /// Returns a string that is the <see cref="content"/>.ToString + <see cref="Count"/>.
        /// </summary>
        public override string ToString()
        {
            return $"Content : \"{content}\", Count : {Count}";
        }
        public override int GetHashCode()
        {
            return HashCode.Combine(content, Selected, ctx, m_Children);
        }

        public override bool Equals(object obj)
        {
            return Equals(obj as SearchDropdownElement);
        }
        public bool Equals(SearchDropdownElement other)
        {
            return !(other is null) &&
                   EqualityComparer<SearchDropdownElementContent>.Default.Equals(content, other.content) &&
                   Selected == other.Selected &&
                   EqualityComparer<PropertyRectContext>.Default.Equals(ctx, other.ctx) &&
                   EqualityComparer<List<SearchDropdownElement>>.Default.Equals(m_Children, other.m_Children);
        }

        public static bool operator ==(SearchDropdownElement left, SearchDropdownElement right)
        {
            return EqualityComparer<SearchDropdownElement>.Default.Equals(left, right);
        }
        public static bool operator !=(SearchDropdownElement left, SearchDropdownElement right)
        {
            return !(left == right);
        }
    }
}
