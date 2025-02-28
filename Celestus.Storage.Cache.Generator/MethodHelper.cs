using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis;
using System.Linq;
using System.Collections.Generic;

namespace Celestus.Storage.Cache.Generator
{
    static class MethodHelper
    {
        public static bool IsStatic(SyntaxToken token)
        {
            return token.Text == "static";
        }

        public static bool IsStatic(MethodDeclarationSyntax methodDeclaration)
        {
            return methodDeclaration.Modifiers.Any(IsStatic);
        }

        public static bool IsNotStatic(SyntaxToken token)
        {
            return token.Text != "static";
        }

        public static string GetParameterDeclaration(MethodDeclarationSyntax methodDeclaration)
        {
            return methodDeclaration.ParameterList.ToString();
        }

        public static string GetParameters(MethodDeclarationSyntax methodDeclaration)
        {
            var parameters = methodDeclaration.ParameterList.Parameters;
            return parameters.Select(x => $"{x.Modifiers} {x.Identifier}".Trim())
                             .Aggregate("", (a, b) => $"{a}, {b}")
                             .Substring(2);
        }

        public static List<ParameterSyntax> GetOutputParameters(MethodDeclarationSyntax methodDeclaration)
        {
            List<ParameterSyntax> outParameters = [];

            foreach (var parameter in methodDeclaration.ParameterList.Parameters)
            {
                if (parameter.Modifiers.Any(x => x.Text == "out"))
                {
                    outParameters.Add(parameter);
                }
            }

            return outParameters;
        }

        public static List<ParameterSyntax> GetInputParameters(MethodDeclarationSyntax methodDeclaration)
        {
            List<ParameterSyntax> outParameters = [];

            foreach (var parameter in methodDeclaration.ParameterList.Parameters)
            {
                if (parameter.Modifiers.All(x => x.Text != "out"))
                {
                    outParameters.Add(parameter);
                }
            }

            return outParameters;
        }
    }
}
