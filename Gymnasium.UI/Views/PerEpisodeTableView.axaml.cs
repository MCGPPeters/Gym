using Avalonia.Controls;
using System.Collections.Generic;
using Gymnasium.UI.Models;

namespace Gymnasium.UI.Views;

public partial class PerEpisodeTableView : UserControl
{
    private DataGrid? _table;
    public PerEpisodeTableView()
    {
        InitializeComponent();
        this.AttachedToVisualTree += (s, e) => _table = this.FindControl<DataGrid>("EpisodeTable");
    }

    public void SetEpisodes(IReadOnlyList<EpisodeStats>? episodes)
    {
        if (_table == null) return;
        _table.ItemsSource = episodes ?? new List<EpisodeStats>();
    }
}
