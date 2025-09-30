using System;
using UnityEngine;

namespace BX.Editor
{
    /// <summary>
    /// A list dropdown displayer. Similar to the <see cref="UnityEditor.IMGUI.Controls.AdvancedDropdown"/>, it also offers async searching and slightly better optimization.
    /// </summary>
    /// <remarks>
    /// The usage of this class can be found in the README.md
    /// </remarks>
    public abstract class SearchDropdown<T> : SearchDropdownData where T : ISearchDropdownWindow, new()
    {
        /// <summary>
        /// The given spawned window instance.
        /// <br>Calling close on this will close the window.</br>
        /// </summary>
        private ISearchDropdownWindow m_Window;

        /// <summary>
        /// Show the dropdown at the <paramref name="rect"/> position.
        /// <br>This also creates a new root.</br>
        /// </summary>
        /// <param name="rect">Position of the button that opened the dropdown.</param>
        public override void Show(Rect rect)
        {
            if (m_Window != null)
            {
                m_Window.Close();
                m_Window = null;
            }

            // The 'AdvancedDropdown' handles all of it's data using a 'DataSource'
            // I will do it the good ole way of suffering
            m_RootElement = BuildRoot();
            if (m_RootElement == null)
            {
                throw new NullReferenceException("[SearchDropdown::Show(BuildRoot phase)] Result of BuildRoot is null. The result should be a SearchDropdownElement that isn't null.");
            }

            // Create window generically (hacky because unity doesn't follow C# convention)
            if (typeof(ScriptableObject).IsAssignableFrom(typeof(T)))
            {
                // Create window without 'CreateWindow' as that injects titlebar GUI
                // Which i don't want. It is only useful when IMGUI debugger is needed and this window being
                m_Window = (T)(object)ScriptableObject.CreateInstance(typeof(T));
            }
            // Assuming parameterless .ctor, which we can add to the constraints but we must not use if it's a Unity object.
            else
            {
                m_Window = new T();
            }

            m_Window.Setup(this, rect.x, rect.y, rect.width, rect.height, MinimumSize.x, MinimumSize.y, MaximumHeight);
            m_Window.OnClose += () =>
            {
                if (!m_Window.ClosingWithSelection)
                {
                    InvokeOnDiscardEvent();
                    return;
                }

                SearchDropdownElement selected = m_Window.GetSelected();

                if (selected != null)
                {
                    InvokeElementSelectedEvent(selected);
                }
                else
                {
                    throw new InvalidOperationException("[SearchDropdown::m_Window+OnClosed] Selected is null but the window was closed with selection intent. This must not happen and probably caused by an erroreneous situation.");
                }

                m_RootElement = null;
            };

            PostBuildRoot();
        }

        /// <summary>
        /// Sets the searching filter of the window.
        /// On non-searchable dropdowns, this will make the window display an unclearable search results display.
        /// <br>This is <b>not safe</b> to be called from <see cref="BuildRoot"/>, use <see cref="PostBuildRoot"/> for calling this.</br>
        /// </summary>
        protected void SetFilter(string searchString)
        {
            m_Window.SearchString = searchString;
        }

        /// <summary>
        /// Build the root of this searching dropdown.
        /// </summary>
        /// <returns>
        /// The root element containing the elements. 
        /// If the root element has no children a dropdown with <see cref="NoElementPlaceholderText"/> will appear.
        /// </returns>
        protected abstract SearchDropdownElement BuildRoot();
        /// <summary>
        /// Called after everything of the dropdown was initialized on <see cref="Show(Rect)"/>.
        /// </summary>
        protected virtual void PostBuildRoot()
        { }
    }
}
