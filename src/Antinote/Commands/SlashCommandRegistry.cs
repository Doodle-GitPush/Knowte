namespace Antinote.Commands;

public class SlashCommandRegistry
{
    private readonly List<SlashCommand> _commands =
    [
        new("x",         "Mark task done",         ""),
        new("math",      "Activate math mode",      "math;"),
        new("list",      "Activate list mode",      "list;"),
        new("checklist", "Activate checklist mode", "checklist;"),
        new("convert",   "Activate convert mode",   "convert;"),
        new("paste",     "Activate paste mode",     "paste;"),
        new("date",      "Insert today's date",     DateTime.Today.ToString("MMMM d, yyyy")),
        new("time",      "Insert current time",     DateTime.Now.ToString("HH:mm")),
        new("divider",   "Section separator",       "---"),
        new("code",      "Fenced code block",       "```\n\n```", 4),
        new("heading",   "Section heading",         "## "),
        new("lorem",     "Insert lorem ipsum",      "Lorem ipsum dolor sit amet, consectetur adipiscing elit, sed do eiusmod tempor incididunt ut labore et dolore magna aliqua. Ut enim ad minim veniam, quis nostrud exercitation ullamco laboris nisi ut aliquip ex ea commodo consequat. Duis aute irure dolor in reprehenderit in voluptate velit esse cillum dolore eu fugiat nulla pariatur."),
        new("timer",     "Activate timer mode",     "timer;"),
    ];

    public List<SlashCommand> GetAll() => _commands;

    public List<SlashCommand> Filter(string query) =>
        _commands
            .Where(c => c.Name.StartsWith(query.ToLower()))
            .ToList();
}
