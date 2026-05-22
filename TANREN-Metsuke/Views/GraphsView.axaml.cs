using System;
using Avalonia.Controls;
using TANREN_Metsuke.Models;
using TANREN_Metsuke.ViewModels;

namespace TANREN_Metsuke.Views;

public partial class GraphsView : UserControl
{
    private GraphsViewModel? viewModel;
    private Action<WorkoutSession>? handler;

    public GraphsView()
    {
        InitializeComponent();
        DataContextChanged += OnDataContextChanged;
    }

    // unsubscribes from the old view model, then adds the WorkoutSessionClicked event on the new one
    // clicking a bar opens the WorkoutDetailWindow without leaking handlers across view model rebuilds
    private void OnDataContextChanged(object? sender, EventArgs e)
    {
        if (viewModel != null && handler != null)
            viewModel.WorkoutSessionClicked -= handler;

        viewModel = DataContext as GraphsViewModel;

        if (viewModel != null)
        {
            handler = session =>
            {
                var dialog = new WorkoutDetailWindow(session, viewModel!.Imperial);
                if (TopLevel.GetTopLevel(this) is Window owner)
                    dialog.ShowDialog(owner);
                else
                    dialog.Show();
            };
            viewModel.WorkoutSessionClicked += handler;
        }
    }
}
