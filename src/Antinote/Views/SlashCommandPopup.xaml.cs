using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using Antinote.Commands;

namespace Antinote.Views;

public partial class SlashCommandPopup : Popup
{
    public event Action<SlashCommand>? CommandSelected;

    public SlashCommandPopup()
    {
        InitializeComponent();
    }

    public void UpdateCommands(List<SlashCommand> commands)
    {
        CommandList.ItemsSource = commands;
        if (commands.Count > 0)
            CommandList.SelectedIndex = 0;
        IsOpen = commands.Count > 0;
    }

    public void MoveSelectionUp()
    {
        if (CommandList.SelectedIndex > 0)
            CommandList.SelectedIndex--;
    }

    public void MoveSelectionDown()
    {
        if (CommandList.SelectedIndex < CommandList.Items.Count - 1)
            CommandList.SelectedIndex++;
    }

    public void ConfirmSelection()
    {
        if (CommandList.SelectedItem is SlashCommand cmd)
        {
            IsOpen = false;
            CommandSelected?.Invoke(cmd);
        }
    }

    private void CommandList_MouseLeftButtonUp(object sender, System.Windows.Input.MouseButtonEventArgs e)
    {
        if (CommandList.SelectedItem is SlashCommand)
            ConfirmSelection();
    }
}
