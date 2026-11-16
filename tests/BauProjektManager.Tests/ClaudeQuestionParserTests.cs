using BauProjektManager.Infrastructure.Services.Claude;

namespace BauProjektManager.Tests;

/// <summary>
/// FRAGE-Parser fuer "Mit Claude sortieren" (Format des Auftragsrahmens, Vorbild ha-hausbau _rahmen.md).
/// </summary>
public class ClaudeQuestionParserTests
{
    [Fact]
    public void Parse_SingleQuestion_ReturnsAnswersWithRecommendationFirst()
    {
        var text = "FRAGE: Welcher Dokumenttyp?\n1. Polierplan (Empfehlung)\n2. Schalplan";

        var questions = ClaudeQuestionParser.Parse(text);

        var q = Assert.Single(questions);
        Assert.Equal("Welcher Dokumenttyp?", q.Title);
        Assert.False(q.AllowMultiple);
        Assert.Equal(2, q.Answers.Count);
        Assert.Equal(1, q.Answers[0].Number);
        Assert.Equal("Polierplan", q.Answers[0].Text);
        Assert.True(q.Answers[0].IsRecommended);
        Assert.False(q.Answers[1].IsRecommended);
        Assert.Null(q.LeadText);
    }

    [Fact]
    public void Parse_MultipleMarker_SetsAllowMultiple()
    {
        var text = "FRAGE (mehrere): Welche Geschosse betrifft der Plan?\n1. EG\n2. OG1\n3. OG2";

        var q = Assert.Single(ClaudeQuestionParser.Parse(text));

        Assert.True(q.AllowMultiple);
        Assert.Equal("Welche Geschosse betrifft der Plan?", q.Title);
        Assert.Equal(3, q.Answers.Count);
    }

    [Theory]
    [InlineData("2. Schalplan – Plankopf nennt Schalung")]
    [InlineData("2. Schalplan — Plankopf nennt Schalung")]
    public void Parse_ExplanationAfterDash_SplitsTextAndExplanation(string answerLine)
    {
        var text = $"FRAGE: Typ?\n1. Polierplan (Empfehlung)\n{answerLine}";

        var answer = Assert.Single(ClaudeQuestionParser.Parse(text)).Answers[1];

        Assert.Equal("Schalplan", answer.Text);
        Assert.Equal("Plankopf nennt Schalung", answer.Explanation);
    }

    [Fact]
    public void Parse_RecommendationBeforeExplanation_KeepsBoth()
    {
        var text = "FRAGE: Welches Geschoss?\n1. OG3 (Empfehlung) – steht im Plankopf\n2. OG2";

        var answer = Assert.Single(ClaudeQuestionParser.Parse(text)).Answers[0];

        Assert.Equal("OG3", answer.Text);
        Assert.True(answer.IsRecommended);
        Assert.Equal("steht im Plankopf", answer.Explanation);
    }

    [Fact]
    public void Parse_HyphenInAnswer_IsNotAnExplanation()
    {
        var text = "FRAGE: Welcher Bauteil?\n1. Haus 1 - Tiefgarage (Empfehlung)\n2. Haus 2";

        var answer = Assert.Single(ClaudeQuestionParser.Parse(text)).Answers[0];

        Assert.Equal("Haus 1 - Tiefgarage", answer.Text);
        Assert.Null(answer.Explanation);
    }

    [Fact]
    public void Parse_TextBeforeQuestion_BecomesLeadTextOfFirstQuestion()
    {
        var text = "Ich habe 12 Plaene gelesen, 2 sind unklar.\n\nFRAGE: Index von Plan 3?\n1. B (Empfehlung)\n2. C";

        var q = Assert.Single(ClaudeQuestionParser.Parse(text));

        Assert.Equal("Ich habe 12 Plaene gelesen, 2 sind unklar.", q.LeadText);
    }

    [Fact]
    public void Parse_SeveralBlocks_ReturnsAtMostFour()
    {
        var block = "FRAGE: Frage {0}?\n1. Ja (Empfehlung)\n2. Nein\n\n";
        var text = string.Concat(Enumerable.Range(1, 5).Select(i => string.Format(block, i)));

        var questions = ClaudeQuestionParser.Parse(text);

        Assert.Equal(ClaudeQuestionParser.MaxQuestions, questions.Count);
        Assert.Equal("Frage 1?", questions[0].Title);
        Assert.Equal("Frage 4?", questions[3].Title);
        Assert.All(questions, q => Assert.Equal(2, q.Answers.Count));
    }

    [Fact]
    public void Parse_BlockInCodeFenceWithParenNumbers_IsRecognized()
    {
        var text = "```\nFRAGE: Welcher Plantyp?\n1) Polierplan (Empfehlung)\n2) Werkplan\n```";

        var q = Assert.Single(ClaudeQuestionParser.Parse(text));

        Assert.Equal(["Polierplan", "Werkplan"], q.Answers.Select(a => a.Text));
    }

    [Fact]
    public void Parse_AnswersEndAtFollowingText()
    {
        var text = "FRAGE: Typ?\n1. A (Empfehlung)\n2. B\nDanach schreibe ich den Entwurf.\n3. kein Teil der Frage";

        var q = Assert.Single(ClaudeQuestionParser.Parse(text));

        Assert.Equal(2, q.Answers.Count);
    }

    [Fact]
    public void Parse_QuestionLineWithoutAnswers_IsIgnored()
    {
        Assert.Empty(ClaudeQuestionParser.Parse("FRAGE: Alles klar.\nIch schreibe jetzt den Entwurf."));
    }

    [Fact]
    public void Parse_NoFormatButEnumeration_FallsBackToNumberedAnswers()
    {
        var text = "Plan 4 ist nicht eindeutig.\nWelchen Typ soll ich nehmen?\n- Polierplan\n- Schalplan";

        var q = Assert.Single(ClaudeQuestionParser.Parse(text));

        Assert.Equal("Welchen Typ soll ich nehmen?", q.Title);
        Assert.Equal([1, 2], q.Answers.Select(a => a.Number));
        Assert.Equal("Polierplan", q.Answers[0].Text);
        Assert.Equal("Plan 4 ist nicht eindeutig.", q.LeadText);
    }

    [Fact]
    public void Parse_NoFormatQuestionThenProseThenList_ListIsNoAnswer()
    {
        var text = "Welche Plaene fehlen noch?\n\nIch habe folgendes erledigt:\n- Plan A umbenannt\n- Plan B verschoben";

        Assert.Empty(ClaudeQuestionParser.Parse(text));
    }

    [Fact]
    public void Parse_NoFormatYesNoQuestion_FallsBackToJaNein()
    {
        var text = "Der Entwurf hat 8 Zeilen. Soll ich die zwei Duplikate weglassen?";

        var q = Assert.Single(ClaudeQuestionParser.Parse(text));

        Assert.Equal("Soll ich die zwei Duplikate weglassen?", q.Title);
        Assert.Equal(["Ja", "Nein"], q.Answers.Select(a => a.Text));
        Assert.Equal("Der Entwurf hat 8 Zeilen.", q.LeadText);
    }

    [Theory]
    [InlineData("")]
    [InlineData("Entwurf geschrieben: 12 Zeilen, keine Unklarheiten.")]
    [InlineData("Welche Plaene fehlen noch?")]
    public void Parse_NoQuestion_ReturnsEmpty(string text)
    {
        Assert.Empty(ClaudeQuestionParser.Parse(text));
    }

    [Fact]
    public void FormatAnswer_SingleAndMultiple_UsesNumberAndText()
    {
        var q = Assert.Single(ClaudeQuestionParser.Parse("FRAGE (mehrere): Welche?\n1. EG – Erdgeschoss\n2. OG1\n3. OG2"));

        Assert.Equal("1. EG", ClaudeQuestionParser.FormatAnswer(q, [1]));
        Assert.Equal("1. EG, 3. OG2", ClaudeQuestionParser.FormatAnswer(q, [1, 3, 9]));
    }
}
