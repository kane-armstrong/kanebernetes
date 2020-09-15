using System.Threading.Tasks;
using Pulumi;

namespace Kanebernetes.Stack
{
    class Program
    {
        static Task<int> Main() => Deployment.RunAsync<KanbernetesStack>();
    }
}
