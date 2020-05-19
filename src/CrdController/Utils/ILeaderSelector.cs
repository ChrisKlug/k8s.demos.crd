using System.Collections.Generic;
using System.Threading.Tasks;

namespace CrdController.Utils
{
    public interface ILeaderSelector
    {
        Task<bool> IsLeader();
    }
}
