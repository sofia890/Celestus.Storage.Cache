using Celestus.Storage.Cache.Attributes;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using System;
using System.Collections.Generic;
using System.Data.Common;
using System.Diagnostics;
using System.Linq;
using System.Reflection.Metadata;
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
            var tupleDeclaration = GetValueDeclaration(methodDeclaration, outputParameters);
            var tupleOutVariableAssignment = GetOutputTupleOutVariableAssignment(methodDeclaration, outputParameters);
            var cacheAttributes = CacheAttributeHelper.GetCacheAttributes(methodDeclaration.AttributeLists, (location) => ReportUnknownCacheAttribute(context, location));
            var cacheStore = Name.GetCacheStoreVariableName(methodDeclaration);
            string timeoutInMilliseconds = GetTimeoutInMs(cacheAttributes);
            string durationInMs = GetDurationInMs(cacheAttributes);
            string uniqueKeyBase = GetUniqueIdentifier(methodIdentifier, cacheAttributes);
            var indentation = GetIndentation(namespaceContext.depth);
            var indentationDeeper = indentation + "            ";
            var hacCodeInputParameters = GetHashCodeForParameters(methodDeclaration, indentation + "        ");
            var outVariableAssignment = GetOutVariableAssignment(methodDeclaration, outputParameters, indentationDeeper);
            var cacheElement = GetCacheElement(methodDeclaration, parameters, tupleOutVariableAssignment);
            string returnExpression = GetReturnExpression(methodDeclaration, tupleOutVariableAssignment);
            string methodCall = GetMethodCall(methodDeclaration, methodIdentifier, parameters);

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
                {{indentation}}            {{methodCall}}
                {{indentation}}            value = {{cacheElement}};
                {{indentation}}            
                {{indentation}}            // Avoid throwing an exception if TrySet(...) fails. Just try next time the value is fetched.
                {{indentation}}            _ = {{cacheStore}}.TrySet(uniqueKey, value, {{timeoutInMilliseconds}}, duration: TimeSpan.FromMilliseconds({{durationInMs}}));
                {{indentation}}        }
                {{indentation}}        else
                {{indentation}}        {
                {{outVariableAssignment}}
                {{indentation}}        }
                {{indentation}}        
                {{indentation}}        {{returnExpression}}
                {{indentation}}    }
                {{indentation}}}
                {{namespaceContext.footer}}
                """;

            context.AddSource($"{namespaceContext.path}{classInfo.name}.{methodIdentifier}.g.cs", sourceCode.Trim());
        }

        private static string GetTimeoutInMs(Dictionary<string, (string value, NameColonSyntax syntax)> cacheAttributes)
        {
            string timeoutInMilliseconds = $"{CacheAttribute.DEFAULT_TIMEOUT}";

            if (cacheAttributes.TryGetValue("timeoutInMilliseconds", out var timeout))
            {
                timeoutInMilliseconds = timeout.value;
            }

            return timeoutInMilliseconds;
        }

        private static string GetDurationInMs(Dictionary<string, (string value, NameColonSyntax syntax)> cacheAttributes)
        {
            string durationInMs = $"{CacheAttribute.DEFAULT_DURATION}";

            if (cacheAttributes.TryGetValue("durationInMs", out var duration))
            {
                durationInMs = duration.value;
            }

            return durationInMs;
        }

        private static string GetUniqueIdentifier(string methodIdentifier, Dictionary<string, (string value, NameColonSyntax syntax)> cacheAttributes)
        {
            string uniqueKeyBase = $"\"{methodIdentifier}\"";

            if (cacheAttributes.TryGetValue("key", out var key))
            {
                uniqueKeyBase = key.value;
            }

            return uniqueKeyBase;
        }

        private static string GetMethodCall(MethodDeclarationSyntax methodDeclaration, string methodIdentifier, string parameters)
        {
            var methodCall = $"{methodIdentifier}({parameters});";

            if (!ReturnsNull(methodDeclaration))
            {
                methodCall = $"var result = {methodCall}";
            }

            return methodCall;
        }

        private static string GetReturnExpression(MethodDeclarationSyntax methodDeclaration, string tupleOutVariableAssignment)
        {
            if (!ReturnsNull(methodDeclaration))
            {
                var returnValue = "return value";

                if (tupleOutVariableAssignment.Length != 0)
                {
                    returnValue += ".returnValue";
                }

                return $"{returnValue};";
            }
            else
            {
                return "// No return value!";
            }
        }

        private static string GetCacheElement(MethodDeclarationSyntax methodDeclaration, string parameters, string tupleOutVariableAssignment)
        {
            var value = string.Empty;

            if (tupleOutVariableAssignment.Length > 0)
            {
                if (!ReturnsNull(methodDeclaration))
                {
                    value = $"(returnValue: result, {tupleOutVariableAssignment})";
                } 
                else if (tupleOutVariableAssignment.Contains(","))
                {
                    value = $"({tupleOutVariableAssignment})";
                }
                else
                {
                    value = $"{tupleOutVariableAssignment}";
                }
            }
            else
            {
                value  = "result";
            }

            return value;
        }

        private static bool ReturnsNull(MethodDeclarationSyntax methodDeclaration)
        {
            return methodDeclaration.ReturnType.ToString() == "void";
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

        private string GetValueDeclaration(MethodDeclarationSyntax methodDeclaration, List<ParameterSyntax> outParameters)
        {
            StringBuilder outputTupleBuilder = new();
            int nrOfParameters = 0;

            if (!ReturnsNull(methodDeclaration))
            {
                _ = outputTupleBuilder.Append($", {methodDeclaration.ReturnType}");

                nrOfParameters++;
                
                if (outParameters.Count > 0)
                {
                    _ = outputTupleBuilder.Append(" returnValue");
                }
            }

            if (outParameters.Count == 1 && ReturnsNull(methodDeclaration))
            {
                var parameter = outParameters.First();
                _ = outputTupleBuilder.Append(parameter.Type);

                nrOfParameters++;
            }
            else if (outParameters.Count == 1 && !ReturnsNull(methodDeclaration))
            {
                var parameter = outParameters.First();
                _ = outputTupleBuilder.Append($", {parameter.Type} {Name.TryGetName(parameter)}");

                nrOfParameters++;
            }
            else
            {
                foreach (var parameter in outParameters)
                {
                    _ = outputTupleBuilder.Append($", {parameter.Type} {Name.TryGetName(parameter)}");

                    nrOfParameters++;
                }
            }

            var outputTuple = outputTupleBuilder.ToString().Trim([' ', ',']);

            if (nrOfParameters == 1)
            {
                return $"{outputTuple}";
            }
            else if (nrOfParameters > 1)
            {
                return $"({outputTuple})";
            }
            else
            {
                return string.Empty;
            }
        }

        private string GetHashCodeForParameters(MethodDeclarationSyntax methodDeclaration, string indentation)
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

        private string GetOutputTupleOutVariableAssignment(MethodDeclarationSyntax methodDeclaration, List<ParameterSyntax> outParameters)
        {
            StringBuilder variableAssignment = new();

            if (outParameters.Count == 1 && ReturnsNull(methodDeclaration))
            {
                var name = Name.TryGetName(outParameters.First());

                _ = variableAssignment.Append(name);
            }
            else
            {
                foreach (var parameter in outParameters)
                {
                    var name = Name.TryGetName(parameter);

                    _ = variableAssignment.Append($", {name}: {name}");
                }
            }

            return variableAssignment.ToString().Trim([' ', ',']);
        }

        private string GetOutVariableAssignment(MethodDeclarationSyntax methodDeclaration, List<ParameterSyntax> outParameters, string indentation)
        {
            StringBuilder variableAssignment = new();

            if (outParameters.Count == 1 && ReturnsNull(methodDeclaration))
            {
                var parameter = outParameters.First();
                var parameterName = Name.TryGetName(parameter);
                _ = variableAssignment.AppendLine($"{indentation}{parameterName} = value;");
            }
            else
            {
                foreach (var parameter in outParameters)
                {
                    var parameterName = Name.TryGetName(parameter);
                    _ = variableAssignment.AppendLine($"{indentation}{parameterName} = value.{parameterName};");
                }
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