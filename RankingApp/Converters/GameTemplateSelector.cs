using RankingApp.Core.Models;

namespace RankingApp.Converters
{
    public class GameTemplateSelector : DataTemplateSelector
    {
        // Not using this Selector, created Interface IGame
        public DataTemplate GameTemplate { get; set; }
        public DataTemplate DoublesGameTemplate { get; set; }

        protected override DataTemplate OnSelectTemplate(object item, BindableObject container)
        {
            return item switch
            {
                Game => GameTemplate,
                DoublesGame => DoublesGameTemplate,
                _ => null
            };
        }
    }
}
