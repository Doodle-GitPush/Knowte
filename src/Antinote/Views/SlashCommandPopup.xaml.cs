using System.Windows;
using System.Windows.Controls.Primitives;
using System.Windows.Media;
using System.Windows.Media.Animation;
using Knowte.Commands;

namespace Knowte.Views;

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

        if (commands.Count > 0 && !base.IsOpen)
            AnimateOpen();
        else if (commands.Count == 0 && base.IsOpen)
            AnimateClose();
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
            AnimateClose(() => CommandSelected?.Invoke(cmd));
    }

    public new bool IsOpen
    {
        get => base.IsOpen;
        set { if (!value && base.IsOpen) AnimateClose(); else base.IsOpen = value; }
    }

    private void AnimateOpen()
    {
        base.IsOpen = true;
        var duration = new Duration(TimeSpan.FromMilliseconds(120));
        var ease = new CubicEase { EasingMode = EasingMode.EaseOut };

        PopupBorder.BeginAnimation(UIElement.OpacityProperty, new DoubleAnimation(0, 1, duration) { EasingFunction = ease });
        SlideTransform.BeginAnimation(TranslateTransform.YProperty, new DoubleAnimation(8, 0, duration) { EasingFunction = ease });
    }

    private void AnimateClose(Action? onComplete = null)
    {
        var duration = new Duration(TimeSpan.FromMilliseconds(80));
        var opacityAnim = new DoubleAnimation(1, 0, duration);
        opacityAnim.Completed += (_, _) =>
        {
            base.IsOpen = false;
            onComplete?.Invoke();
        };

        PopupBorder.BeginAnimation(UIElement.OpacityProperty, opacityAnim);
        SlideTransform.BeginAnimation(TranslateTransform.YProperty, new DoubleAnimation(0, 8, duration));
    }

    private void CommandList_MouseLeftButtonUp(object sender, System.Windows.Input.MouseButtonEventArgs e)
    {
        if (CommandList.SelectedItem is SlashCommand)
            ConfirmSelection();
    }
}
