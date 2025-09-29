using System;
using UnityEngine;

namespace BX.Editor
{
    /// <summary>
    /// A list dropdown displayer. Similar to the <see cref="UnityEditor.IMGUI.Controls.AdvancedDropdown"/>, it also offers async searching and slightly better optimization.
    /// </summary>
    /// <remarks>
    /// The usage of this class can be found in the source.
    /// </remarks>
    /// <example>
    /// <![CDATA[
    /// // ExampleDropdown.cs
    /// // Assembly-CSharp-Editor
    /// // Example dropdown.
    /// using UnityEngine;
    /// using UnityEditor;
    /// using BX.Editor;
    /// 
    /// public class ExampleDropdown<T> : SearchDropdown<T> where T : ISearchDropdownWindow, new()
    /// {
    ///     protected override SearchDropdownElement BuildRoot()
    ///     {
    ///         // Create a root element to return
    ///         // Other elements are to be attached to this root
    ///         SearchDropdownElement root = new SearchDropdownElement("Root Element")
    ///         {
    ///             // Any element can be started as a c# collection
    ///             new SearchDropdownElement("Child 1"),
    ///             new SearchDropdownElement("Child 2"),
    ///             new SearchDropdownElement("Child 3")
    ///             {
    ///                 // Every child can have it's own values and so on...
    ///                 new SearchDropdownElement("Child Of Child 1"),
    ///                 new SearchDropdownElement("Child Of Child 2")
    ///             }
    ///         };
    ///         // The children can be also systematically be added using SearchDropdownElement.Add()
    ///         // Basically a 'SearchDropdownElement' is an ICollection of 'SearchDropdownElement'
    ///         // Which technically is a tree data type. (or not, you are reading the sample written by the guy who failed DSA after all)
    ///         
    ///         // Return the root after creating it, this is required as a part of the abstract class 'SearchDropdown'.
    ///         return root;
    ///     }
    /// }
    /// // ... SampleClass.cs
    /// // Assembly-CSharp
    /// using UnityEngine;
    /// 
    /// public class SampleClass : MonoBehaviour
    /// {
    ///     public string dropdownSettingString;
    /// }
    /// // ... SampleClassEditor.cs
    /// // Assembly-CSharp-Editor
    /// using UnityEngine;
    /// using UnityEditor;
    /// using BX.Editor;
    /// 
    /// [CustomEditor(typeof(SampleClass))]
    /// public class SampleClassEditor : Editor
    /// {
    ///     // Unity gives a dummy rect on the Event.current.type == EventType.Layout
    ///     private Rect lastRepaintDropdownParentRect;
    /// 
    ///     public override void OnInspectorGUI()
    ///     {
    ///         var target = base.target as SampleClass;    
    /// 
    ///         // Draw the private 'm_Script' field (optional)
    ///         using (EditorGUI.DisabledScope scope = new EditorGUI.DisabledScope(true))
    ///         {
    ///             EditorGUILayout.PropertyField(serializedObject.FindProperty("m_Script"));
    ///         }
    ///         
    ///         // Draw the dropdown button
    ///         EditorGUILayout.BeginHorizontal();
    ///         EditorGUILayout.LabelField("Dropdown Setting String", GUILayout.Width(EditorGUIUtility.labelWidth));
    ///         if (GUILayout.Button($"Value : {target.dropdownSettingString}", EditorStyles.popup))
    ///         {
    ///             ExampleDropdown<UGUISearchDropdownWindow> dropdown = new();
    ///             dropdown.Show(lastRepaintDropdownParentRect);
    ///             dropdown.OnElementSelectedEvent += (SearchDropdownElement element) =>
    ///             {
    ///                 // Will not take a 'SerializedProperty' inside a delegate
    ///                 // Because SerializedObject and SerializedProperty disposes automatically after the OnGUI call
    ///                 // But you can clone the entire SerializedObject and SerializedProperty just for this delegate, 
    ///                 // then apply changed values and dispose of it inside this delegate when done (manually managing it's lifetime)
    ///                 // -- 
    ///                 // For this example, a basic undo with direct access to the object is used
    ///                 Undo.RecordObject(target, "set value from dropdown");
    ///                 // You can create custom Element/Item classes that inherit from 'SearchDropdownElement' and type test it to get it's values.
    ///                 // With this, you can test/unbox the type like `if (element is ExampleDropdown<UGUISearchDropdownWindow>.Item item)` and get extra data.
    ///                 // For now we are just assigning the content text set to the element for simplicity.
    ///                 target.dropdownSettingString = element.content.text;
    ///             };
    ///         }
    ///         // Get the last rect for getting the proper value
    ///         // This is only needed on automatically layouted GUI's, with the GUI's
    ///         // that you know the rect to you can use that rect instead.
    ///         // (as the rect becomes kDummyRect during certain events that trigger the dropdown)
    ///         if (Event.current.type == EventType.Repaint)
    ///         {
    ///             lastRepaintDropdownParentRect = GUILayoutUtility.GetLastRect();
    ///         }
    ///         EditorGUILayout.EndHorizontal();
    /// 
    ///     }
    /// }
    /// // Basically, you can embed this dropdown into anything, including 
    /// // PropertyDrawers of attributes or classes, or on GUILayout based UI (and likely on UIElements too)
    /// ]]>
    /// </example>
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
