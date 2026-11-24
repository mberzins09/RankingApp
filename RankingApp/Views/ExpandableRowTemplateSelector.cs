using RankingApp.Core.Models;

namespace RankingApp.Views;

/// <summary>
/// Flat list with expandable rows (Calendar / Results competitions, team matches):
/// a header row (<see cref="IExpandableRow"/>) or one of its child rows shown right below it when expanded.
/// </summary>
public class ExpandableRowTemplateSelector : DataTemplateSelector
{
    public DataTemplate? HeaderTemplate { get; set; }
    public DataTemplate? ChildTemplate { get; set; }

    protected override DataTemplate OnSelectTemplate(object item, BindableObject container) =>
        (item is IExpandableRow ? HeaderTemplate : ChildTemplate)
        ?? throw new InvalidOperationException("ExpandableRowTemplateSelector templates are not set.");
}
