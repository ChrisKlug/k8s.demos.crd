using System.Threading.Tasks;

namespace CrdController.Services
{
    public class DummyLeaderSelector : ILeaderSelector
    {
        private readonly bool _isLeader;

        public DummyLeaderSelector(bool isLeader = true)
        {
            _isLeader = isLeader;
        }

        public Task<bool> IsLeader()
        {
            return Task.FromResult(_isLeader);
        }
    }
}
