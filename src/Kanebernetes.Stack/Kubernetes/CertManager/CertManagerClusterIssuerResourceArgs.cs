﻿using Pulumi;
using Pulumi.Kubernetes.ApiExtensions;

namespace Kanebernetes.Stack.Kubernetes.CertManager
{
    public class CertManagerClusterIssuerResourceArgs : CustomResourceArgs
    {
        public CertManagerClusterIssuerResourceArgs() : base("certmanager.k8s.io/v1alpha1", "ClusterIssuer")
        {
        }

        [Input("spec", true)]
        public Input<CertManagerClusterIssuerSpecArgs> Spec { get; set; }
    }
}