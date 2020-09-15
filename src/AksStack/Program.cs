using System.Threading.Tasks;
using Pulumi;

namespace AksStack
{
    class Program
    {
        static Task<int> Main() => Deployment.RunAsync<AksStack>();
    }
}
