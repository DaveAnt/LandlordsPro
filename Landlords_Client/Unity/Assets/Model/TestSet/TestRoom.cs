using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace ETModel
{
    public sealed class TestRoom : Entity
    {
        public CancellationTokenSource waitCts;
        public CancellationTokenSource randCts;
        public Dictionary<int, string> gamers = new Dictionary<int, string>();
    }
}
