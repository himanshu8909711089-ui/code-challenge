namespace CodeChallenge.Api.Logic;

public abstract record Result;

public record Success : Result;
public record Created<T>(T Value) : Result;
public record Updated : Result;
public record Deleted : Result;
public record NotFound(string Message) : Result;
public record Conflict(string Message) : Result;
public record ValidationError(Dictionary<string, string[]> Errors) : Result;
