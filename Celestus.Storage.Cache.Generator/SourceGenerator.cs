using Celestus.Storage.Cache.Attributes;
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
                           CacheAttributeHelper.IsMarked(methodDeclaration);
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
                           CacheAttributeHelper.IsMarked(classDeclaration);
                },
                transform: (GeneratorSyntaxContext ctx, CancellationToken cancelToken) =>
                {
                    var classDeclaration = (ClassDeclarationSyntax)ctx.Node;
                    return classDeclaration;
                }
            );

            context.RegisterSourceOutput(classProvider, (sourceProductionContext, cachedClass) => Execute(cachedClass, sourceProductionContext));
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
            else if (!Name.TryGetName(classDeclaration, out string name))
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
            if (!Name.TryGetName(methodDeclaration, out string methodIdentifier))
            {
                return;
            }

            var methodModifiers = methodDeclaration.Modifiers;
            var returnType = methodDeclaration.ReturnType;
            var parameterDeclarations = MethodHelper.GetParameterDeclaration(methodDeclaration);
            var parameters = MethodHelper.GetParameters(methodDeclaration);
            var outputParameters = MethodHelper.GetOutputParameters(methodDeclaration);
            var tupleDeclaration = GetOutputTuple(methodDeclaration, outputParameters);
            var tupleOutVariableAssignment = GetOutputTupleOutVariableAssignment(outputParameters);
            var cacheAttributes = CacheAttributeHelper.GetCacheAttributes(methodDeclaration.AttributeLists, (location) => ReportUnknownCacheAttribute(context, location));
            var cacheStore = Name.GetCacheStoreVariableName(methodDeclaration);

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
                    {{indentation}}            _ = {{cacheStore}}.TrySet(uniqueKey, value, {{timeoutInMilliseconds}}, duration: TimeSpan.FromMilliseconds({{durationInMs}}));
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
            var cacheAttributes = CacheAttributeHelper.GetCacheAttributes(classDeclaration.AttributeLists, (location) => ReportUnknownCacheAttribute(context, location));

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

            string cacheKey = string.Empty;

            if (cacheAttributes.TryGetValue("key", out var key))
            {
                cacheKey = key.value;
            }

            var indentation = GetIndentation(namespaceContext.depth);

            var builder = new StringBuilder();
            _ = builder.AppendLine($"using Celestus.Storage.Cache;");
            _ = builder.AppendLine($"");
            _ = builder.AppendLine($"{namespaceContext.header}");
            _ = builder.AppendLine($"{indentation}{classInfo.modifiers} class {classInfo.name}");
            _ = builder.AppendLine($"{indentation}{{");

            if (ClassHelper.HasStaticMethods(classDeclaration))
            {
                _ = builder.AppendLine($"{indentation}    readonly static private ThreadCache _staticThreadCache = ThreadCache.Factory.GetOrCreateShared({cacheKey});");
                _ = builder.AppendLine($"{indentation}    public static ThreadCache StaticThreadCache => _staticThreadCache;");
            }

            if (ClassHelper.HasNonStaticMethods(classDeclaration))
            {
                _ = builder.AppendLine($"{indentation}    readonly private ThreadCache _threadCache = ThreadCache.Factory.GetOrCreateShared({cacheKey});");
                _ = builder.AppendLine($"{indentation}    public ThreadCache ThreadCache => _threadCache;");
            }

            _ = builder.AppendLine($"{indentation}}}");
            _ = builder.AppendLine($"{namespaceContext.footer}");

            var sourceCode = builder.ToString();

            context.AddSource($"{namespaceContext.path}{classInfo.name}.g.cs", sourceCode.Trim());
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
                if (!Name.TryGetName(currentNamespace, out string name))
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
                footer = footerBuilder.ToString().Trim(),
                path = pathBuilder.ToString(),
                depth = depth
            };
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
                _ = outputTuple.Append($", {parameter.Type} {Name.TryGetName(parameter)}");
            }

            if (outputTuple.Length > 2)
            {
                return $"({outputTuple.ToString().Substring(2)})";
            }
            else
            {
                return string.Empty;
            }
        }

        private string GetHashCodeForInputParameters(MethodDeclarationSyntax methodDeclaration, string indentation)
        {
            var inputParameters = MethodHelper.GetInputParameters(methodDeclaration);

            StringBuilder inputCheck = new();

            if (MethodHelper.IsStatic(methodDeclaration))
            {
                _ = inputCheck.AppendLine($"{indentation}var hashCode = 0;");
            }
            else
            {
                _ = inputCheck.AppendLine($"{indentation}var hashCode = GetHashCode();");
            }

            for (int i = 0; i < inputParameters.Count; i++)
            {
                _ = inputCheck.AppendLine($"{indentation}hashCode ^= {Name.TryGetName(inputParameters[i])}.GetHashCode();");
            }

            return inputCheck.ToString().TrimEnd();
        }

        private string GetOutputTupleOutVariableAssignment(List<ParameterSyntax> outParameters)
        {
            StringBuilder variableAssignment = new();

            foreach (var parameter in outParameters)
            {
                _ = variableAssignment.Append($", {Name.TryGetName(parameter)}");
            }

            return variableAssignment.ToString().Trim([' ', ',']);
        }

        private string GetOutVariableAssignment(List<ParameterSyntax> outParameters, string indentation)
        {
            StringBuilder variableAssignment = new();

            foreach (var parameter in outParameters)
            {
                var parameterName = Name.TryGetName(parameter);
                _ = variableAssignment.AppendLine($"{indentation}{parameterName} = value.{parameterName};");
            }

            return variableAssignment.ToString().TrimEnd();
        }

        private string GetIndentation(int depth)
        {
            const char INDENT_CHAR = ' ';
            const int NROF_CHARS_PER_INDENT = 4;

            return new string(INDENT_CHAR, depth++ * NROF_CHARS_PER_INDENT);
        }

        private void ReportUnknownCacheAttribute(SourceProductionContext context, Location location)
        {
            context.ReportDiagnostic(Diagnostic.Create(
                id: "CelestusCache3",
                category: "ArgumentException",
                message: $"Could not determine which Cache attribute argument belongs to.",
                severity: DiagnosticSeverity.Error,
                defaultSeverity: DiagnosticSeverity.Error,
                isEnabledByDefault: true,
                warningLevel: 0,
                location: location)
            );
        }
    }
}