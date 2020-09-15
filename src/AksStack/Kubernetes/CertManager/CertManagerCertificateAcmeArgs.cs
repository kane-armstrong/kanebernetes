using Pulumi;

namespace AksStack.Kubernetes.CertManager
{
    public class CertManagerCertificateAcmeArgs : ResourceArgs
    {
        [Input("config", true)]
        public InputList<CertManagerCertificateAcmeConfigArgs> Config { get; set; }
    }
}