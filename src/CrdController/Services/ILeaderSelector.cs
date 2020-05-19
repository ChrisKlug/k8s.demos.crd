using System.Collections.Generic;
using System.Threading.Tasks;

namespace CrdController.Services
{
    public interface ILeaderSelector
    {
        Task<bool> IsLeader();
    }
}
