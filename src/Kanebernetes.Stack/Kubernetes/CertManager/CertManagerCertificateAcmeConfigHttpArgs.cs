using Pulumi;

namespace Kanebernetes.Stack.Kubernetes.CertManager
{
    public class CertManagerCertificateAcmeConfigHttpArgs : ResourceArgs
    {
        [Input("ingressClass", true)]
        public Input<string> IngressClass { get; set; }
    }
}