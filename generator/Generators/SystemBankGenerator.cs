using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Threading;
using static Simulation.Constants;

namespace Simulation.Generators
{
    [Generator(LanguageNames.CSharp)]
    internal class SystemBankGenerator : IIncrementalGenerator
    {
        void IIncrementalGenerator.Initialize(IncrementalGeneratorInitializationContext context)
        {
            IncrementalValuesProvider<ITypeSymbol?> types = context.SyntaxProvider.CreateSyntaxProvider(Predicate, Transform);
            context.RegisterSourceOutput(types.Collect(), Generate);
        }

        private static bool Predicate(SyntaxNode node, CancellationToken token)
        {
            return node.IsKind(SyntaxKind.StructDeclaration);
        }

        private void Generate(SourceProductionContext context, ImmutableArray<ITypeSymbol?> typesArray)
        {
            List<ITypeSymbol> types = new();
            foreach (ITypeSymbol? type in typesArray)
            {
                if (type is not null)
                {
                    //iterate through the types constructors, and skip this type if the default one is obsolete
                    bool skip = false;
                    foreach (ISymbol member in type.GetMembers())
                    {
                        if (member is IMethodSymbol methodSymbol)
                        {
                            if (methodSymbol.MethodKind == MethodKind.Constructor && methodSymbol.Parameters.Length == 0)
                            {
                                foreach (AttributeData attribute in methodSymbol.GetAttributes())
                                {
                                    if (attribute.AttributeClass?.ToDisplayString() == "System.ObsoleteAttribute")
                                    {
                                        skip = true;
                                        break;
                                    }
                                }

                                if (skip)
                                {
                                    break;
                                }
                            }
                        }
                    }

                    if (skip)
                    {
                        continue;
                    }

                    types.Add(type);
                }
            }

            if (types.Count > 1) //dont generate if just 1
            {
                //sort types by their declared order
                ITypeSymbol[] sortedTypes = new ITypeSymbol[types.Count];
                for (int i = 0; i < types.Count; i++)
                {
                    sortedTypes[i] = types[i];
                }

                sortedTypes = sortedTypes.OrderBy(GetSortOrderHint).ToArray();
                string source = Generate(sortedTypes, out string typeName);
                context.AddSource($"{typeName}.generated.cs", source);
            }
        }

        private static ITypeSymbol? Transform(GeneratorSyntaxContext context, CancellationToken token)
        {
            StructDeclarationSyntax node = (StructDeclarationSyntax)context.Node;
            ITypeSymbol? type = context.SemanticModel.GetDeclaredSymbol(node);
            if (type is null)
            {
                return null;
            }

            if (type is INamedTypeSymbol namedType)
            {
                if (namedType.IsGenericType)
                {
                    return null;
                }
            }

            if (type.IsRefLikeType)
            {
                return null;
            }

            if (type.DeclaredAccessibility != Accessibility.Public && type.DeclaredAccessibility != Accessibility.Internal)
            {
                return null;
            }

            foreach (INamedTypeSymbol interfaceSymbol in type.AllInterfaces)
            {
                string interfaceTypeName = interfaceSymbol.ToDisplayString();
                if (interfaceTypeName == SystemInterfaceTypeName || SystemInterfaceTypeName.EndsWith("." + interfaceTypeName))
                {
                    return type;
                }
            }

            return null;
        }

        public static string Generate(IReadOnlyList<ITypeSymbol> types, out string typeName)
        {
            string? assemblyName = types[0].ContainingAssembly?.Name;
            if (assemblyName is not null)
            {
                if (assemblyName.EndsWith(".Core"))
                {
                    assemblyName = assemblyName.Substring(0, assemblyName.Length - 5);
                }
            }

            SourceBuilder source = new();
            source.Append("using ");
            source.Append(Namespace);
            source.Append(';');
            source.AppendLine();

            source.Append("using ");
            source.Append(Namespace);
            source.Append('.');
            source.Append("Functions");
            source.Append(';');
            source.AppendLine();

            source.AppendLine("using Worlds;");
            source.AppendLine("using System;");
            source.AppendLine("using System.Runtime.InteropServices;");
            source.AppendLine();

            if (assemblyName is not null)
            {
                source.Append("namespace ");
                source.AppendLine(assemblyName);
                source.BeginGroup();
            }

            source.AppendLine("/// <summary>");
            source.AppendLine("/// Contains all types declared by this project.");
            source.AppendLine("/// </summary>");

            if (assemblyName is not null && assemblyName.EndsWith(".Systems"))
            {
                typeName = PluralSystemBankTypeNameFormat.Replace("{0}", assemblyName.Substring(0, assemblyName.Length - 8));
            }
            else
            {
                typeName = SystemBankTypeNameFormat.Replace("{0}", assemblyName ?? "");
            }

            typeName = typeName.Replace(".", "");
            source.Append("public readonly struct ");
            source.Append(typeName);
            source.Append(" : ");
            source.Append(SystemInterfaceTypeName);
            source.AppendLine();
            source.BeginGroup();
            {
                source.Append("unsafe readonly (StartSystem start, UpdateSystem update, FinishSystem finish, DisposeSystem dispose) ISystem.Functions");
                source.AppendLine();
                source.BeginGroup();
                {
                    source.Append("get");
                    source.AppendLine();
                    source.BeginGroup();
                    {
                        source.Append("return (new(&StartSystem), new(&UpdateSystem), new(&FinishSystem), new(&DisposeSystem));");
                        source.AppendLine();
                        source.AppendLine();

                        //start
                        source.Append("[UnmanagedCallersOnly]");
                        source.AppendLine();
                        source.Append("static void StartSystem(SystemContainer systemContainer, World world)");
                        source.AppendLine();
                        source.BeginGroup();
                        {
                            source.Append("if (systemContainer.IsSimulatorWorld(world))");
                            source.AppendLine();
                            source.BeginGroup();
                            {
                                foreach (ITypeSymbol systemType in types)
                                {
                                    source.Append("systemContainer.simulator.AddSystem(new ");
                                    source.Append(systemType.ToDisplayString());
                                    source.Append("());");
                                    source.AppendLine();
                                }
                            }
                            source.EndGroup();
                        }
                        source.EndGroup();
                        source.AppendLine();

                        //update
                        source.Append("[UnmanagedCallersOnly]");
                        source.AppendLine();
                        source.Append("static void UpdateSystem(SystemContainer systemContainer, World world, TimeSpan delta)");
                        source.AppendLine();
                        source.BeginGroup();
                        {
                        }
                        source.EndGroup();
                        source.AppendLine();

                        //finish
                        source.Append("[UnmanagedCallersOnly]");
                        source.AppendLine();
                        source.Append("static void FinishSystem(SystemContainer systemContainer, World world)");
                        source.AppendLine();
                        source.BeginGroup();
                        {
                            source.Append("if (systemContainer.IsSimulatorWorld(world))");
                            source.AppendLine();
                            source.BeginGroup();
                            {
                                for (int i = types.Count - 1; i >= 0; i--)
                                {
                                    ITypeSymbol systemType = types[i];
                                    source.Append("systemContainer.simulator.RemoveSystem<");
                                    source.Append(systemType.ToDisplayString());
                                    source.Append(">();");
                                    source.AppendLine();
                                }
                            }
                            source.EndGroup();
                        }
                        source.EndGroup();
                        source.AppendLine();

                        //dispose
                        source.Append("[UnmanagedCallersOnly]");
                        source.AppendLine();
                        source.Append("static void DisposeSystem(SystemContainer systemContainer, World world)");
                        source.AppendLine();
                        source.BeginGroup();
                        {
                        }
                        source.EndGroup();
                    }
                    source.EndGroup();
                }
                source.EndGroup();
                source.AppendLine();

                string shortTypeName = SystemInterfaceTypeName.Substring(SystemInterfaceTypeName.IndexOf('.') + 1);
                source.Append("readonly void IDisposable.Dispose()");
                source.AppendLine();
                source.BeginGroup();
                {
                }
                source.EndGroup();
                source.AppendLine();

                //start function
                source.Append("readonly void ");
                source.Append(shortTypeName);
                source.Append(".Start(in SystemContext context, in World world)");
                source.AppendLine();

                source.BeginGroup();
                {
                }
                source.EndGroup();

                //update function
                source.Append("readonly void ");
                source.Append(shortTypeName);
                source.Append(".Update(in SystemContext context, in World world, in TimeSpan delta)");
                source.AppendLine();

                source.BeginGroup();
                {
                }
                source.EndGroup();

                //finish function
                source.Append("readonly void ");
                source.Append(shortTypeName);
                source.Append(".Finish(in SystemContext context, in World world)");
                source.AppendLine();

                source.BeginGroup();
                {
                }
                source.EndGroup();
            }
            source.EndGroup();

            if (assemblyName is not null)
            {
                source.EndGroup();
            }

            return source.ToString();
        }

        private static int GetSortOrderHint(ITypeSymbol typeSymbol)
        {
            foreach (AttributeData attribute in typeSymbol.GetAttributes())
            {
                if (attribute.AttributeClass?.ToDisplayString() == "Simulation.SystemOrderAttribute")
                {
                    foreach (TypedConstant argument in attribute.ConstructorArguments)
                    {
                        if (argument.Kind == TypedConstantKind.Primitive && argument.Type?.SpecialType == SpecialType.System_Int32)
                        {
                            return (int)argument.Value!;
                        }
                    }
                }
            }

            return 0;
        }
    }
}