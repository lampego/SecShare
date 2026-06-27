using Spectre.Console;

namespace SecShare.Console.Services.Download;

public sealed class ConsoleDecryptionKeyReader : IDecryptionKeyReader
{
    public string ReadDecryptionKey()
    {
        AnsiConsole.MarkupLine("Enter decryption key:");

        return AnsiConsole.Prompt(
            new TextPrompt<string>(">")
                .Secret()
                .Validate(key => string.IsNullOrWhiteSpace(key)
                    ? ValidationResult.Error("Decryption key is required.")
                    : ValidationResult.Success()
                )
        );
    }
}
