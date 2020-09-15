# Kanebernetes

Personal tool that enables quick spin up/tear down of an AKS cluster. Useful for getting a working cluster up so I can use it various side projects.

Steps to get this to work properly:

1. Open a terminal
2. cd to `PetDoctor.Infrastructure` 
3. Change config as appropriate (see `Pulumi.yaml` and `Pulumi.dev.yaml`)
4. Run `pulumi up`
5. If it falls over, running `pulumi up` again typically works
6. Run `kubectl --namespace ingress-nginx get services -o wide -w ingress-nginx-controller` and note down the value of `EXTERNAL-IP`
7. Create an A record pointing the domain specified in your `Pulumi.dev.yaml` file to the IP address obtained in the previous step
8. Give DNS a some time to propagate

Up:

`pulumi up`

Down:

`pulumi destroy`
