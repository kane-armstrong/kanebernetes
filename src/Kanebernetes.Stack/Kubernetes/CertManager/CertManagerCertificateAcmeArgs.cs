using Pulumi;

namespace Kanebernetes.Stack.Kubernetes.CertManager
{
    public class CertManagerCertificateAcmeArgs : ResourceArgs
    {
        [Input("config", true)]
        public InputList<CertManagerCertificateAcmeConfigArgs> Config { get; set; }
    }
}