using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RankingApp.ViewModels
{
    internal interface ISaveBeforeNavigate
    {
        Task<bool> SaveBeforeNavigateAsync();
    }
}
