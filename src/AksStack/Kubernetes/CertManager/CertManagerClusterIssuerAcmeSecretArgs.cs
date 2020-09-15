using Pulumi;

namespace AksStack.Kubernetes.CertManager
{
    public class CertManagerClusterIssuerAcmeSecretArgs : ResourceArgs
    {
        [Input("name", true)]
        public Input<string> Name { get; set; }
    }
}