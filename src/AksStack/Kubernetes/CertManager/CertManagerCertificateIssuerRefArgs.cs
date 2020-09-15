using Pulumi;

namespace Kanebernetes.Stack.Kubernetes.CertManager
{
    public class CertManagerCertificateIssuerRefArgs : ResourceArgs
    {
        [Input("name", true)]
        public Input<string> Name { get; set; }

        [Input("kind", true)]
        public Input<string> Kind { get; set; }
    }
}