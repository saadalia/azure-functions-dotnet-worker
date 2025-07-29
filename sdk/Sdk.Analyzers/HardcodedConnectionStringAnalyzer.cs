// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;
using System.Collections.Immutable;
using System.Linq;
using System.Text.RegularExpressions;

namespace Microsoft.Azure.Functions.Worker.Sdk.Analyzers
{
    [DiagnosticAnalyzer(LanguageNames.CSharp)]
    public sealed class HardcodedConnectionStringAnalyzer : DiagnosticAnalyzer
    {
        public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics { get; } = 
            ImmutableArray.Create(DiagnosticDescriptors.HardcodedConnectionStringInBinding);

        // Connection string patterns to detect
        private static readonly string[] ConnectionStringPatterns = new[]
        {
            @"Default\s*Endpoints\s*Protocol\s*=",
            @"Account\s*Name\s*=",
            @"Account\s*Key\s*=",
            @"Connection\s*String\s*=",
            @"Server\s*=",
            @"Database\s*=",
            @"Data\s+Source\s*=",
            @"Initial\s+Catalog\s*=",
            @"Integrated\s+Security\s*=",
            @"User\s+ID\s*=",
            @"Password\s*=",
            @"Trusted_?Connection\s*=",
            @"Encrypt\s*=",
            @"Trust\s*Server\s*Certificate\s*=",
            @"Multiple\s*Active\s*Result\s*Sets\s*=",
            @"Endpoint\s*=",
            @"Shared\s*Access\s*Key\s*=",
            @"Shared\s*Access\s*Key\s*Name\s*=",
            @"Entity\s*Path\s*=",
            @"Use\s*Development\s*Storage\s*="
        };

        private static readonly Regex ConnectionStringRegex = new Regex(
            string.Join("|", ConnectionStringPatterns), 
            RegexOptions.IgnoreCase | RegexOptions.Compiled);

        public override void Initialize(AnalysisContext context)
        {
            context.EnableConcurrentExecution();
            context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
            context.RegisterSyntaxNodeAction(AnalyzeAttribute, SyntaxKind.Attribute);
        }

        private static void AnalyzeAttribute(SyntaxNodeAnalysisContext context)
        {
            var attributeSyntax = (AttributeSyntax)context.Node;
            
            // Check if this is a binding attribute with a Connection property
            if (attributeSyntax.ArgumentList?.Arguments == null)
                return;

            foreach (var argument in attributeSyntax.ArgumentList.Arguments)
            {
                // Look for Connection = "value" pattern
                if (argument.NameEquals?.Name?.Identifier.ValueText == "Connection")
                {
                    // Extract the string literal value
                    if (argument.Expression is LiteralExpressionSyntax literalExpression &&
                        literalExpression.IsKind(SyntaxKind.StringLiteralExpression))
                    {
                        var connectionValue = literalExpression.Token.ValueText;
                        
                        // Check if the connection value contains connection string patterns
                        if (!string.IsNullOrWhiteSpace(connectionValue) && 
                            ContainsConnectionStringPattern(connectionValue))
                        {
                            var diagnostic = Diagnostic.Create(
                                DiagnosticDescriptors.HardcodedConnectionStringInBinding,
                                literalExpression.GetLocation());
                            
                            context.ReportDiagnostic(diagnostic);
                        }
                    }
                }
            }
        }

        private static bool ContainsConnectionStringPattern(string value)
        {
            // Check for obvious connection string patterns
            return ConnectionStringRegex.IsMatch(value);
        }
    }
}