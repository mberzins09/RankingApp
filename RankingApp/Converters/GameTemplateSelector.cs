using RankingApp.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RankingApp.Converters
{
    public class GameTemplateSelector : DataTemplateSelector
    {
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
