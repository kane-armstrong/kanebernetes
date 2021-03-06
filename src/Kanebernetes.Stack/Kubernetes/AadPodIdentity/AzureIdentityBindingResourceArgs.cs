﻿using Pulumi;
using Pulumi.Kubernetes.ApiExtensions;

namespace Kanebernetes.Stack.Kubernetes.AadPodIdentity
{
    public class AzureIdentityBindingResourceArgs : CustomResourceArgs
    {
        public AzureIdentityBindingResourceArgs() : base("aadpodidentity.k8s.io/v1", "AzureIdentityBinding")
        {
        }

        [Input("spec", true)]
        public Input<AzureIdentityBindingSpecArgs> Spec { get; set; }
    }
}