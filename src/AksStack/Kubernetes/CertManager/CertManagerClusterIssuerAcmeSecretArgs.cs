using Pulumi;

namespace Kanebernetes.Stack.Kubernetes.CertManager
{
    public class CertManagerClusterIssuerAcmeSecretArgs : ResourceArgs
    {
        [Input("name", true)]
        public Input<string> Name { get; set; }
    }
}