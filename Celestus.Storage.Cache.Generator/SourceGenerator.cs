﻿using Celestus.Storage.Cache.Attributes;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading;

namespace Celestus.Storage.Cache.Generator
{
    internal class NamespaceContext
    {
        public string header = string.Empty;
        public string footer = string.Empty;
        public string path = string.Empty;
        public int depth;
    }

    class DeclarationInfo
    {
        public string name = string.Empty;
        public string modifiers = string.Empty;
    }

    [Generator]
    public class SourceGenerator : IIncrementalGenerator
    {
        public void Initialize(IncrementalGeneratorInitializationContext context)
        {
            if (!Debugger.IsAttached)
            {
                //Debugger.Launch();
            }

            IncrementalValuesProvider<MethodDeclarationSyntax> methodProvider = context.SyntaxProvider.CreateSyntaxProvider(
                predicate: (SyntaxNode node, CancellationToken cancelToken) =>
                {
                    return node is MethodDeclarationSyntax methodDeclaration &&
                           IsMarked(methodDeclaration);
                },
                transform: (GeneratorSyntaxContext ctx, CancellationToken cancelToken) =>
                {
                    var classDeclaration = (MethodDeclarationSyntax)ctx.Node;
                    return classDeclaration;
                }
             );

            context.RegisterSourceOutput(methodProvider, (sourceProductionContext, cachedMethod) => Execute(cachedMethod, sourceProductionContext));

            IncrementalValuesProvider<ClassDeclarationSyntax> classProvider = context.SyntaxProvider.CreateSyntaxProvider(
                predicate: (SyntaxNode node, CancellationToken cancelToken) =>
                {
                    return node is ClassDeclarationSyntax classDeclaration &&
                    IsMarked(classDeclaration);
                },
                transform: (GeneratorSyntaxContext ctx, CancellationToken cancelToken) =>
                {
                    var classDeclaration = (ClassDeclarationSyntax)ctx.Node;
                    return classDeclaration;
                }
            );

            context.RegisterSourceOutput(classProvider, (sourceProductionContext, cachedClass) => Execute(cachedClass, sourceProductionContext));
        }

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
            return name.StartsWith("Cache");
        }

        public void Execute(MethodDeclarationSyntax context, SourceProductionContext sourceProductionContext)
        {
            if (TryGetClassInfo(context, out DeclarationInfo classInfo))
            {
                var namespaceContext = ProcessNamespaces(context);
                ProcessMethod(sourceProductionContext, context, namespaceContext, classInfo);
            }
        }

        public void Execute(ClassDeclarationSyntax context, SourceProductionContext sourceProductionContext)
        {
            if (TryGetClassInfo(context, out DeclarationInfo classInfo))
            {
                var namespaceContext = ProcessNamespaces(context);
                ProcessClass(sourceProductionContext, context, namespaceContext, classInfo);
            }
        }

        bool TryGetClassInfo(MethodDeclarationSyntax methodDeclaration, out DeclarationInfo info)
        {
            info = new();

            var classDeclarations = methodDeclaration.Ancestors().OfType<ClassDeclarationSyntax>();
            var closestClass = classDeclarations.First();

            if (closestClass == null)
            {
                return false;
            }
            else
            {
                return TryGetClassInfo(closestClass, out info);
            }
        }

        bool TryGetClassInfo(ClassDeclarationSyntax classDeclaration, out DeclarationInfo info)
        {
            info = new();

            if (!TryGetClassModifiers(classDeclaration, out string classModifiers))
            {
                return false;
            }
            else if (!TryGetName(classDeclaration, out string name))
            {
                return false;
            }
            else
            {
                info.name = name;
                info.modifiers = classModifiers;
            }

            return true;
        }

        bool TryGetClassModifiers(ClassDeclarationSyntax syntax, out string classModifiers)
        {
            classModifiers = syntax.Modifiers.ToString();

            return classModifiers.Contains("partial");
        }

        void ProcessMethod(SourceProductionContext context,
                           MethodDeclarationSyntax methodDeclaration,
                           NamespaceContext namespaceContext,
                           DeclarationInfo classInfo)
        {
            if (!TryGetName(methodDeclaration, out string methodIdentifier))
            {
                return;
            }

            var methodModifiers = methodDeclaration.Modifiers;
            var returnType = methodDeclaration.ReturnType;
            var parameterDeclarations = GetParameterDeclaration(methodDeclaration);
            var parameters = GetParameters(methodDeclaration);
            var outputParameters = GetOutputParameters(methodDeclaration);
            var tupleDeclaration = GetOutputTuple(methodDeclaration, outputParameters);
            var tupleOutVariableAssignment = GetOutputTupleOutVariableAssignment(outputParameters);
            var cacheAttributes = GetCacheAttributes(context, methodDeclaration.AttributeLists);
            var cacheStore = GetCacheStore(methodDeclaration);

            string timeoutInMilliseconds = $"{CacheAttribute.DEFAULT_TIMEOUT}";

            if (cacheAttributes.TryGetValue("timeoutInMilliseconds", out var timeout))
            {
                timeoutInMilliseconds = timeout.value;
            }

            string durationInMs = $"{CacheAttribute.DEFAULT_DURATION}";

            if (cacheAttributes.TryGetValue("durationInMs", out var duration))
            {
                durationInMs = duration.value;
            }

            string uniqueKeyBase = methodIdentifier;

            if (cacheAttributes.TryGetValue("key", out var key))
            {
                uniqueKeyBase = key.value;
            }

            var indentation = GetIndentation(namespaceContext.depth);
            var indentationDeeper = indentation + "            ";

            var hacCodeInputParameters = GetHashCodeForInputParameters(methodDeclaration, indentation + "        ");
            var outVariableAssignment = GetOutVariableAssignment(outputParameters, indentationDeeper);

            string sourceCode =
                $$"""
                    {{namespaceContext.header}}
                    {{indentation}}{{classInfo.modifiers}} class {{classInfo.name}}
                    {{indentation}}{
                    {{indentation}}    {{methodModifiers}} {{returnType}} {{methodIdentifier}}Cached{{parameterDeclarations}}
                    {{indentation}}    {
                    {{hacCodeInputParameters}}
                    {{indentation}}        var uniqueKeyBase = {{uniqueKeyBase}};
                    {{indentation}}        var uniqueKey = $"{uniqueKeyBase}-{hashCode}";
                    {{indentation}}        
                    {{indentation}}        if ({{cacheStore}}.TryGet<{{tupleDeclaration}}>(uniqueKey, timeout: {{timeoutInMilliseconds}}) is not (result: true, var value))
                    {{indentation}}        {
                    {{indentation}}            value = ({{methodIdentifier}}({{parameters}}), {{tupleOutVariableAssignment}});
                    {{indentation}}            
                    {{indentation}}            // Avoid throwing an exception if TrySet(...) fails. Just try next time the value is fetched.
                    {{indentation}}            _ = {{cacheStore}}.TrySet(uniqueKey, value, duration: TimeSpan.FromMilliseconds({{durationInMs}}), timeout: {{timeoutInMilliseconds}});
                    {{indentation}}        }
                    {{indentation}}        else
                    {{indentation}}        {
                    {{outVariableAssignment}}
                    {{indentation}}        }
                    {{indentation}}        
                    {{indentation}}        return value.returnValue;
                    {{indentation}}    }
                    {{indentation}}}
                    {{namespaceContext.footer}}
                    """;

            context.AddSource($"{namespaceContext.path}{classInfo.name}.{methodIdentifier}.g.cs", sourceCode.Trim());
        }

        void ProcessClass(SourceProductionContext context,
                          ClassDeclarationSyntax classDeclaration,
                          NamespaceContext namespaceContext,
                          DeclarationInfo classInfo)
        {
            var cacheAttributes = GetCacheAttributes(context, classDeclaration.AttributeLists);

            if (cacheAttributes.TryGetValue("durationInMs", out var duration))
            {
                context.ReportDiagnostic(Diagnostic.Create(
                    id: "CelestusCache1",
                    category: "ArgumentException",
                    message: "The durationInMs attribute is not allowed on a class.",
                    severity: DiagnosticSeverity.Warning,
                    defaultSeverity: DiagnosticSeverity.Warning,
                    isEnabledByDefault: true,
                    warningLevel: 4,
                    location: Location.Create(classDeclaration.SyntaxTree, duration.syntax.Span)));
            }

            if (cacheAttributes.TryGetValue("timeoutInMilliseconds", out var timeout))
            {
                context.ReportDiagnostic(Diagnostic.Create(
                    id: "CelestusCache2",
                    category: "ArgumentException",
                    message: "The timeoutInMilliseconds attribute is not allowed on a class.",
                    severity: DiagnosticSeverity.Warning,
                    defaultSeverity: DiagnosticSeverity.Warning,
                    isEnabledByDefault: true,
                    warningLevel: 4,
                    location: Location.Create(classDeclaration.SyntaxTree, timeout.syntax.Span)));
            }

            string uniqueKey = string.Empty;

            if (cacheAttributes.TryGetValue("key", out var key))
            {
                uniqueKey = key.value;
            }

            var indentation = GetIndentation(namespaceContext.depth);

            var builder = new StringBuilder();
            _ = builder.AppendLine($"using Celestus.Storage.Cache;");
            _ = builder.AppendLine($"");
            _ = builder.AppendLine($"{namespaceContext.header}");
            _ = builder.AppendLine($"{indentation}{classInfo.modifiers} class {classInfo.name}");
            _ = builder.AppendLine($"{indentation}{{");

            if (HasStaticMethods(classDeclaration))
            {
                _ = builder.AppendLine($"{indentation}    readonly static private ThreadCache _staticThreadCache = new({uniqueKey});");
                _ = builder.AppendLine($"{indentation}    public static ThreadCache StaticThreadCache => _staticThreadCache;");
            }

            if (HasNonStaticMethods(classDeclaration))
            {
                _ = builder.AppendLine($"{indentation}    readonly private ThreadCache _threadCache = new({uniqueKey});");
                _ = builder.AppendLine($"{indentation}    public ThreadCache ThreadCache => _threadCache;");
            }

            _ = builder.AppendLine($"{indentation}}}");
            _ = builder.AppendLine($"{namespaceContext.footer}");

            var sourceCode = builder.ToString();

            context.AddSource($"{namespaceContext.path}{classInfo.name}.g.cs", sourceCode.Trim());
        }

        private bool HasStaticMethods(ClassDeclarationSyntax classDeclaration)
        {
            return classDeclaration.Members
                                   .Select(x => x as MethodDeclarationSyntax)
                                   .Where(x => x != null && IsMarked(x))
                                   .Any(x => x.Modifiers.Any(IsStatic));
        }

        private bool HasNonStaticMethods(ClassDeclarationSyntax classDeclaration)
        {
            return classDeclaration.Members
                                   .Select(x => x as MethodDeclarationSyntax)
                                   .Where(x => x != null && IsMarked(x))
                                   .All(x => x.Modifiers.Any(IsNotStatic));
        }

        private NamespaceContext ProcessNamespaces(SyntaxNode node)
        {
            var namespaceDeclarations = node.Ancestors().OfType<NamespaceDeclarationSyntax>();

            var headerBuilder = new StringBuilder();
            var footerBuilder = new StringBuilder();
            var pathBuilder = new StringBuilder();
            var depth = 0;

            foreach (var currentNamespace in namespaceDeclarations.Reverse())
            {
                if (!TryGetName(currentNamespace, out string name))
                {
                    continue;
                }

                _ = pathBuilder.Append($"{name}.");

                var indentation = GetIndentation(depth++);

                _ = headerBuilder.AppendLine($"{indentation}namespace {name}")
                                 .AppendLine($"{indentation}{{");

                _ = footerBuilder.Insert(0, $"{indentation}}}\r\n");
            }

            return new NamespaceContext()
            {
                header = headerBuilder.ToString().Trim(),
                footer = footerBuilder.ToString().TrimEnd(),
                path = pathBuilder.ToString(),
                depth = depth
            };
        }

        private string GetParameterDeclaration(MethodDeclarationSyntax methodDeclaration)
        {
            return methodDeclaration.ParameterList.ToString();
        }

        private string GetParameters(MethodDeclarationSyntax methodDeclaration)
        {
            var parameters = methodDeclaration.ParameterList.Parameters;
            return parameters.Select(x => $"{x.Modifiers} {x.Identifier}".Trim())
                             .Aggregate("", (a, b) => $"{a}, {b}")
                             .Substring(2);
        }

        private string GetOutputTuple(MethodDeclarationSyntax methodDeclaration, List<ParameterSyntax> outParameters)
        {
            StringBuilder outputTuple = new();

            if (methodDeclaration.ReturnType.GetType() != typeof(void))
            {
                _ = outputTuple.Append($", {methodDeclaration.ReturnType} returnValue");
            }

            foreach (var parameter in outParameters)
            {
                _ = outputTuple.Append($", {parameter.Type} {GetName(parameter)}");
            }

            return $"({outputTuple.ToString().Substring(2)})";
        }

        private List<ParameterSyntax> GetOutputParameters(MethodDeclarationSyntax methodDeclaration)
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

        private List<ParameterSyntax> GetInputParameters(MethodDeclarationSyntax methodDeclaration)
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

        private bool IsStatic(SyntaxToken token)
        {
            return token.Text == "static";
        }

        private bool IsStatic(MethodDeclarationSyntax methodDeclaration)
        {
            return methodDeclaration.Modifiers.Any(IsStatic);
        }

        private bool IsNotStatic(SyntaxToken token)
        {
            return token.Text != "static";
        }

        private string GetHashCodeForInputParameters(MethodDeclarationSyntax methodDeclaration, string indentation)
        {
            var inputParameters = GetInputParameters(methodDeclaration);

            StringBuilder inputCheck = new();

            if (IsStatic(methodDeclaration))
            {
                _ = inputCheck.AppendLine($"{indentation}var hashCode = 0;");
            }
            else
            {
                _ = inputCheck.AppendLine($"{indentation}var hashCode = GetHashCode();");
            }

            for (int i = 0; i < inputParameters.Count; i++)
            {
                _ = inputCheck.AppendLine($"{indentation}hashCode ^= {GetName(inputParameters[i])}.GetHashCode();");
            }

            return inputCheck.ToString().TrimEnd();
        }

        private string GetOutputTupleOutVariableAssignment(List<ParameterSyntax> outParameters)
        {
            StringBuilder variableAssignment = new();

            foreach (var parameter in outParameters)
            {
                _ = variableAssignment.Append($", {GetName(parameter)}");
            }

            return variableAssignment.ToString().Substring(2);
        }

        private string GetOutVariableAssignment(List<ParameterSyntax> outParameters, string indentation)
        {
            StringBuilder variableAssignment = new();

            foreach (var parameter in outParameters)
            {
                var parameterName = GetName(parameter);
                _ = variableAssignment.AppendLine($"{indentation}{parameterName} = value.{parameterName};");
            }

            return variableAssignment.ToString().TrimEnd();
        }
        private Dictionary<string, (string value, NameColonSyntax syntax)> GetCacheAttributes(
            SourceProductionContext context,
            SyntaxList<AttributeListSyntax> attributeLists)
        {
            Dictionary<string, (string value, NameColonSyntax syntax)> foundCacheArguments = [];

            foreach (var attributeListOuter in attributeLists)
            {
                foreach (var attribute in attributeListOuter.Attributes)
                {
                    if (attribute.Name is IdentifierNameSyntax name &&
                        name.Identifier.ValueText == "Cache")
                    {
                        var argumentList = attribute.ArgumentList.Arguments;

                        foreach (var argument in argumentList)
                        {
                            if (argument.NameColon is not NameColonSyntax nameColonSyntax ||
                                !TryGetName(nameColonSyntax, out var attributeName))
                            {
                                context.ReportDiagnostic(Diagnostic.Create(
                                    id: "CelestusCache3",
                                    category: "ArgumentException",
                                    message: $"Could not determine which Cache attribute argument belongs to.",
                                    severity: DiagnosticSeverity.Error,
                                    defaultSeverity: DiagnosticSeverity.Error,
                                    isEnabledByDefault: true,
                                    warningLevel: 10,
                                    location: Location.Create(argument.SyntaxTree, argument.Span))
                                );
                            }
                            else if (argument.Expression is IdentifierNameSyntax variableName)
                            {
                                foundCacheArguments.Add(attributeName, (variableName.Identifier.ValueText, nameColonSyntax));
                            }
                            else
                            {
                                foundCacheArguments.Add(attributeName, (argument.Expression.ToString(), nameColonSyntax));
                            }
                        }
                    }
                }
            }

            return foundCacheArguments;
        }

        private string GetCacheStore(MethodDeclarationSyntax methodDeclaration)
        {
            if (IsStatic(methodDeclaration))
            {
                return "_staticThreadCache";
            }
            else
            {
                return "_threadCache";
            }
        }

        private string GetName(ParameterSyntax parameter)
        {
            var value = parameter.Identifier.Value;
            return value?.ToString() ?? string.Empty;
        }

        private bool TryGetName(NamespaceDeclarationSyntax namespaceSyntax, out string name)
        {
            name = namespaceSyntax.Name.ToString();

            return name != string.Empty;
        }

        private bool TryGetName(MethodDeclarationSyntax node, out string name)
        {
            var value = node.Identifier.Value;
            name = value?.ToString() ?? string.Empty;

            return name != string.Empty;
        }

        private bool TryGetName(ClassDeclarationSyntax node, out string name)
        {
            var value = node.Identifier.Value;
            name = value?.ToString() ?? string.Empty;

            return name != string.Empty;
        }

        private bool TryGetName(NameColonSyntax nameColonSyntax, out string name)
        {
            name = nameColonSyntax.Name.ToString();

            return name != string.Empty;
        }

        private string GetIndentation(int depth)
        {
            const char _char = ' ';
            const int amount = 4;

            return new string(_char, depth++ * amount);
        }
    }
}