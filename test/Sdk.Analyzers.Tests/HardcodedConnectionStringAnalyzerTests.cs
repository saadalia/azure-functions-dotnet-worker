using Xunit;
using AnalyzerTest = Microsoft.CodeAnalysis.CSharp.Testing.CSharpAnalyzerTest<Microsoft.Azure.Functions.Worker.Sdk.Analyzers.HardcodedConnectionStringAnalyzer, Microsoft.CodeAnalysis.Testing.DefaultVerifier>;
using AnalyzerVerifier = Microsoft.CodeAnalysis.CSharp.Testing.CSharpAnalyzerVerifier<Microsoft.Azure.Functions.Worker.Sdk.Analyzers.HardcodedConnectionStringAnalyzer, Microsoft.CodeAnalysis.Testing.DefaultVerifier>;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Testing;
using System.Collections.Immutable;

namespace Sdk.Analyzers.Tests
{
    public sealed class HardcodedConnectionStringAnalyzerTests
    {
        private const string DiagnosticId = "AZFW0017";

        [Theory]
        [InlineData("DefaultEndpointsProtocol=https;AccountName=test;AccountKey=key")]
        [InlineData("Server=localhost;Database=mydb;User ID=sa;Password=pass")]
        [InlineData("Data Source=localhost;Initial Catalog=mydb")]
        [InlineData("Endpoint=sb://test.servicebus.windows.net/;SharedAccessKeyName=key;SharedAccessKey=secret")]
        [InlineData("AccountName=myaccount;AccountKey=mykey")]
        [InlineData("ConnectionString=DefaultEndpointsProtocol=https")]
        [InlineData("UseDevelopmentStorage=true")]
        public async Task AnalyzerReportsWarningForHardcodedConnectionStrings(string connectionString)
        {
            string inputCode = $@"
using Microsoft.Azure.Functions.Worker;

namespace FunctionApp
{{
    public static class Function1
    {{
        [Function(nameof(Function1))]
        public static void Run([QueueTrigger(""myqueue"", Connection = ""{connectionString}"")] string queueItem)
        {{
        }}
    }}
}}";

            var test = new AnalyzerTest
            {
                ReferenceAssemblies = LoadRequiredDependencyAssemblies(),
                TestCode = inputCode
            };

            var expectedDiagnosticResult = AnalyzerVerifier
                                                .Diagnostic(DiagnosticId)
                                                .WithSeverity(Microsoft.CodeAnalysis.DiagnosticSeverity.Warning);

            test.ExpectedDiagnostics.Add(expectedDiagnosticResult);

            await test.RunAsync();
        }

        [Theory]
        [InlineData("MyConnectionSetting")]
        [InlineData("")]
        [InlineData("AzureWebJobsStorage")]
        [InlineData("Storage")]
        [InlineData("%MyStorageConnectionString%")]
        public async Task AnalyzerDoesNotReportForValidConnectionReferences(string connectionReference)
        {
            string inputCode = $@"
using Microsoft.Azure.Functions.Worker;

namespace FunctionApp
{{
    public static class Function1
    {{
        [Function(nameof(Function1))]
        public static void Run([QueueTrigger(""myqueue"", Connection = ""{connectionReference}"")] string queueItem)
        {{
        }}
    }}
}}";

            var test = new AnalyzerTest
            {
                ReferenceAssemblies = LoadRequiredDependencyAssemblies(),
                TestCode = inputCode
            };

            await test.RunAsync();
        }

        [Fact]
        public async Task AnalyzerReportsWarningForBlobConnectionString()
        {
            string inputCode = @"
using Microsoft.Azure.Functions.Worker;

namespace FunctionApp
{
    public static class Function1
    {
        [Function(nameof(Function1))]
        public static void Run([BlobTrigger(""container/path"", Connection = ""DefaultEndpointsProtocol=https;AccountName=test;AccountKey=key"")] string blob)
        {
        }
    }
}";

            var test = new AnalyzerTest
            {
                ReferenceAssemblies = LoadRequiredDependencyAssemblies(),
                TestCode = inputCode
            };

            var expectedDiagnosticResult = AnalyzerVerifier
                                                .Diagnostic(DiagnosticId)
                                                .WithSeverity(Microsoft.CodeAnalysis.DiagnosticSeverity.Warning);

            test.ExpectedDiagnostics.Add(expectedDiagnosticResult);

            await test.RunAsync();
        }

        [Fact]
        public async Task AnalyzerReportsWarningForServiceBusConnectionString()
        {
            string inputCode = @"
using Microsoft.Azure.Functions.Worker;

namespace FunctionApp
{
    public static class Function1
    {
        [Function(nameof(Function1))]
        public static void Run([ServiceBusTrigger(""myqueue"", Connection = ""Endpoint=sb://test.servicebus.windows.net/;SharedAccessKeyName=key;SharedAccessKey=secret"")] string message)
        {
        }
    }
}";

            var test = new AnalyzerTest
            {
                ReferenceAssemblies = LoadRequiredDependencyAssemblies(),
                TestCode = inputCode
            };

            var expectedDiagnosticResult = AnalyzerVerifier
                                                .Diagnostic(DiagnosticId)
                                                .WithSeverity(Microsoft.CodeAnalysis.DiagnosticSeverity.Warning);

            test.ExpectedDiagnostics.Add(expectedDiagnosticResult);

            await test.RunAsync();
        }

        [Fact]
        public async Task AnalyzerReportsWarningForSqlConnectionString()
        {
            string inputCode = @"
using Microsoft.Azure.Functions.Worker;

namespace FunctionApp
{
    public static class Function1
    {
        [Function(nameof(Function1))]
        public static void Run([SqlTrigger(""dbo.table"", Connection = ""Server=localhost;Database=mydb;User ID=sa;Password=pass"")] string change)
        {
        }
    }
}";

            var test = new AnalyzerTest
            {
                ReferenceAssemblies = LoadRequiredDependencyAssemblies(),
                TestCode = inputCode
            };

            var expectedDiagnosticResult = AnalyzerVerifier
                                                .Diagnostic(DiagnosticId)
                                                .WithSeverity(Microsoft.CodeAnalysis.DiagnosticSeverity.Warning);

            test.ExpectedDiagnostics.Add(expectedDiagnosticResult);

            await test.RunAsync();
        }

        [Fact]
        public async Task AnalyzerReportsWarningForOutputBindingConnectionString()
        {
            string inputCode = @"
using Microsoft.Azure.Functions.Worker;

namespace FunctionApp
{
    public static class Function1
    {
        [Function(nameof(Function1))]
        [QueueOutput(""myqueue"", Connection = ""DefaultEndpointsProtocol=https;AccountName=test;AccountKey=secret"")]
        public static string Run([TimerTrigger(""0 */5 * * * *"")] string timer)
        {
            return ""message"";
        }
    }
}";

            var test = new AnalyzerTest
            {
                ReferenceAssemblies = LoadRequiredDependencyAssemblies(),
                TestCode = inputCode
            };

            var expectedDiagnosticResult = AnalyzerVerifier
                                                .Diagnostic(DiagnosticId)
                                                .WithSeverity(Microsoft.CodeAnalysis.DiagnosticSeverity.Warning);

            test.ExpectedDiagnostics.Add(expectedDiagnosticResult);

            await test.RunAsync();
        }

        [Fact]
        public async Task AnalyzerDoesNotReportForAttributeWithoutConnectionProperty()
        {
            string inputCode = @"
using Microsoft.Azure.Functions.Worker;

namespace FunctionApp
{
    public static class Function1
    {
        [Function(nameof(Function1))]
        public static void Run([TimerTrigger(""0 */5 * * * *"")] string timer)
        {
        }
    }
}";

            var test = new AnalyzerTest
            {
                ReferenceAssemblies = LoadRequiredDependencyAssemblies(),
                TestCode = inputCode
            };

            await test.RunAsync();
        }

        [Fact]
        public async Task AnalyzerHandlesCaseInsensitiveConnectionStringPatterns()
        {
            string inputCode = @"
using Microsoft.Azure.Functions.Worker;

namespace FunctionApp
{
    public static class Function1
    {
        [Function(nameof(Function1))]
        public static void Run([QueueTrigger(""myqueue"", Connection = ""accountname=test;accountkey=secret"")] string queueItem)
        {
        }
    }
}";

            var test = new AnalyzerTest
            {
                ReferenceAssemblies = LoadRequiredDependencyAssemblies(),
                TestCode = inputCode
            };

            var expectedDiagnosticResult = AnalyzerVerifier
                                                .Diagnostic(DiagnosticId)
                                                .WithSeverity(Microsoft.CodeAnalysis.DiagnosticSeverity.Warning);

            test.ExpectedDiagnostics.Add(expectedDiagnosticResult);

            await test.RunAsync();
        }

        [Fact]
        public async Task AnalyzerDetectsConnectionStringWithSpacing()
        {
            string inputCode = @"
using Microsoft.Azure.Functions.Worker;

namespace FunctionApp
{
    public static class Function1
    {
        [Function(nameof(Function1))]
        public static void Run([QueueTrigger(""myqueue"", Connection = ""Default Endpoints Protocol = https; Account Name = test"")] string queueItem)
        {
        }
    }
}";

            var test = new AnalyzerTest
            {
                ReferenceAssemblies = LoadRequiredDependencyAssemblies(),
                TestCode = inputCode
            };

            var expectedDiagnosticResult = AnalyzerVerifier
                                                .Diagnostic(DiagnosticId)
                                                .WithSeverity(Microsoft.CodeAnalysis.DiagnosticSeverity.Warning);

            test.ExpectedDiagnostics.Add(expectedDiagnosticResult);

            await test.RunAsync();
        }

        private static ReferenceAssemblies LoadRequiredDependencyAssemblies()
        {
            var referenceAssemblies = ReferenceAssemblies.Net.Net50.WithPackages(ImmutableArray.Create(
                new PackageIdentity("Microsoft.Azure.WebJobs.Extensions", "4.0.1"),
                new PackageIdentity("Microsoft.Azure.Functions.Worker", "1.1.0"),
                new PackageIdentity("Microsoft.Azure.Functions.Worker.Extensions.Storage", "4.0.4")));

            return referenceAssemblies;
        }
    }
}