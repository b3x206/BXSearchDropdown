using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using UnityEngine;

namespace BX.Editor
{
    /// <summary>
    /// A type selector that categorizes and has the ability to select types.
    /// </summary>
    public class TypeSelectorDropdown<T> : SearchDropdown<T> where T : ISearchDropdownWindow, new()
    {
        /// <summary>
        /// Categorization of assemblies.
        /// </summary>
        public enum AssemblyCategory
        {
            None = 0,
            CoreAssembly = 1 << 0,
            UnityAssembly = 1 << 1,
            CSharpAssembly = 1 << 2,
            CSharpEditorAssembly = 1 << 3,
            Dynamic = 1 << 4,
            // TODO : Fetch names dynamically here, if applicable for the category of assemblies.
            ScriptAssembly = 1 << 5,

            All = unchecked(~0),
            Uncategorized = unchecked(1 << 31),
        }
        public static AssemblyCategory GetAssemblyCategory(Assembly asm)
        {
            // Check if the assembly is in MS_GAC or GAC
            if (Assembly.GetAssembly(typeof(object)) == asm || asm.GlobalAssemblyCache)
            {
                // Check name patterning (note : GAC should enforce this well for the time being?)
                string assemblyName = asm.FullName;
                if (assemblyName.StartsWith("System.") || assemblyName.StartsWith("Microsoft."))
                {
                    return AssemblyCategory.CoreAssembly;
                }
            }
            // These are fragile
            if (asm.GetName().Name.Contains("UnityEngine") ||
                asm.GetName().Name == "UnityEngine.SharedInternalsModule" ||
                asm.GetReferencedAssemblies().Any((asmName) => asmName.Name == "UnityEngine.SharedInternalsModule"))
            {
                return AssemblyCategory.UnityAssembly;
            }
            if (asm.FullName.StartsWith("Assembly-CSharp"))
            {
                return asm.FullName.Contains("Editor") ? AssemblyCategory.CSharpEditorAssembly : AssemblyCategory.CSharpAssembly;
            }
            // Getting assembly location while the assembly is dynamic is not supported, so do this first.
            if (asm.IsDynamic)
            {
                return AssemblyCategory.Dynamic;
            }
            // Contained in 'ProjectRoot/ScriptAssemblies' file of unity
            else if (asm.Location.Contains(Path.Combine(Directory.GetCurrentDirectory(), "Library")))
            {
                return AssemblyCategory.ScriptAssembly;
            }

            return AssemblyCategory.Uncategorized;
        }

        /// <summary>
        /// An 'SearchDropdownElement' that contains extra data.
        /// </summary>
        public class Item : SearchDropdownElement
        {
            public Type type;

            public Item(string name, Type type) : base(name)
            {
                this.type = type;
            }
            public Item(string name, string tooltip, Type type) : base(name, tooltip)
            {
                this.type = type;
            }
        }

        public override bool AllowRichText => true;

        private readonly Type m_selectedType;
        private readonly bool m_addNoneOption = true;
        private readonly Predicate<Type> m_filterPredicate;
        /// <summary>
        /// Adds <see cref="Type.AssemblyQualifiedName"/> as tooltip.
        /// <br>Could make the Dropdown bootstrap time higher as a result, if there are many elements.</br>
        /// </summary>
        public bool addAssemblyInfoTooltip = true;
        public readonly AssemblyCategory filterCategory = AssemblyCategory.All & (~AssemblyCategory.Dynamic);

        public override Vector2 MinimumSize { get => new(base.MinimumSize.x, 130f); set => base.MinimumSize = value; }

        // This could be more optimized. Use your own type cache and modify this dropdown code.
        private static readonly Dictionary<AssemblyCategory, List<Type>> m_assemblies = new(512);
        static TypeSelectorDropdown()
        {
            // While a linq query is possible, it:
            // * doesn't append the lists to the existing key, only creates new key. causing exception
            // * slightly less performant and encumbered to IEnumerable
            // The query looks like
            // AppDomain.CurrentDomain.GetAssemblies()
            //     .GroupBy(GetAssemblyCategory)
            //     .SelectMany(kv => kv.Select(asm => new KeyValuePair<AssemblyCategory, Type[]>(kv.Key, asm.GetTypes())))

            // Initialize m_assemblies.
            Assembly[] assemblies = AppDomain.CurrentDomain.GetAssemblies();
            for (int i = 0; i < assemblies.Length; i++)
            {
                Assembly assembly = assemblies[i];
                Type[] types = assembly.GetTypes();
                AssemblyCategory category = GetAssemblyCategory(assembly);

                for (int j = 0; j < types.Length; j++)
                {
                    Type type = types[j];
                    if (m_assemblies.TryGetValue(category, out List<Type> list))
                    {
                        list.Add(type);
                    }
                    else
                    {
                        m_assemblies.Add(category, new List<Type>(2048)
                        {
                            type
                        });
                    }
                }
            }
        }

        protected override SearchDropdownElement BuildRoot()
        {
            SearchDropdownElement rootItem = new SearchDropdownElement("Type Categories");
            if (m_addNoneOption)
            {
                rootItem.Add(new Item("None", null));
            }

            // Assembly Categorization:
            foreach (KeyValuePair<AssemblyCategory, List<Type>> domainCategoryType in m_assemblies)
            {
                if ((domainCategoryType.Key & filterCategory) != domainCategoryType.Key)
                {
                    continue;
                }

                SearchDropdownElement categoryItem = new SearchDropdownElement($"<color=#a2d4a3>{domainCategoryType.Key}</color>", domainCategoryType.Value.Count);
                rootItem.Add(categoryItem);

                foreach (Type t in domainCategoryType.Value)
                {
                    if (!m_filterPredicate(t))
                    {
                        continue;
                    }

                    string typeIdentifier = string.Empty;
                    if (t.IsClass)
                    {
                        typeIdentifier = "<color=#4ec9b0>C</color>";
                    }
                    else if (t.IsEnum)
                    {
                        // Enum is also value type, so do it before?
                        typeIdentifier = "<color=#b8d797>E</color>";
                    }
                    else if (t.IsValueType)
                    {
                        typeIdentifier = "<color=#86b86a>S</color>";
                    }
                    else if (t.IsInterface)
                    {
                        typeIdentifier = "<color=#b8d7a3>I</color>";
                    }

                    Item categoryChildItem = new Item(
                        name: $"{typeIdentifier} | <color=white>{t.FullName}</color>",
                        tooltip: addAssemblyInfoTooltip ? t.AssemblyQualifiedName : string.Empty,
                        type: t
                    )
                    {
                        Selected = t == m_selectedType
                    };

                    categoryItem.Add(categoryChildItem);
                }

                // Make element non-selectable if the predicate ignored all
                if (!categoryItem.HasChildren)
                {
                    rootItem.Remove(categoryItem);
                }
            }

            return rootItem;
        }

        private static bool DefaultFilterPredicate(Type t)
        {
            return t.IsPublic;
        }

        public TypeSelectorDropdown()
        {
            m_filterPredicate = DefaultFilterPredicate;
        }
        public TypeSelectorDropdown(bool addNoneOption)
        {
            m_filterPredicate = DefaultFilterPredicate;
            m_addNoneOption = addNoneOption;
        }
        public TypeSelectorDropdown(Type selected)
        {
            m_selectedType = selected;
            m_filterPredicate = DefaultFilterPredicate;
        }
        public TypeSelectorDropdown(Type selected, bool addNoneOption)
        {
            m_selectedType = selected;
            m_addNoneOption = addNoneOption;
            m_filterPredicate = DefaultFilterPredicate;
        }
        public TypeSelectorDropdown(Predicate<Type> filterPredicate)
        {
            m_filterPredicate = filterPredicate ?? DefaultFilterPredicate;
        }
        public TypeSelectorDropdown(Type selected, Predicate<Type> filterPredicate)
        {
            m_selectedType = selected;
            m_filterPredicate = filterPredicate ?? DefaultFilterPredicate;
        }
        public TypeSelectorDropdown(Type selected, Predicate<Type> filterPredicate, bool addNoneOption)
        {
            m_selectedType = selected;
            m_addNoneOption = addNoneOption;
            m_filterPredicate = filterPredicate ?? DefaultFilterPredicate;
        }
    }
}
