# Kubernetes Custom Rresource Definition (CRD) Demo

This repo contains code for demonstrating the use of Custom Resource Dictionaries (CRD). It is the foundation for a blog post published at https://fearofoblivion.com/intro-to-kubernetes-custom-resource-definitions-or-crds.

## Content

In the __/yaml__ directory, you will find the YAML-files required to set up a CRD called Foo, as well as a couple of Foo instances. On top of that, there is also a YAML-file that contains the spec needed to run the sample in a Kubernetes cluster.

In the __/src/CrdController__ you will find .NET Core source code for a Kubernetens controller responsible for managing the Foo resources in the cluster. It is built as an ASP.NET Core application with a HostedService to do the monitoring. It also allows you to list the currently managed Foo resources in the cluster by browsing to the application. 

__Note:__ The controller is running with multiple replicas, and browsing to it will only work for the leader instance as the list is just an in-memory list.

## Leader selection

Monitoring the cluster for Foo resources should only be done by a single instance of the controller. However, for availability, the solution uses 2 replicas of the controller Pod. This means that the system needs to make sure that only one controller at the time is responsible for doing the actual work, while the second one sits idle waiting to step up in case the "active" controller stops working for some reason.

To solve this, the Pod includes a sidecar container for leader selection, called fredrikjanssonse/leader-elector:0.6. When a controller Pod comes online, it asks the leader-elector Pod if it is the current leader. if it is, it starts monitoring the cluster for Foo resources. if not, it sets up a timer to poll the leader-elector once every 10 seconds to see if the status has changed. If it changes, the newly elected leader starts monitoring the Foo resources.

__Note:__ The leader-elector image is supposed to be k8s.gcr.io/leader-elector, however at the moment that seems to have a bug that causes it to fail. This is why I am using a less official version from Docker Hub.

## Running the application

The first step in running this demo, is to add the Foo CRD to your cluster. This is easily done by running

```bash
kubectl apply -f ./yaml/foo-crd.yaml
```

This will add the CRD to your selected cluster. Once you have the CRD, you can create a basic Foo resource by running

```bash
kubectl apply -f ./yaml/a-foo.yaml
```

This will add you first Foo resource to your cluster. However, without a controller, this will do very little.

### Debugging/running locally

If you want to run the controller locally using for example Visual Studio to step through the code, you can do so by just starting the CrdController project. This will automatically figure out that it is not running inside a cluster, and instead try to talkj to the API using port 8001 on localhost. However, this does require you to run the Kubernetes API proxy before starting it. So remember to run the following command first

```bash
kubectl proxy
```

### Running in a Kubernetes cluster

To run the application inside your cluster, you need to generate a Docker image that can be used by Kubernetes. This can be done by running

```bash
docker -t <register/repo>/crd-demo:<version> ./src/CrdController
```

__Note:__ Remember to replace the _register/repo_ part with the path to your Docker registry.

Once you have the image, you need to push it to your repo using

```bash
docker push <register/repo>/crd-demo:<version>
```

With the image in place, you need to update the /yaml/foo-controller-deployment.yaml. There is a section in the deployment spec that needs to be updated to include the name of your repo

```
...
spec:
  containers:
  - name: foo-controller
    image: < IMAGE REPO >/crd-demo
    ports:
    - containerPort: 80
...
```

Just replace the __< IMAGE REPO >__ part with the address to your repo.

__Note:__ If you are using a private repo, don't forget to add an imagePullSecrets field

Once that is done, you can start the solution by applying the file to Kubernetes

```bash
kubectl apply -f ./yaml/foo-controller-deployment.yml
```

As soon as the controller pods are up and running, you can look at the logs to see what is happening. One of the pods should have log statements saying that it is the leader, and that it found a Foo called a-foo.

__Note:__ Just remeber, as there are 2 containers in the pod, you need to define which container you want to see the logs for by using `kubectl logs pod/foo-controller foo-controller`

If you want to make sure that the controller is really managing the Foo resources properly, you can try to add a second Foo by running

```bash
kubectl apply -f ./yaml/b-foo.yaml
```

and then check the logs to see that the change has been picked up.

__Tip:__ If you want to verify that the leader election is working, just locate the current leader and kill that pod. Another pod should automatically step up and take the leader role.
