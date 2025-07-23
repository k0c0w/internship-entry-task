namespace TicTacToe.Services;


public readonly struct Error
{
    public ErrorType Type { get; init; }

    public string Message { get; init; }

    public enum ErrorType
    {
        ValidationError = 1,
        NotFoundError = 2
    } 
}