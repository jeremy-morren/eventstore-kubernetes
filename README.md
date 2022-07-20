## EventStore helm chart

This is a helm chart which deploys a secure `eventstore` cluster in kubernetes.

Use the following commands to install

```shell
helm repo add es-kubernetes https://jeremy-morren.github.io/es-kubernetes/
helm install esdb es-kubernetes/es-kubernetes --namespace esdb --create-namespace -f values.yaml
```

> It may take a short while to install, due to the need to create an EventStore CA certificate. Please be patient.

The chart will create a LoadBalancer service for ingress, so you will need to point the domain names at the relevant IP/DNS.

A sample `values.yaml` is shown below (
The full list of values can be seen [here](es-kubernetes/values.yaml)).

```yaml
eventStore:
  clusterSize: 3

proxy:
  #Note that strange errors will occur
  #if there are no 
  hosts:
    '0': 'a.esdb.example.com'
    '1': 'b.esdb.example.com'
    '2': 'c.esdb.example.com'
    
  #Ingress must support tls-passthrough
  ingress: {}
 
eventstore:
  storage:
    data:
      class: storage-premium
      size: 10Gi
    logs:
      class: storage-premium
      size: 10Gi
```

You will now be able to connect using connection string `esdb://admin:changeit@a.esdb.example.com:443,b.esdb.example.com:443,c.esdb.example.com:443?tls=true`. 

> Note that the `:443` is required since the ESDB client assumes port `2113` if not specified.


### Proxy
The chart makes use of a [reverse proxy](/EventStoreProxy) built with YARP.  It is exposed behind an ingress resource, thus allowing HTTP connections.
The primary motivations are as follows:
- Eventstore permits anonymous access to certain endpoints such as `/ping`, `/gossip` etc.  It is undesirable to expose these publicly over the internet. As such, the proxy requires `Basic` auth on **all** endpoints.
- EventStore requires all certificates to have the same common name. This is problematic when using automatic Lets Encrypt certificate with multiple domain names.

#### License
This project is licensed under the [MIT license](/LICENSE).