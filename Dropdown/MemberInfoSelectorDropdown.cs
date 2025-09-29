using System;
using System.Collections.Generic;
using System.Reflection;
using System.Text;

namespace BX.Editor
{
    public static class MemberInfoSelectorUtility
    {
        /// <summary>
        /// A backtick used in the generic name definitions.
        /// </summary>
        private const char GenericArgumentsDefinitionChar = '`';
        /// <summary>
        /// A list of global type aliases/predefined types used by C# for System types.
        /// <br>For example, <c>System.Int32</c> being aliased to <see cref="int"/>.</br>
        /// <br/>
        /// <br>These types are also called built-in types as well.</br>
        /// </summary>
        private static readonly Dictionary<Type, string> GlobalTypeAliasesMap = new Dictionary<Type, string>
        {
            // Value
            { typeof(bool), "bool" },
            { typeof(char), "char" },
            { typeof(sbyte), "sbyte" },
            { typeof(byte), "byte" },
            { typeof(short), "short" },
            { typeof(ushort), "ushort" },
            { typeof(int), "int" },
            { typeof(uint), "uint" },
            { typeof(long), "long" },
            { typeof(ulong), "ulong" },
            { typeof(float), "float" },
            { typeof(double), "double" },
            { typeof(decimal), "decimal" },
            // These types are pointer types which can be explicitly written
            // And these shorthand names are more confusing anyways
            // { typeof(nint), "nint" },   // Which is System.IntPtr
            // { typeof(nuint), "nuint" }, // Which is System.UIntPtr
            // Reference
            { typeof(object), "object" },
            { typeof(string), "string" },
        };
        /// <summary>
        /// Returns the pretty name/type definition string of the c# type.
        /// <br>(like <c>Foo&lt;Parameter&gt;</c> instead of <c>Foo`1[[typeof(Parameter).QualifiedAssemblyName]]</c>).</br>
        /// </summary>
        /// <param name="type">Type to return it's name.</param>
        /// <param name="includeNamespace">Whether to include the type's namespace in the start. This applies to all types.</param>
        /// <param name="usePredefinedTypeAliases">Whether to use the shorthand aliases/type definitions for default c# types. Applies to all types.</param>
        public static string GetPrettyTypeName(Type type, bool includeNamespace = false, bool usePredefinedTypeAliases = true)
        {
            if (type == null)
            {
                throw new ArgumentNullException(nameof(type), "[MemberInfoSelectorDropdown::GetPrettyTypeName] Given argument was null.");
            }

            // Type exists on global aliases/predefined types
            if (usePredefinedTypeAliases && GlobalTypeAliasesMap.TryGetValue(type, out string alias))
            {
                return alias;
            }

            string typeName = includeNamespace ? type.FullName : type.Name;
            // type.Name or type.FullName can be null if the given type is weird
            int indexOfGenericArgsChar = typeName?.IndexOf(GenericArgumentsDefinitionChar) ?? -1;
            if (indexOfGenericArgsChar < 0)
            {
                return typeName;
            }

            Type[] typeGenericArgs = type.GetGenericArguments();
            StringBuilder typeNameSb = new StringBuilder(typeName.Substring(0, indexOfGenericArgsChar));
            typeNameSb.Append('<');
            for (int i = 0; i < typeGenericArgs.Length; i++)
            {
                Type genericArg = typeGenericArgs[i];
                // Open type adding, add semicolons
                if (genericArg.IsGenericParameter)
                {
                    if (i != typeGenericArgs.Length - 1)
                    {
                        // (all GenericArguments are likely open if one of them is open too, so no space needed)
                        typeNameSb.Append(',');
                    }
                    continue;
                }

                // Add type strings recursively
                typeNameSb.Append(GetPrettyTypeName(genericArg, includeNamespace, usePredefinedTypeAliases));
                if (i != typeGenericArgs.Length - 1)
                {
                    // Leave a space for closed types
                    typeNameSb.Append(", ");
                }
            }
            typeNameSb.Append('>');

            return typeNameSb.ToString();
        }
    }

    /// <summary>
    /// Allows for selection of a <see cref="MemberInfo"/> inside given <see cref="targetType"/>.
    /// <br>By default, only allows <see cref="MemberTypes.Property"/> and <see cref="MemberTypes.Field"/>, but can allow all members.</br>
    /// </summary>
    public class MemberInfoSelectorDropdown<T> : SearchDropdown<T> where T : ISearchDropdownWindow, new()
    {
        /// <summary>
        /// A <see cref="MemberInfo"/> selection predicate delegate typedef.
        /// </summary>
        /// <param name="member">The member to select.</param>
        /// <param name="accessModifierName">The access modifier name. Can be left blank/unassigned.</param>
        /// <param name="memberTypeName">The type name. Can be left blank/unassigned.</param>
        /// <returns>Whether if this selection delegate passes the predicate test.</returns>
        public delegate bool MemberSelectionDelegate(MemberInfo member);
        /// <summary>
        /// A <see cref="MemberInfo"/> to string builder.
        /// </summary>
        /// <param name="member">Member to build name for.</param>
        /// <param name="sb">Supplied string builder to build the result into.</param>
        /// <param name="richText">Whether to use rich text on the name.</param>
        public delegate void MemberStringifyDelegate(MemberInfo member, in StringBuilder sb, bool richText);
        /// <summary>
        /// Iterate the given <see cref="MemberInfo"/>, if the <see cref="MemberInfo"/> contains more members to iterate.
        /// <br>The </br>
        /// </summary>
        /// <param name="iterableMemberInfo"></param>
        /// <returns></returns>
        public delegate IEnumerable<MemberInfo> IterateMemberDelegate(MemberInfo iterableMemberInfo);

        /// <summary>
        /// Item that contains extra data for selected.
        /// </summary>
        public class Item : SearchDropdownElement
        {
            /// <summary>
            /// MemberInfo that owns the <see cref="memberInfo"/>
            /// </summary>
            public readonly MemberInfo parentMemberInfo;
            /// <summary>
            /// The member info that this item contains.
            /// </summary>
            public readonly MemberInfo memberInfo;

            public Item(string label, MemberInfo rootInfo, MemberInfo info) : base(label)
            {
                memberInfo = info;
                parentMemberInfo = rootInfo;
            }

            public Item(string label, int childrenCapacity, MemberInfo rootInfo, MemberInfo info) : base(label, childrenCapacity)
            {
                memberInfo = info;
                parentMemberInfo = rootInfo;
            }
        }

        public override bool AllowRichText => true;
        public override bool AllowSelectionOfElementsWithChild => true;

        /// <summary>
        /// The selected target type that this dropdown was generated for.
        /// </summary>
        public readonly Type targetType;
        /// <summary>
        /// A <see cref="MemberInfo"/> selection predicate.
        /// </summary>
        public MemberSelectionDelegate memberSelectionPredicate;
        /// <summary>
        /// This converts a given <see cref="MemberInfo"/> into a representation string, for the selection.
        /// </summary>
        public MemberStringifyDelegate memberStringifyPredicate;
        /// <summary>
        /// A child member iteration predicate.
        /// <br>This should only iterate one layer of the given <see cref="MemberInfo"/>.</br>
        /// <br/>
        /// <br>This value can be null.</br>
        /// </summary>
        public IterateMemberDelegate memberChildIterationPredicate;

        /// <summary>
        /// Predicate used to select the member.
        /// <br>By default, allows <see cref="MemberTypes.Field"/> and <see cref="MemberTypes.Property"/>.</br>
        /// </summary>
        private static bool DefaultSelectionPredicate(MemberInfo info)
        {
            if (info is PropertyInfo propertyInfo)
            {
                return propertyInfo.CanRead && propertyInfo.CanWrite;
            }
            if (info is FieldInfo)
            {
                return true;
            }

            return false;
        }
        private static void DefaultStringifyPredicate(MemberInfo info, in StringBuilder sb, bool richText = true)
        {
            if (info is PropertyInfo propertyInfo)
            {
                MethodInfo[] propertyAccessorInfos = propertyInfo.GetAccessors();
                if (richText)
                {
                    sb.Append("<color=#569cd6>");
                }

                bool lastAccessorPublic = false;
                for (int i = 0; i < propertyAccessorInfos.Length; i++)
                {
                    MethodInfo methodInfo = propertyAccessorInfos[i];

                    // Getter
                    if (methodInfo.ReturnType != typeof(void))
                    {
                        if (methodInfo.IsPublic && !lastAccessorPublic)
                        {
                            sb.Append("public ");
                        }
                        else if (lastAccessorPublic)
                        {
                            sb.Append("private ");
                        }

                        sb.Append("get");
                    }
                    // Setter
                    else
                    {
                        if (methodInfo.IsPublic && !lastAccessorPublic)
                        {
                            sb.Append("public ");
                        }
                        else if (lastAccessorPublic)
                        {
                            sb.Append("private ");
                        }

                        sb.Append("set");
                    }

                    // add space + comma between to make the name neat
                    if (i != propertyAccessorInfos.Length - 1)
                    {
                        sb.Append(", ");
                    }

                    lastAccessorPublic = methodInfo.IsPublic;
                }

                if (richText)
                {
                    sb.Append("</color> ");
                }

                string typeName = MemberInfoSelectorUtility.GetPrettyTypeName(propertyInfo.PropertyType);
                sb.Append(richText ? $"<color=#2e9fa4>{typeName}</color>" : typeName);
            }
            else if (info is FieldInfo fieldInfo)
            {
                string modifierName = fieldInfo.IsPublic ? "public" : "private/protected";
                sb.Append(richText ? $"<color=#569cd6>{(modifierName)}</color> " : modifierName);
                string typeName = MemberInfoSelectorUtility.GetPrettyTypeName(fieldInfo.FieldType);
                sb.Append(richText ? $"<color=#2e9fa4>{typeName}</color>" : typeName);
            }
            else if (info is MethodInfo methodInfo)
            {
                string modifierName = methodInfo.IsPublic ? "public" : "private/protected";
                sb.Append(richText ? $"<color=#569cd6>{(modifierName)}</color> " : modifierName);
                string typeName = MemberInfoSelectorUtility.GetPrettyTypeName(methodInfo.ReturnType);
                sb.Append(richText ? $"<color=#2e9fa4>{typeName}</color>" : typeName);
            }
            else if (info is ConstructorInfo ctorInfo)
            {
                string modifierName = ctorInfo.IsPublic ? "public" : "private/protected";
                sb.Append(richText ? $"<color=#569cd6>{(modifierName)}</color> " : modifierName);
                string typeName = MemberInfoSelectorUtility.GetPrettyTypeName(ctorInfo.DeclaringType);
                sb.Append(richText ? $"<color=#2e9fa4>{typeName}()</color>" : $"{typeName}()");
            }
            else if (info is TypeInfo typeInfo)
            {
                string modifierName = typeInfo.IsPublic ? "public" : "private/protected";
                sb.Append(richText ? $"<color=#569cd6>{(modifierName)}</color> " : modifierName);
                string typeName = MemberInfoSelectorUtility.GetPrettyTypeName(typeInfo.AsType());
                sb.Append(richText ? $"<color=#2e9fa4>{typeName}</color>" : typeName);
            }
        }

        /// <summary>
        /// Predicate used to iterate a <see cref="MemberInfo"/>.
        /// <br>By default only iterates members with a valid "declaring" type (such as a FieldType or a PropertyType).</br>
        /// </summary>
        private static IEnumerable<MemberInfo> DefaultIterationPredicate(MemberInfo info)
        {
            // TODO : To make this selector more coherent (in terms of code)
            // * Get the "IEnumerable<MemberInfo>" through the memberSelectionPredicate
            // Or just keep this as is :p. though the StringifyPredicate is somewhat overkill
            if (info is FieldInfo fieldInfo)
            {
                foreach (MemberInfo childInfo in fieldInfo.FieldType.GetFields())
                {
                    yield return childInfo;
                }
            }
            if (info is PropertyInfo propertyInfo)
            {
                foreach (MemberInfo childInfo in propertyInfo.PropertyType.GetFields())
                {
                    yield return childInfo;
                }
            }
        }

        /// <summary>
        /// Recursively builds a <see cref="SearchDropdownElement"/> using a <see cref="IterateMemberDelegate"/>.
        /// </summary>
        /// <param name="parentInfo">The member info to iterate into it's children.</param>
        private Item BuildMemberInfoRecursive(MemberInfo rootInfo, MemberInfo createItemInfo)
        {
            // Accept + Create item
            if (!memberSelectionPredicate(createItemInfo))
            {
                return null;
            }

            StringBuilder itemNameSb = new StringBuilder(64);
            memberStringifyPredicate(
                createItemInfo,
                itemNameSb, 
                // TODO : Retrieve actual dropdown style state while not spawning window
                richText: true
            );
            itemNameSb.Append(' ').Append(createItemInfo.Name);
            Item infoElement = new Item(itemNameSb.ToString(), rootInfo, createItemInfo);

            // If the iteration predicate is empty, only use one layer depth elements.
            if (memberChildIterationPredicate == null)
            {
                return infoElement;
            }

            // Iterate the 'rootInfo'.
            foreach (MemberInfo member in memberChildIterationPredicate(createItemInfo))
            {
                Item createdMemberElement = BuildMemberInfoRecursive(rootInfo, member);
                // Don't add null members
                if (createdMemberElement == null)
                {
                    continue;
                }

                infoElement.Add(createdMemberElement);
            }

            return infoElement;
        }

        protected override SearchDropdownElement BuildRoot()
        {
            SearchDropdownElement rootItem = new SearchDropdownElement("Select Member Info");

            // Only draw public fields + properties with get+set
            MemberInfo[] rootTypeMembersArray = targetType.GetMembers();
            rootItem.ChildCapacity = rootTypeMembersArray.Length;

            foreach (MemberInfo member in rootTypeMembersArray)
            {
                Item memberItem = BuildMemberInfoRecursive(member, member);
                // MemberItem most likely didn't pass the tests.
                if (memberItem == null)
                {
                    continue;
                }

                rootItem.Add(memberItem);
            }

            return rootItem;
        }

        /// <inheritdoc cref="MemberInfoSelectorDropdown(Type, MemberSelectionDelegate, IterateMemberDelegate)"/>
        public MemberInfoSelectorDropdown(Type target)
        {
            if (target == null)
            {
                throw new ArgumentNullException(nameof(target), "[MemberInfoSelectorDropdown::Create]");
            }

            targetType = target;
            memberSelectionPredicate = DefaultSelectionPredicate;
            memberStringifyPredicate = DefaultStringifyPredicate;
            memberChildIterationPredicate = DefaultIterationPredicate;
        }
        /// <inheritdoc cref="MemberInfoSelectorDropdown(Type, MemberSelectionDelegate, MemberStringifyDelegate, IterateMemberDelegate)"/>
        /// <param name="allowMultiLevelMemberSelection">Whether to allow multi depth member info selection.</param>
        public MemberInfoSelectorDropdown(Type target, bool allowMultiLevelMemberSelection) : this(target)
        {
            if (!allowMultiLevelMemberSelection)
            {
                memberChildIterationPredicate = null;
            }
        }
        /// <inheritdoc cref="MemberInfoSelectorDropdown(Type, MemberSelectionDelegate, MemberStringifyDelegate, IterateMemberDelegate)"/>
        /// <param name="allowMultiLevelMemberSelection">Whether to allow multi depth member info selection.</param>
        public MemberInfoSelectorDropdown(Type target, MemberSelectionDelegate memberPredicate, bool allowMultiLevelMemberSelection) : this(target)
        {
            if (memberPredicate != null)
            {
                memberSelectionPredicate = memberPredicate;
            }

            if (!allowMultiLevelMemberSelection)
            {
                memberChildIterationPredicate = null;
            }
        }
        /// <inheritdoc cref="MemberInfoSelectorDropdown(Type, MemberSelectionDelegate, MemberStringifyDelegate, IterateMemberDelegate)"/>
        public MemberInfoSelectorDropdown(Type target, MemberSelectionDelegate memberPredicate) : this(target)
        {
            if (memberPredicate != null)
            {
                memberSelectionPredicate = memberPredicate;
            }
        }
        /// <inheritdoc cref="MemberInfoSelectorDropdown(Type, MemberSelectionDelegate, MemberStringifyDelegate, IterateMemberDelegate)"/>
        public MemberInfoSelectorDropdown(Type target, MemberStringifyDelegate stringifyPredicate) : this(target)
        {
            if (stringifyPredicate != null)
            {
                memberStringifyPredicate = stringifyPredicate;
            }
        }
        /// <inheritdoc cref="MemberInfoSelectorDropdown(Type, MemberSelectionDelegate, MemberStringifyDelegate, IterateMemberDelegate)"/>
        public MemberInfoSelectorDropdown(Type target, IterateMemberDelegate childIteratePredicate) : this(target)
        {
            memberChildIterationPredicate = childIteratePredicate;
        }
        /// <summary>
        /// Creates a MemberInfoSelectorDropdown.
        /// </summary>
        /// <param name="target">Target type to get the member infos from. This musn't be null.</param>
        /// <param name="memberPredicate">Predicate used to select the <see cref="MemberInfo"/>.</param>
        /// <param name="stringifyPredicate">Convert a given <see cref="MemberInfo"/> into a string depending on it's type.</param>
        /// <param name="childIteratePredicate">
        /// Child MemberInfo iteration predicate. 
        /// This must only iterate 1 layer, anything more or using recursive iterations, will cause erroreneous behaviour.
        /// </param>
        public MemberInfoSelectorDropdown(
            Type target, MemberSelectionDelegate memberPredicate, 
            MemberStringifyDelegate stringifyPredicate, IterateMemberDelegate childIteratePredicate
        ) : this(target)
        {
            if (memberPredicate != null)
            {
                memberSelectionPredicate = memberPredicate;
            }
            if (stringifyPredicate != null)
            {
                memberStringifyPredicate = stringifyPredicate;
            }

            memberChildIterationPredicate = childIteratePredicate;
        }
    }
}
