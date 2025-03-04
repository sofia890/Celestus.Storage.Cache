using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Celestus.Storage.Cache.Generator
{
    static class Name
    {
        static public string GetCacheStoreVariableName(MethodDeclarationSyntax methodDeclaration)
        {
            if (MethodHelper.IsStatic(methodDeclaration))
            {
                return "_staticThreadCache";
            }
            else
            {
                return "_threadCache";
            }
        }

        static public string TryGetName(ParameterSyntax parameter)
        {
            var value = parameter.Identifier.Value;
            return value?.ToString() ?? string.Empty;
        }

        static public bool TryGetName(NamespaceDeclarationSyntax namespaceSyntax, out string name)
        {
            name = namespaceSyntax.Name.ToString();

            return name != string.Empty;
        }

        static public bool TryGetName(MethodDeclarationSyntax node, out string name)
        {
            var value = node.Identifier.Value;
            name = value?.ToString() ?? string.Empty;

            return name != string.Empty;
        }

        static public bool TryGetName(ClassDeclarationSyntax node, out string name)
        {
            var value = node.Identifier.Value;
            name = value?.ToString() ?? string.Empty;

            return name != string.Empty;
        }

        public static bool TryGetName(NameColonSyntax nameColonSyntax, out string name)
        {
            return CacheAttributeHelper.TryGetName(nameColonSyntax, out name);
        }
    }
}
