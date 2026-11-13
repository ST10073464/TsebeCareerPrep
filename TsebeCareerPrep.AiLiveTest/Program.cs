
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using TsebeCareerPrep.Api.Models;
using TsebeCareerPrep.Api.Services;

// Controlled development-only Gemini assessment test.
// Uses fictional interview data.
// Does not access or modify the application database.

Console.WriteLine("Tsebe CareerPrep - Gemini AI Live Test");
Console.WriteLine("--------------------------------------");

// Load User Secrets from the API project.
// No API key is stored in this source file.
var configuration = new ConfigurationBuilder()
    .AddUserSecrets<GeminiInterviewAssessmentService>(
        optional: true)
    .AddInMemoryCollection(
        new Dictionary<string, string?>
        {
            ["Gemini:Model"] = "gemini-2.5-flash",
            ["Gemini:BaseUrl"] =
                "https://generativelanguage.googleapis.com/v1beta/",
            ["Gemini:TimeoutSeconds"] = "60"
        })
    .Build();

string? apiKey = configuration["Gemini:ApiKey"];

if (string.IsNullOrWhiteSpace(apiKey))
{
    Console.WriteLine(
        "Gemini API key was not found in User Secrets.");
    return;
}

// Register the same Gemini service and HTTP client
// used by the main API application.
var services = new ServiceCollection();

services.AddSingleton<IConfiguration>(configuration);

services.AddHttpClient("Gemini", client =>
{
    string baseUrl =
        configuration["Gemini:BaseUrl"]
        ?? "https://generativelanguage.googleapis.com/v1beta/";

    int timeoutSeconds =
        configuration.GetValue<int?>(
            "Gemini:TimeoutSeconds") ?? 60;

    client.BaseAddress = new Uri(
        baseUrl.TrimEnd('/') + "/");

    client.Timeout =
        TimeSpan.FromSeconds(timeoutSeconds);
});

services.AddScoped<
    IAiInterviewAssessmentService,
    GeminiInterviewAssessmentService>();

using var provider = services.BuildServiceProvider();
using var scope = provider.CreateScope();

var assessmentService = scope.ServiceProvider
    .GetRequiredService<IAiInterviewAssessmentService>();

// Fictional question and answer.
var response = new InterviewResponse
{
    AnswerText =
        "A class is a blueprint that defines properties " +
        "and methods. An object is an instance of that " +
        "class created in memory. For example, a Car " +
        "class can define a Colour property and a Start " +
        "method, while myCar is an object created " +
        "from that class.",

    InterviewQuestion = new InterviewQuestion
    {
        QuestionText =
            "What is the difference between a class " +
            "and an object in C#?"
    }
};

// Ten-mark provisional assessment rubric.
var rubric = new InterviewQuestionRubric
{
    IsActive = true,

    ExpectedConcepts =
        "Class as blueprint; object as instance; " +
        "properties; methods; practical example",

    AcceptableAlternatives =
        "Equivalent accurate object-oriented " +
        "programming explanations",

    AssessmentGuidance =
        "Assess technical correctness, understanding, " +
        "clarity and practical application.",

    TechnicalAccuracyMarks = 4,
    KeyConceptMarks = 3,
    ClarityMarks = 2,
    PracticalExampleMarks = 1
};

string model =
    configuration["Gemini:Model"]
    ?? "gemini-2.5-flash";

Console.WriteLine();
Console.WriteLine("Provider: Google Gemini");
Console.WriteLine($"Model: {model}");
Console.WriteLine("Assessment: Fictional C# interview");
Console.WriteLine("Maximum score: 10");
Console.WriteLine("Human review: Required");
Console.WriteLine("Database changes: None");
Console.WriteLine();

// No API request is made until explicitly approved.
Console.Write(
    "Send one live Gemini request? Type YES: ");

string? confirmation = Console.ReadLine();

if (confirmation != "YES")
{
    Console.WriteLine(
        "Cancelled. No API request was sent.");
    return;
}

Console.WriteLine();
Console.WriteLine("Requesting provisional assessment...");

try
{
    var result =
        await assessmentService
            .GenerateProvisionalAssessmentAsync(
                response,
                rubric);

    if (result == null)
    {
        Console.WriteLine(
            "No valid assessment was returned.");

        Console.WriteLine(
            "Possible causes include API access, " +
            "model availability, quota limits, " +
            "response format or rubric validation.");

        return;
    }

    Console.WriteLine();
    Console.WriteLine("PROVISIONAL GEMINI ASSESSMENT");
    Console.WriteLine("-----------------------------");

    Console.WriteLine(
        $"Suggested score: {result.SuggestedScore}/10");

    Console.WriteLine(
        $"Technical accuracy: " +
        $"{result.TechnicalAccuracyScore}/4");

    Console.WriteLine(
        $"Key concepts: {result.KeyConceptScore}/3");

    Console.WriteLine(
        $"Clarity: {result.ClarityScore}/2");

    Console.WriteLine(
        $"Practical example: " +
        $"{result.PracticalExampleScore}/1");

    Console.WriteLine();
    Console.WriteLine(
        $"Strengths: {result.Strengths}");

    Console.WriteLine(
        $"Areas for improvement: " +
        $"{result.AreasForImprovement}");

    Console.WriteLine(
        $"Improvement guidance: " +
        $"{result.ImprovementGuidance}");

    Console.WriteLine(
        $"Requires manual review: " +
        $"{result.RequiresManualReview}");

    Console.WriteLine();

    if (!result.RequiresManualReview)
    {
        Console.WriteLine(
            "WARNING: Assessment requires human review.");
        return;
    }

    Console.WriteLine(
        "SUCCESS: Gemini returned a valid " +
        "provisional assessment.");

    Console.WriteLine(
        "This assessment is not a final approved mark.");
}
catch (OperationCanceledException)
{
    Console.WriteLine(
        "The live request was cancelled or timed out.");
}
catch (Exception ex)
{
    // Never display API keys or sensitive request data.
    Console.WriteLine(
        $"Live test failed: {ex.GetType().Name}");
}
