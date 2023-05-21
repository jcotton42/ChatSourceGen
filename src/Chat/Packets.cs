namespace Chat;

public readonly struct Hello
{
    public required string Name { get; init; }
    public required string Password { get; init; }
}

public readonly struct Ping
{
    public required string Token { get; init; }
}

public readonly struct Pong
{
    public required string Token { get; init; }
}
