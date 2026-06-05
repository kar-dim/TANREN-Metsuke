using System;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using TANREN_Metsuke.Models;
using TANREN_Metsuke.ViewModels;

namespace TANREN_Metsuke.Views;

public partial class GraphsView : UserControl
{
    // a press that moves less than this (in pixels) between down and up counts as a click, not a pan
    private const double ClickThreshold = 5;

    private GraphsViewModel? viewModel;
    private Action<WorkoutSession>? handler;
    private Point workoutPressPosition;

    public GraphsView()
    {
        InitializeComponent();
        DataContextChanged += OnDataContextChanged;

        // runs before the chart's pan handling, we clear any pending bar before
        // ChartPointPointerDown records the one under the cursor (the release decides click vs pan)
        WorkoutChart.AddHandler(PointerPressedEvent, OnWorkoutPointerPressed, RoutingStrategies.Tunnel);
        WorkoutChart.AddHandler(PointerReleasedEvent, OnWorkoutPointerReleased, RoutingStrategies.Bubble);
    }

    private void OnWorkoutPointerPressed(object? sender, PointerPressedEventArgs e)
    {
        workoutPressPosition = e.GetPosition(WorkoutChart);
        viewModel?.ClearPendingWorkoutClick();
    }

    private void OnWorkoutPointerReleased(object? sender, PointerReleasedEventArgs e)
    {
        var delta = e.GetPosition(WorkoutChart) - workoutPressPosition;
        if (delta.X * delta.X + delta.Y * delta.Y <= ClickThreshold * ClickThreshold)
            viewModel?.CommitWorkoutClickIfPending();
        else
            viewModel?.ClearPendingWorkoutClick();
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
