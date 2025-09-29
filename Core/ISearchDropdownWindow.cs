using System;

namespace BX.Editor
{
    /// <summary>
    /// Generic interface to implement for a custom dropdown window.
    /// </summary>
    public interface ISearchDropdownWindow
    {
        /// <summary>
        /// Initializes the window after creation. This is called by the base SearchDropdown.
        /// </summary>
        public void Setup(
            SearchDropdownData parentManager,
            // Parent Rect parameters
            float rectX, float rectY, float rectW, float rectH,
            // Control parameters
            float minSizeX, float minSizeY, float maxHeight
        );

        public event Action OnClose;
        /// <summary>
        /// Whether if the window closes with the intent of user selecting an element in the window.
        /// </summary>
        public bool ClosingWithSelection { get; }
        /// <summary>
        /// Change the query string. This may effect the contents of the window.
        /// </summary>
        public string SearchString { get; set; }

        public SearchDropdownElement GetSelected();
        public void Close();
    }
}
