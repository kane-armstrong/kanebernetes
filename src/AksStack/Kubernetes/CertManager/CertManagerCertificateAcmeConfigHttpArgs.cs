using Pulumi;

namespace AksStack.Kubernetes.CertManager
{
    public class CertManagerCertificateAcmeConfigHttpArgs : ResourceArgs
    {
        [Input("ingressClass", true)]
        public Input<string> IngressClass { get; set; }
    }
}