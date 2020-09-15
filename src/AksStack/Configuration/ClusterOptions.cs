namespace Kanebernetes.Stack.Configuration
{
    public class ClusterOptions
    {
        public string Domain { get; set; }
        public string Namespace { get; set; }
        public string CertificateIssuerAcmeEmail { get; set; }
    }
}