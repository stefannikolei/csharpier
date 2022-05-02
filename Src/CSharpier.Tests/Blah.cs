using System;
using System.Reflection;
using FluentAssertions;
using Microsoft.AspNetCore.Razor.Language;
using Microsoft.AspNetCore.Razor.Language.CodeGeneration;
using Microsoft.AspNetCore.Razor.Language.Intermediate;
using Microsoft.CodeAnalysis;
using NUnit.Framework;

namespace CSharpier.Tests;

[TestFixture]
public class BlazorParse
{
    [Test]
    public void DoWork()
    {
        var sourceDocument = RazorSourceDocument.Create(
            this.blazor,
            new RazorSourceDocumentProperties("Test.cshtml", null)
        );
        var codeDocument = RazorCodeDocument.Create(sourceDocument);
        var syntaxTree = RazorSyntaxTree.Parse(codeDocument.Source);
        object node = null;
        foreach (
            var property in syntaxTree
                .GetType()
                .GetProperties(BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public)
        )
        {
            if (!property.Name.Equals("Root"))
            {
                continue;
            }
            node = property.GetValue(syntaxTree);
        }
        // this is a RazorDocumentSyntax which is a Microsoft.AspNetCore.Razor.Language.Syntax.SyntaxNode
        // it is possible to get to the code blocks through the properties of it and its children
        // but everything seems to be internal
        node.Should().NotBeNull();
    }

    private string razor =
        @"@{
    string message = ""foreignObject example with Scalable Vector Graphics (SVG)"";
}

<svg width=""200"" height=""200"" xmlns=""http://www.w3.org/2000/svg"">
    <rect x=""0"" y=""0"" rx=""10"" ry=""10"" width=""200"" height=""200"" stroke=""black"" 
fill=""none"" />
    <foreignObject x=""20"" y=""20"" width=""160"" height=""160"">
    <p>@message</p>
    </foreignObject>
    </svg>";

    private string blazor =
        @"@page ""/counter""

<PageTitle>Counter</PageTitle>

<h1>Counter</h1>

<p>Current count: @currentCount</p>

<button class=""btn btn-primary"" @onclick=""IncrementCount"">Click me</button>

@code {
    private int currentCount = 0;

    [Parameter]
    public int IncrementAmount { get; set; } = 1;

    private void IncrementCount()
    {
        currentCount += IncrementAmount;
    }
}";
}
