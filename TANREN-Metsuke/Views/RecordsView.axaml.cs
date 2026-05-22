using Avalonia.Media;
using TANREN_Metsuke.Models;
using TANREN_Metsuke.ViewModels;

namespace TANREN_Metsuke.Views;

public partial class RecordsView : BodyMapView
{
    // accent cyan for muscles with records, gray for none
    private static readonly Color AccentColor = Color.Parse("#29B6F6");

    protected override byte HoverAlpha => 185;

    private RecordsViewModel? ViewModel => DataContext as RecordsViewModel;

    public RecordsView()
    {
        InitializeComponent();
        AttachBodyMap();
    }

    protected override void OnViewModelChanged()
    {
        if (ViewModel != null)
            HeaderText.Text = $"{ViewModel.TotalExercisesTracked} exercises tracked";
    }

    protected override Color ColorForGroup(MuscleGroup group) =>
        ViewModel != null && ViewModel.HasRecords(group) ? AccentColor : BodyMapHelper.MuscleInactive;

    protected override string HoverTextFor(MuscleGroup group)
    {
        var count = ViewModel?.RecordCount(group) ?? 0;
        return count == 0
            ? $"{group}  |  no exercises logged yet"
            : $"{group}  |  {count} exercise{(count == 1 ? "" : "s")} tracked  (click to view records)";
    }

    protected override string DefaultHoverText => "Hover over a muscle group (click to see personal records)";

    protected override bool CanOpenDetail(MuscleGroup group) => ViewModel != null && ViewModel.HasRecords(group);

    protected override void PopulateDetail(MuscleGroup group)
    {
        if (ViewModel == null)
            return;
        DetailTitle.Text = group.ToString();
        ExerciseRecordsList.ItemsSource = ViewModel.GetRecords(group);
    }
}
