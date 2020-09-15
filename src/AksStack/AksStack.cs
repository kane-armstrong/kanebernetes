using AksStack.Configuration;
using AksStack.Kubernetes.CertManager;
using Pulumi;
using Pulumi.Azure.Authorization;
using Pulumi.Azure.ContainerService;
using Pulumi.Azure.ContainerService.Inputs;
using Pulumi.Azure.Core;
using Pulumi.Azure.Network;
using Pulumi.Azure.OperationalInsights;
using Pulumi.Azure.OperationalInsights.Inputs;
using Pulumi.AzureAD;
using Pulumi.Kubernetes.Core.V1;
using Pulumi.Kubernetes.Types.Inputs.Core.V1;
using Pulumi.Kubernetes.Types.Inputs.Meta.V1;
using Pulumi.Kubernetes.Yaml;
using Pulumi.Random;
using Pulumi.Tls;
using Application = Pulumi.AzureAD.Application;
using ApplicationArgs = Pulumi.AzureAD.ApplicationArgs;
using Config = Pulumi.Config;
using CustomResource = Pulumi.Kubernetes.ApiExtensions.CustomResource;
using Provider = Pulumi.Kubernetes.Provider;
using ProviderArgs = Pulumi.Kubernetes.ProviderArgs;
using VirtualNetwork = Pulumi.Azure.Network.VirtualNetwork;
using VirtualNetworkArgs = Pulumi.Azure.Network.VirtualNetworkArgs;
// ReSharper disable UnusedVariable

namespace AksStack
{
    public class AksStack : Stack
    {
        public AksStack()
        {
            var config = new Config();

            var azureResources = CreateBaseAzureInfrastructure(config);

            var clusterOptions = new ClusterOptions
            {
                Domain = config.Require("domain"),
                Namespace = config.Require("kubernetes-namespace"),
                CertificateIssuerAcmeEmail = config.Require("certmanager-acme-email")
            };

            ConfigureKubernetesCluster(azureResources, clusterOptions);
        }

        private static AzureResourceBag CreateBaseAzureInfrastructure(Config config)
        {
            var location = config.Require("azure-location");

            var environment = config.Require("azure-tags-environment");
            var owner = config.Require("azure-tags-owner");
            var createdBy = config.Require("azure-tags-createdby");

            var kubernetesVersion = config.Require("kubernetes-version");
            var kubernetesNodeCount = config.RequireInt32("kubernetes-scaling-nodecount");

            var sqlUser = config.RequireSecret("azure-sqlserver-username");
            var sqlPassword = config.RequireSecret("azure-sqlserver-password");

            var resourceGroup = new ResourceGroup("kanebernetes-rg", new ResourceGroupArgs
            {
                Name = "kanebernetes",
                Location = location
            });

            var vnet = new VirtualNetwork("kanebernetes-vnet", new VirtualNetworkArgs
            {
                ResourceGroupName = resourceGroup.Name,
                Name = "kanebernetes",
                AddressSpaces = { "10.0.0.0/8" }
            });

            var subnet = new Subnet("kanebernetes-subnet", new SubnetArgs
            {
                ResourceGroupName = resourceGroup.Name,
                Name = "kanebernetes",
                AddressPrefixes = { "10.240.0.0/16" },
                VirtualNetworkName = vnet.Name,
                ServiceEndpoints = new InputList<string> { "Microsoft.KeyVault", "Microsoft.Sql" }
            });

            var registry = new Registry("containers", new RegistryArgs
            {
                ResourceGroupName = resourceGroup.Name,
                Name = "containers",
                Sku = "Standard",
                AdminEnabled = true
            });

            var aksServicePrincipalPassword = new RandomPassword("kanebernetes-sp-password", new RandomPasswordArgs
            {
                Length = 20,
                Special = true,
            }).Result;

            var clusterAdApp = new Application("kanebernetes-app", new ApplicationArgs
            {
                Name = "kanebernetes"
            });

            var clusterAdServicePrincipal = new ServicePrincipal("kanebernetes-sp", new ServicePrincipalArgs
            {
                ApplicationId = clusterAdApp.ApplicationId
            });

            var clusterAdServicePrincipalPassword = new ServicePrincipalPassword("kanebernetes-sp-pwd", new ServicePrincipalPasswordArgs
            {
                ServicePrincipalId = clusterAdServicePrincipal.ObjectId,
                EndDate = "2099-01-01T00:00:00Z",
                Value = aksServicePrincipalPassword
            });

            // Grant networking permissions to the SP (needed e.g. to provision Load Balancers)
            var subnetAssignment = new Assignment("kanebernetes-subnet-assignment", new AssignmentArgs
            {
                PrincipalId = clusterAdServicePrincipal.Id,
                RoleDefinitionName = "Network Contributor",
                Scope = subnet.Id
            });

            var acrAssignment = new Assignment("kanebernetes-acr-assignment", new AssignmentArgs
            {
                PrincipalId = clusterAdServicePrincipal.Id,
                RoleDefinitionName = "AcrPull",
                Scope = registry.Id
            });

            var logAnalyticsWorkspace = new AnalyticsWorkspace("kanebernetes-analytics-ws", new AnalyticsWorkspaceArgs
            {
                ResourceGroupName = resourceGroup.Name,
                Name = "kanebernetesloganalytics",
                Sku = "PerGB2018"
            });

            var logAnalyticsSolution = new AnalyticsSolution("kanebernetes-analytics-sln", new AnalyticsSolutionArgs
            {
                ResourceGroupName = resourceGroup.Name,
                SolutionName = "ContainerInsights",
                WorkspaceName = logAnalyticsWorkspace.Name,
                WorkspaceResourceId = logAnalyticsWorkspace.Id,
                Plan = new AnalyticsSolutionPlanArgs
                {
                    Product = "OMSGallery/ContainerInsights",
                    Publisher = "Microsoft"
                }
            });

            var sshPublicKey = new PrivateKey("ssh-key", new PrivateKeyArgs
            {
                Algorithm = "RSA",
                RsaBits = 4096,
            });

            var cluster = new KubernetesCluster("kanebernetes-aks", new KubernetesClusterArgs
            {
                ResourceGroupName = resourceGroup.Name,
                Name = "kanebernetes",
                DnsPrefix = "dns",
                KubernetesVersion = kubernetesVersion,
                DefaultNodePool = new KubernetesClusterDefaultNodePoolArgs
                {
                    Name = "aksagentpool",
                    NodeCount = kubernetesNodeCount,
                    VmSize = "Standard_D2_v2",
                    OsDiskSizeGb = 30,
                    VnetSubnetId = subnet.Id
                },
                LinuxProfile = new KubernetesClusterLinuxProfileArgs
                {
                    AdminUsername = "aksuser",
                    SshKey = new KubernetesClusterLinuxProfileSshKeyArgs
                    {
                        KeyData = sshPublicKey.PublicKeyOpenssh
                    }
                },
                ServicePrincipal = new KubernetesClusterServicePrincipalArgs
                {
                    ClientId = clusterAdApp.ApplicationId,
                    ClientSecret = clusterAdServicePrincipalPassword.Value
                },
                RoleBasedAccessControl = new KubernetesClusterRoleBasedAccessControlArgs
                {
                    Enabled = true
                },
                NetworkProfile = new KubernetesClusterNetworkProfileArgs
                {
                    NetworkPlugin = "azure",
                    ServiceCidr = "10.2.0.0/24",
                    DnsServiceIp = "10.2.0.10",
                    DockerBridgeCidr = "172.17.0.1/16"
                },
                AddonProfile = new KubernetesClusterAddonProfileArgs
                {
                    OmsAgent = new KubernetesClusterAddonProfileOmsAgentArgs
                    {
                        Enabled = true,
                        LogAnalyticsWorkspaceId = logAnalyticsWorkspace.Id
                    }
                }
            });

            // TODO output subnet id so that we can make sqlvnetrules for sql instances
            // TODO output AKS cluster config
            // TODO output registry too

            var provider = new Provider("pet-doctor-aks-provider", new ProviderArgs
            {
                KubeConfig = cluster.KubeConfigRaw
            });

            return new AzureResourceBag
            {
                ResourceGroup = resourceGroup,
                Cluster = cluster,
                ClusterProvider = provider,
                AksServicePrincipal = clusterAdServicePrincipal
            };
        }

        private static void ConfigureKubernetesCluster(AzureResourceBag azureResources, ClusterOptions clusterOptions)
        {
            var componentOpts = new ComponentResourceOptions
            {
                DependsOn = azureResources.Cluster,
                Provider = azureResources.ClusterProvider
            };

            var customOpts = new CustomResourceOptions
            {
                DependsOn = azureResources.Cluster,
                Provider = azureResources.ClusterProvider
            };

            var aadPodIdentityDeployment = new ConfigFile("k8s-aad-pod-identity", new ConfigFileArgs
            {
                File = "https://raw.githubusercontent.com/Azure/aad-pod-identity/master/deploy/infra/deployment-rbac.yaml"
            }, componentOpts);

            var certManagerDeployment = new ConfigFile("k8s-cert-manager", new ConfigFileArgs
            {
                File = "https://raw.githubusercontent.com/jetstack/cert-manager/release-0.8/deploy/manifests/00-crds.yaml"
            }, componentOpts);

            var nginxDeployment = new ConfigFile("k8s-nginx-ingress", new ConfigFileArgs
            {
                File =
                    "https://raw.githubusercontent.com/kubernetes/ingress-nginx/controller-v0.34.1/deploy/static/provider/cloud/deploy.yaml"
            }, componentOpts);

            var clusterNamespace = new Namespace("k8s-namespace", new NamespaceArgs
            {
                Metadata = new ObjectMetaArgs
                {
                    Name = clusterOptions.Namespace
                }
            }, customOpts);

            var clusterIssuer = new CustomResource("k8s-cert-manager-cluster-issuer", new CertManagerClusterIssuerResourceArgs
            {
                Metadata = new ObjectMetaArgs
                {
                    Name = "letsencrypt-prod"
                },
                Spec = new CertManagerClusterIssuerSpecArgs
                {
                    Acme = new CertManagerClusterIssuerAcmeArgs
                    {
                        Email = clusterOptions.CertificateIssuerAcmeEmail,
                        Server = "https://acme-v02.api.letsencrypt.org/directory",
                        PrivateKeySecretRef = new CertManagerClusterIssuerAcmeSecretArgs
                        {
                            Name = "letsencrypt-prod"
                        }
                    }
                }
            }, customOpts);

            var certs = new CustomResource("k8s-cert-manager-domain-cert", new CertManagerCertificateResourceArgs
            {
                Metadata = new ObjectMetaArgs
                {
                    Name = "tls-secret"
                },
                Spec = new CertManagerCertificateSpecArgs
                {
                    SecretName = "tls-secret",
                    DnsNames = clusterOptions.Domain,
                    Acme = new CertManagerCertificateAcmeArgs
                    {
                        Config = new CertManagerCertificateAcmeConfigArgs
                        {
                            Http = new CertManagerCertificateAcmeConfigHttpArgs
                            {
                                IngressClass = "nginx"
                            },
                            Domains = clusterOptions.Domain
                        }
                    },
                    IssuerRef = new CertManagerCertificateIssuerRefArgs
                    {
                        Name = "letsencrypt-prod",
                        Kind = "ClusterIssuer"
                    }
                }
            }, customOpts);
        }

        private class AzureResourceBag
        {
            public ResourceGroup ResourceGroup { get; set; }
            public KubernetesCluster Cluster { get; set; }
            public ServicePrincipal AksServicePrincipal { get; set; }
            public Provider ClusterProvider { get; set; }
        }
    }
}
