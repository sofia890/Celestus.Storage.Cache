using Celestus.Storage.Cache.Attributes;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using System.Linq;

namespace Celestus.Storage.Cache.Generator
{
    static class ClassHelper
    {
        static public bool HasStaticMethods(ClassDeclarationSyntax classDeclaration)
        {
            return classDeclaration.Members
                                   .Select(x => x as MethodDeclarationSyntax)
                                   .Where(x => x != null && CacheAttributeHelper.IsMarked(x))
                                   .Any(x => x.Modifiers.Any(MethodHelper.IsStatic));
        }

        static public bool HasNonStaticMethods(ClassDeclarationSyntax classDeclaration)
        {
            return classDeclaration.Members
                                   .Select(x => x as MethodDeclarationSyntax)
                                   .Where(x => x != null && CacheAttributeHelper.IsMarked(x))
                                   .All(x => x.Modifiers.Any(MethodHelper.IsNotStatic));
        }
    }
}
