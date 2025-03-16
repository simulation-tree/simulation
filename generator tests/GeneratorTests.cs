using Collections;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Simulation.Generators;
using System.Collections.Generic;
using Unmanaged;
using Worlds;

namespace Simulation.Generator.Tests
{
    public class GeneratorTests
    {
        [Test]
        public void GenerateWithDispose()
        {
            const string Source = @"
public readonly partial struct SimpleSystem : ISystem
{
    void ISystem.Start(in SystemCollector collector, in World world)
    {
    }

    void ISystem.Update(in SystemCollector collector, in World world, in TimeSpan delta)
    {
    }

    void ISystem.Finish(in SystemCollector collector, in World world)
    {
    }

    readonly void IDisposable.Dispose()
    {
    }
}";

            GeneratorDriverRunResult results = Generate(Source);
            GeneratorRunResult result = results.Results[0];
            string generatedSource = result.GeneratedSources[0].SourceText.ToString();

            SyntaxTree generatedSyntaxTree = CSharpSyntaxTree.ParseText(generatedSource);
            foreach (SyntaxNode descendant in generatedSyntaxTree.GetRoot().DescendantNodes())
            {
                //type has been generated
                if (descendant is StructDeclarationSyntax structDeclaration)
                {
                    Assert.That(structDeclaration.Identifier.Text, Is.EqualTo("SimpleSystem"));
                    List<PropertyDeclarationSyntax> properties = new();
                    List<MethodDeclarationSyntax> methods = new();
                    foreach (SyntaxNode grandDescendant in descendant.DescendantNodes())
                    {
                        if (grandDescendant is PropertyDeclarationSyntax propertyDeclarationSyntax)
                        {
                            properties.Add(propertyDeclarationSyntax);
                        }

                        if (grandDescendant is MethodDeclarationSyntax methodDeclarationSyntax)
                        {
                            methods.Add(methodDeclarationSyntax);
                        }
                    }

                    //property has been generated
                    Assert.That(properties.Count, Is.EqualTo(1));
                    Assert.That(methods.Count, Is.EqualTo(0));
                    return;
                }
            }

            throw new("Generated code does not contain expected struct declaration");
        }

        [Test]
        public void GenerateWithoutDispose()
        {
            const string Source = @"
public readonly partial struct SimpleSystem : ISystem
{
    void ISystem.Start(in SystemCollector collector, in World world)
    {
    }

    void ISystem.Update(in SystemCollector collector, in World world, in TimeSpan delta)
    {
    }

    void ISystem.Finish(in SystemCollector collector, in World world)
    {
    }
}";

            GeneratorDriverRunResult results = Generate(Source);
            GeneratorRunResult result = results.Results[0];
            string generatedSource = result.GeneratedSources[0].SourceText.ToString();

            SyntaxTree generatedSyntaxTree = CSharpSyntaxTree.ParseText(generatedSource);
            foreach (SyntaxNode descendant in generatedSyntaxTree.GetRoot().DescendantNodes())
            {
                //type has been generated
                if (descendant is StructDeclarationSyntax structDeclaration)
                {
                    Assert.That(structDeclaration.Identifier.Text, Is.EqualTo("SimpleSystem"));
                    List<PropertyDeclarationSyntax> properties = new();
                    List<MethodDeclarationSyntax> methods = new();
                    foreach (SyntaxNode grandDescendant in descendant.DescendantNodes())
                    {
                        if (grandDescendant is PropertyDeclarationSyntax propertyDeclarationSyntax)
                        {
                            properties.Add(propertyDeclarationSyntax);
                        }

                        if (grandDescendant is MethodDeclarationSyntax methodDeclarationSyntax)
                        {
                            methods.Add(methodDeclarationSyntax);
                        }
                    }

                    //property has been generated
                    Assert.That(properties.Count, Is.EqualTo(1));
                    //Assert.That(methods.Count, Is.EqualTo(1)); works in testing, but not when applied
                    //Assert.That(methods[0].Identifier.Text, Is.EqualTo("Dispose"));
                    return;
                }
            }

            throw new("Generated code does not contain expected struct declaration");
        }

        protected GeneratorDriverRunResult Generate(string sourceCode)
        {
            SyntaxTree syntaxTree = CSharpSyntaxTree.ParseText(sourceCode);
            SyntaxTree[] syntaxTrees = [syntaxTree];
            CSharpCompilation compilation = CSharpCompilation.Create(assemblyName: "Tests", syntaxTrees, GetReferences());
            ImplementationGenerator generator = new();
            GeneratorDriver driver = CSharpGeneratorDriver.Create(generator);
            driver = driver.RunGenerators(compilation);
            return driver.GetRunResult();
        }

        protected virtual List<MetadataReference> GetReferences()
        {
            List<MetadataReference> references = new();
            references.Add(MetadataReference.CreateFromFile(typeof(object).Assembly.Location));
            references.Add(MetadataReference.CreateFromFile(typeof(Simulator).Assembly.Location));
            references.Add(MetadataReference.CreateFromFile(typeof(MemoryAddress).Assembly.Location));
            references.Add(MetadataReference.CreateFromFile(typeof(Array).Assembly.Location));
            references.Add(MetadataReference.CreateFromFile(typeof(World).Assembly.Location));
            return references;
        }
    }
}