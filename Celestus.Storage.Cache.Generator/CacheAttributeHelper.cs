using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Celestus.Storage.Cache.Generator
{
    public static class CacheAttributeHelper
    {
        private const string ATTRIBUTE_NAME = "Cache";

        public static bool IsMarked(ClassDeclarationSyntax classDeclaration)
        {
            return classDeclaration.Members
                                   .Select(x => x as MethodDeclarationSyntax)
                                   .Any(x => x != null && IsMarked(x));
        }

        public static bool IsMarked(MethodDeclarationSyntax methodDeclaration)
        {
            return methodDeclaration.AttributeLists.Any(IsMarked);
        }

        public static bool IsMarked(AttributeListSyntax attributes)
        {
            return attributes.Attributes.Any(IsMarked);
        }

        public static bool IsMarked(AttributeSyntax attribute)
        {
            string name = attribute.Name.ToString();
            return name.StartsWith(ATTRIBUTE_NAME);
        }

        public static bool TryGetName(NameColonSyntax nameColonSyntax, out string name)
        {
            name = nameColonSyntax.Name.ToString();

            return name != string.Empty;
        }

        public static Dictionary<string, (string value, NameColonSyntax syntax)> GetCacheAttributes(
            SyntaxList<AttributeListSyntax> attributeLists,
            Action<Location> onUnknownAttribute)
        {
            var foundCacheArguments = new Dictionary<string, (string value, NameColonSyntax syntax)>();

            foreach (var attributeListOuter in attributeLists)
            {
                foreach (var attribute in attributeListOuter.Attributes)
                {
                    if (attribute.Name is IdentifierNameSyntax name &&
                        name.Identifier.ValueText == ATTRIBUTE_NAME)
                    {
                        var argumentList = attribute.ArgumentList.Arguments;

                        foreach (var argument in argumentList)
                        {
                            if (argument.NameColon == null ||
                                !TryGetName(argument.NameColon, out var attributeName))
                            {
                                onUnknownAttribute(Location.Create(argument.SyntaxTree, argument.Span));
                            }
                            else if (argument.Expression is IdentifierNameSyntax variableName)
                            {
                                foundCacheArguments.Add(attributeName, (variableName.Identifier.ValueText, argument.NameColon));
                            }
                            else
                            {
                                foundCacheArguments.Add(attributeName, (argument.Expression.ToString(), argument.NameColon));
                            }
                        }
                    }
                }
            }

            return foundCacheArguments;
        }
    }
}
