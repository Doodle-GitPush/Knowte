namespace Antinote.Commands;

public class SlashCommandRegistry
{
    private readonly List<SlashCommand> _commands =
    [
        new("math",      "Evaluate a math expression",  "= "),
        new("list",      "Bulleted list item",           "- "),
        new("checklist", "Checklist with checkboxes",    "- [ ] "),
        new("todo",      "Single to-do item",            "- [ ] "),
        new("date",      "Insert today's date",          DateTime.Today.ToString("MMMM d, yyyy")),
        new("time",      "Insert current time",          DateTime.Now.ToString("HH:mm")),
        new("divider",   "Section separator",            "---"),
        new("code",      "Fenced code block",            "```\n\n```", 4),
        new("heading",   "Section heading",              "## "),
    ];

    public List<SlashCommand> GetAll() => _commands;

    public List<SlashCommand> Filter(string query) =>
        _commands
            .Where(c => c.Name.StartsWith(query.ToLower()))
            .ToList();
}
