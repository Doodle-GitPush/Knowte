namespace Antinote.Commands;

public record SlashCommand(
    string Name,
    string Description,
    string InsertText,
    int CursorOffsetFromEnd = 0
);
