using System.Windows.Controls;
using BauProjektManager.Infrastructure.Persistence;
using BauProjektManager.PlanManager.Services;
using BauProjektManager.PlanManager.ViewModels;

namespace BauProjektManager.PlanManager.Views;

/// <summary>
/// Plandaten-Tab (BPM-126 Slice a): tabellarische DB-Sicht auf den kuratierten
/// Planindex. Detail-Panel (Slice b), Segment-Editor (Slice c) und Excel-Export
/// (Slice d) folgen.
/// </summary>
public partial class PlanDataView : UserControl
{
    public PlanDataView()
    {
        Resources.Add("BoolToVis", new BoolToVisConverter());
        Resources.Add("InverseBoolToVis", new InverseBoolToVisConverter());
        InitializeComponent();
    }

    public PlanDataViewModel? ViewModel => DataContext as PlanDataViewModel;

    /// <summary>Vom Host (ProjectDetailView) aufgerufen, sobald die planmanager.db steht.</summary>
    public void Initialize(PlanManagerDatabase planDb, string projectId, ProjectDatabase? bpmDb)
    {
        var vm = new PlanDataViewModel(planDb, projectId, bpmDb);
        DataContext = vm;
        vm.Load();
    }
}
