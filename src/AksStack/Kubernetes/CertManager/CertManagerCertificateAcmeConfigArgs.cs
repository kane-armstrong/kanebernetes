using Pulumi;

namespace AksStack.Kubernetes.CertManager
{
    public class CertManagerCertificateAcmeConfigArgs : ResourceArgs
    {
        [Input("http01", true)]
        public Input<CertManagerCertificateAcmeConfigHttpArgs> Http { get; set; }

        [Input("domains", true)]
        public InputList<string> Domains { get; set; }
    }
}