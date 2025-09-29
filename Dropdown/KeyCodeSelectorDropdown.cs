using System;
using System.Linq;
using UnityEngine;

namespace BX.Editor
{
    /// <summary>
    /// Allows for a selection dropdown for <see cref="KeyCode"/>.
    /// <br>This is the most basic <see cref="SearchDropdown{T}"/> example.</br>
    /// </summary>
    public class KeyCodeSelectorDropdown<T> : SearchDropdown<T> where T : ISearchDropdownWindow, new()
    {
        public KeyCode selectedKeyCode;
        public bool sortKeysAlphabetically = false;
        public override bool AllowRichText => true;

        /// <summary>
        /// Item that contains extra data for selection.
        /// </summary>
        public class Item : SearchDropdownElement
        {
            /// <summary>
            /// The item's keycode value.
            /// </summary>
            public readonly KeyCode keyValue;

            public Item(KeyCode keyCodeValue) : base($"{keyCodeValue} <color=#a8d799> | {(long)keyCodeValue}</color>")
            {
                keyValue = keyCodeValue;
            }

            public Item(KeyCode keyCodeValue, string label) : base(label)
            {
                keyValue = keyCodeValue;
            }

            public Item(KeyCode keyCodeValue, SearchDropdownElementContent content) : base(content)
            {
                keyValue = keyCodeValue;
            }

            public Item(KeyCode keyCodeValue, string label, string tooltip) : base(label, tooltip)
            {
                keyValue = keyCodeValue;
            }

            public Item(KeyCode keyCodeValue, string label, int childrenCapacity) : base(label, childrenCapacity)
            {
                keyValue = keyCodeValue;
            }

            public Item(KeyCode keyCodeValue, SearchDropdownElementContent content, int childrenCapacity) : base(content, childrenCapacity)
            {
                keyValue = keyCodeValue;
            }

            public Item(KeyCode keyCodeValue, string label, string tooltip, int childrenCapacity) : base(label, tooltip, childrenCapacity)
            {
                keyValue = keyCodeValue;
            }
        }

        private static readonly KeyCode[] m_keyCodes = Enum.GetValues(typeof(KeyCode)).Cast<KeyCode>().ToArray();
        protected override SearchDropdownElement BuildRoot()
        {
            string[] keyEnumNames = Enum.GetNames(typeof(KeyCode));
            SearchDropdownElement rootElement = new SearchDropdownElement("Select KeyCode", m_keyCodes.Length);

            for (int i = 0; i < m_keyCodes.Length; i++)
            {
                KeyCode key = m_keyCodes[i];
                // Append the name like this as 'GetEnumName' or 'Enum.ToString' always only gets the first alias for 'ToString'
                rootElement.Add(new Item(key, $"{keyEnumNames[i]} <color=#a8d799> | {(long)key}</color>")
                {
                    Selected = key != KeyCode.None && key == selectedKeyCode
                });
            }

            if (sortKeysAlphabetically)
            {
                rootElement.Sort();
            }

            return rootElement;
        }

        public KeyCodeSelectorDropdown()
        {
            selectedKeyCode = KeyCode.None;
        }
        public KeyCodeSelectorDropdown(KeyCode selected)
        {
            selectedKeyCode = selected;
        }
        public KeyCodeSelectorDropdown(bool sortKeysStrings)
        {
            sortKeysAlphabetically = sortKeysStrings;
        }
        public KeyCodeSelectorDropdown(KeyCode selected, bool sortKeysStrings)
        {
            selectedKeyCode = selected;
            sortKeysAlphabetically = sortKeysStrings;
        }
    }
}
