using System;
using UnityEngine;

namespace BX.Editor
{
    /// <summary>
    /// Data base for <see cref="SearchDropdown{T}"/>.
    /// </summary>
    public abstract class SearchDropdownData
    {
        private Vector2 m_MinimumSize = new(0f, 0f);
        /// <summary>
        /// Minimum size possible for this dropdown.
        /// </summary>
        public virtual Vector2 MinimumSize
        {
            get
            {
                return m_MinimumSize;
            }
            set
            {
                m_MinimumSize = value;
            }
        }
        private float m_MaximumHeight = 300f;
        /// <summary>
        /// The maximum height possible for this dropdown.
        /// <br>If this value clashes with the <see cref="MinimumSize"/> the <see cref="MinimumSize"/>.y is preferred instead.</br>
        /// <br/>
        /// <br>This value both can be overriden or be set as an usual variable on <see cref="BuildRoot"/>.</br>
        /// </summary>
        public virtual float MaximumHeight
        {
            get
            {
                return Math.Max(m_MaximumHeight, MinimumSize.y);
            }
            set
            {
                m_MaximumHeight = value;
            }
        }
        /// <summary>
        /// Whether to allow rich text reprensentation on the OptimizedSearchDropdown elements. 
        /// <br>Note : Any <see cref="SearchDropdownElement"/> can override or ignore this. This only applies
        /// to the global label (<see cref="UGUISearchDropdownWindow.StyleList"/>) styling.</br>
        /// <br>By default, this value is <see langword="false"/>.</br>
        /// </summary>
        public virtual bool AllowRichText => false;
        /// <summary>
        /// Whether to allow selection event to be fired for elements with children.
        /// <br>This will show an extra button and will allow selection of elements with children.</br>
        /// <br>By default, this value is <see langword="false"/>.</br>
        /// </summary>
        public virtual bool AllowSelectionOfElementsWithChild => false;
        /// <summary>
        /// Whether if this 'OptimizedSearchDropdown' will have a search bar.
        /// <br>This will affect the height of the dropdown depending on whether to have a search bar.</br>
        /// <br>By default, this value is <see langword="true"/>.</br>
        /// </summary>
        public virtual bool IsSearchable => true;
        /// <summary>
        /// Amount of maximum children count of search result.
        /// <br>After this many elements the searching will stop.</br>
        /// <br/>
        /// <br>Setting this zero or lower than zero will set this limit to infinite which is not really recommended.</br>
        /// </summary>
        public virtual int SearchElementsResultLimit => 10000;
        /// <summary>
        /// Whether to ignore cAsInG of given search query.
        /// </summary>
        public virtual bool SearchIgnoreCase => false;
        /// <summary>
        /// Whether to use fuzzy matching for searching. If this is true, the <see cref="SearchComparison"/> is ignored.
        /// <br>Setting this to false will make you use the <see cref="SearchComparison"/> 
        /// which searches for exact match and is faster for larger element count.</br>
        /// </summary>
        public virtual bool SearchFuzzy => true;
        /// <summary>
        /// Whether to strip rich HTML or BBCode text tags like <c>&lt;color=red&gt;text html&lt;color&gt; or [c=red]text bb[/c]</c>
        /// </summary>
        public virtual bool SearchStripRichText => true;
        /// <summary>
        /// Whether to sort search results by the Query match index.
        /// </summary>
        public virtual bool SearchSortResultsByQuery => true;
        /// <summary>
        /// Whether to display the current elements count inside the header text.
        /// <br>The header text will be shown as "RootElement.content.name | RootElement.Count".
        /// The "| RootElement.Count" is added/controlled by this value.</br>
        /// <br>By default, this value is <see langword="true"/>.</br>
        /// </summary>
        public virtual bool DisplayCurrentElementsCount => true;
        /// <summary>
        /// Whether to start from the first <see cref="SearchDropdownElement"/> that is selected.
        /// (i.e the scroll position will view the first <see cref="SearchDropdownElement"/> 
        /// that has it's <see cref="SearchDropdownElement.Selected"/> as <see langword="true"/>)
        /// <br>You should set this <see langword="true"/> only if you are planning to set a single <see cref="SearchDropdownElement"/> 
        /// as <see cref="SearchDropdownElement.Selected"/> as other elements may not be viewed (only the first occurance is taken to account)</br>
        /// <br>If none of the elements are selected, nothing will be done.</br>
        /// </summary>
        public virtual bool StartFromFirstSelected => false;

        /// <summary>
        /// Whether to close the 'SearchDropdown' in an event of an undo or a redo.
        /// <br>Setting this <see langword="false"/> does not break anything, it is just added for nicer experience.</br>
        /// <br>By default, this value is <see langword="true"/>.</br>
        /// </summary>
        public virtual bool CloseOnUndoRedoAction { get; set; } = true;
        /// <summary>
        /// Placeholder string displayed for dropdowns without any elements.
        /// </summary>
        public virtual string NoElementPlaceholderText { get; set; } = "No elements added to dropdown.";
        /// <summary>
        /// String used to show that there's no results.
        /// <br>Can have a format argument as {0}, where it will be replaced with the search query.</br>
        /// </summary>
        public virtual string NoSearchResultsText { get; set; } = "No results found on search query \"{0}\"\nTry searching for other elements or check if the search string matches.";

        // TODO : Search children using some haystack algorithm and fuzzy matching
        // Sorting can be done by the caller who builds the root.

        public SearchDropdownData()
        { }

        /// <summary>
        /// The root element. This can be written into.
        /// </summary>
        protected SearchDropdownElement m_RootElement;
        /// <summary>
        /// The root element.
        /// <br>This element is null during <see cref="SearchDropdown{T}.BuildRoot"/>.</br>
        /// </summary>
        public SearchDropdownElement RootElement => m_RootElement;
        /// <summary>
        /// Show the dropdown at the <paramref name="rect"/> position. This handles the window instantiation.
        /// </summary>
        /// <param name="rect">Position of the button that opened the dropdown.</param>
        public abstract void Show(Rect rect);

        /// <summary>
        /// Called when an element is selected.
        /// </summary>
        public event Action<SearchDropdownElement> OnElementSelected;
        /// <summary>
        /// Called when the dropdown is discarded.
        /// (No element selection intent was specified and the dropdown closed)
        /// </summary>
        public event Action OnDiscardEvent;

        /// <summary>
        /// Invokes the <see cref="OnElementSelected"/>
        /// </summary>
        protected void InvokeElementSelectedEvent(SearchDropdownElement element)
        {
            OnElementSelected?.Invoke(element);
        }
        /// <summary>
        /// Invokes the <see cref="OnDiscardEvent"/>
        /// </summary>
        protected void InvokeOnDiscardEvent()
        {
            OnDiscardEvent?.Invoke();
        }
    }
}
