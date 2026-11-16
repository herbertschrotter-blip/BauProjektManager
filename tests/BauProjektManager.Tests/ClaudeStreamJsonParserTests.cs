using BauProjektManager.Domain.Enums;
using BauProjektManager.Infrastructure.Services.Claude;

namespace BauProjektManager.Tests;

/// <summary>
/// stream-json-Zeilen von "claude -p --output-format stream-json --verbose".
/// InitLine, AuthErrorAssistantLine und AuthErrorResultLine sind mit CLI 2.1.296 aufgezeichnet
/// (gekuerzt, cwd ersetzt); die Text-/Werkzeug-Zeilen folgen demselben Nachrichtenformat.
/// </summary>
public class ClaudeStreamJsonParserTests
{
    internal const string SessionId = "c33c25d8-e67b-4548-9318-5def6158a9fa";

    internal const string InitLine =
        """{"type":"system","subtype":"init","cwd":"C:\\Auftraege\\A-0001","session_id":"c33c25d8-e67b-4548-9318-5def6158a9fa","tools":["Edit","Glob","Read","Write"],"mcp_servers":[],"model":"claude-haiku-5-5","permissionMode":"acceptEdits","apiKeySource":"none","claude_code_version":"2.1.296","output_style":"default","uuid":"0b82d614-e45f-455c-8806-28e7bdcf90d6"}""";

    internal const string AuthErrorAssistantLine =
        """{"type":"assistant","message":{"id":"7f25c47c-ba5a-492f-99c6-f543300707da","model":"<synthetic>","role":"assistant","stop_reason":"stop_sequence","type":"message","content":[{"type":"text","text":"Failed to authenticate: OAuth session expired and could not be refreshed"}]},"parent_tool_use_id":null,"session_id":"c33c25d8-e67b-4548-9318-5def6158a9fa","uuid":"70c23576-a377-49c5-8028-3a1adfa291ae","error":"authentication_failed","is_api_error_message":true}""";

    internal const string AuthErrorResultLine =
        """{"duration_api_ms":0,"stop_reason":"stop_sequence","session_id":"c33c25d8-e67b-4548-9318-5def6158a9fa","total_cost_usd":0,"permission_denials":[],"terminal_reason":"api_error","is_error":true,"num_turns":1,"subtype":"success","result":"Failed to authenticate: OAuth session expired and could not be refreshed","type":"result","duration_ms":149}""";

    internal const string SuccessResultLine =
        """{"type":"result","subtype":"success","is_error":false,"num_turns":3,"result":"Entwurf geschrieben.","session_id":"c33c25d8-e67b-4548-9318-5def6158a9fa"}""";

    internal const string ToolUseLine =
        """{"type":"assistant","message":{"role":"assistant","content":[{"type":"tool_use","id":"toolu_01","name":"Write","input":{"file_path":"C:\\Auftraege\\A-0001\\entwurf.json","content":"{}"}}]},"parent_tool_use_id":null,"session_id":"c33c25d8-e67b-4548-9318-5def6158a9fa"}""";

    internal const string QuestionTextLine =
        """{"type":"assistant","message":{"role":"assistant","content":[{"type":"text","text":"Plan 3 ist unklar.\nFRAGE: Welcher Typ?\n1. Polierplan (Empfehlung)\n2. Schalplan – laut Plankopf"}]},"parent_tool_use_id":null,"session_id":"c33c25d8-e67b-4548-9318-5def6158a9fa"}""";

    internal const string ToolResultUserLine =
        """{"type":"user","message":{"role":"user","content":[{"type":"tool_result","tool_use_id":"toolu_01","content":"File created"}]},"session_id":"c33c25d8-e67b-4548-9318-5def6158a9fa"}""";

    [Fact]
    public void Parse_InitLine_ReturnsSessionIdWithoutEvents()
    {
        var result = ClaudeStreamJsonParser.Parse(InitLine);

        Assert.Equal(SessionId, result.SessionId);
        Assert.Empty(result.Events);
    }

    [Fact]
    public void Parse_RecordedAuthError_GivesTextAndErrorTurn()
    {
        var text = Assert.Single(ClaudeStreamJsonParser.Parse(AuthErrorAssistantLine).Events);
        var turn = Assert.Single(ClaudeStreamJsonParser.Parse(AuthErrorResultLine).Events);

        Assert.Equal(ClaudeEventKind.Text, text.Kind);
        Assert.Contains("Failed to authenticate", text.Text);
        Assert.Empty(text.Questions!);
        Assert.Equal(ClaudeEventKind.TurnCompleted, turn.Kind);
        Assert.True(turn.IsError);
        Assert.Contains("OAuth", turn.Text);
    }

    [Fact]
    public void Parse_SuccessResult_GivesTurnCompletedWithoutError()
    {
        var turn = Assert.Single(ClaudeStreamJsonParser.Parse(SuccessResultLine).Events);

        Assert.Equal(ClaudeEventKind.TurnCompleted, turn.Kind);
        Assert.False(turn.IsError);
        Assert.Null(turn.Text);
    }

    [Fact]
    public void Parse_ToolUse_GivesToolNameAndOnlyFileName()
    {
        var tool = Assert.Single(ClaudeStreamJsonParser.Parse(ToolUseLine).Events);

        Assert.Equal(ClaudeEventKind.ToolUse, tool.Kind);
        Assert.Equal("Write", tool.ToolName);
        Assert.Equal("entwurf.json", tool.ToolTarget);
    }

    [Fact]
    public void Parse_GlobToolUse_GivesPattern()
    {
        const string line = """{"type":"assistant","message":{"content":[{"type":"tool_use","name":"Glob","input":{"pattern":"eingang/*.json"}}]}}""";

        var tool = Assert.Single(ClaudeStreamJsonParser.Parse(line).Events);

        Assert.Equal("eingang/*.json", tool.ToolTarget);
    }

    [Fact]
    public void Parse_GlobWithAbsolutePattern_GivesOnlyLastSegment()
    {
        const string line = """{"type":"assistant","message":{"content":[{"type":"tool_use","name":"Glob","input":{"pattern":"C:\\Users\\herbert\\**\\*.json"}}]}}""";

        var tool = Assert.Single(ClaudeStreamJsonParser.Parse(line).Events);

        Assert.Equal("*.json", tool.ToolTarget);
    }

    [Fact]
    public void Parse_TextWithQuestion_ContainsParsedQuestion()
    {
        var ev = Assert.Single(ClaudeStreamJsonParser.Parse(QuestionTextLine).Events);

        var q = Assert.Single(ev.Questions!);
        Assert.Equal("Welcher Typ?", q.Title);
        Assert.Equal("Plan 3 ist unklar.", q.LeadText);
        Assert.Equal("laut Plankopf", q.Answers[1].Explanation);
    }

    [Fact]
    public void Parse_TextAndToolInOneMessage_GivesBothEvents()
    {
        const string line = """{"type":"assistant","message":{"content":[{"type":"text","text":"Ich lese den Kontext."},{"type":"tool_use","name":"Read","input":{"file_path":"/a/kontext.json"}}]}}""";

        var events = ClaudeStreamJsonParser.Parse(line).Events;

        Assert.Equal([ClaudeEventKind.ToolUse, ClaudeEventKind.Text], events.Select(e => e.Kind));
        Assert.Equal("kontext.json", events[0].ToolTarget);
    }

    [Theory]
    [InlineData(ToolResultUserLine)]
    [InlineData("""{"type":"stream_event","event":{}}""")]
    [InlineData("""{"type":"assistant","message":{"content":[{"type":"text","text":"   "}]}}""")]
    [InlineData("""{"type":"assistant"}""")]
    [InlineData("[1,2]")]
    [InlineData("kein json")]
    [InlineData("")]
    public void Parse_IrrelevantOrBrokenLine_GivesNoEvents(string line)
    {
        Assert.Empty(ClaudeStreamJsonParser.Parse(line).Events);
    }
}
