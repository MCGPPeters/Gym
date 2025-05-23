using Avalonia.Controls;
using System.Collections.Generic;
using Gymnasium.UI.Models;

namespace Gymnasium.UI.Views;

public partial class TrajectoryTableView : UserControl
{
    private DataGrid? _table;
    public TrajectoryTableView()
    {
        InitializeComponent();
        this.AttachedToVisualTree += (s, e) => _table = this.FindControl<DataGrid>("TrajectoryTable");
    }

    public void SetTrajectory(IReadOnlyList<EpisodeTrajectory>? trajectory)
    {
        if (_table == null) return;
        _table.Items = trajectory ?? new List<EpisodeTrajectory>();
    }
}
