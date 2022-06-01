## EventStore helm chart

This is a helm chart which deploys a secure `eventstore` cluster in kubernetes.

Use the following commands to install

```shell
helm repo add es-kubernetes https://jeremy-morren.github.io/es-kubernetes/
helm install esdb es-kubernetes/eventstore --namespace esdb --create-namespace -f values.yaml
```

> It may take a short while to install, due to the need to create an EventStore CA certificate. Please be patient.

The chart will create a LoadBalancer service for ingress, so you will need to point the domain names at the relevant IP/DNS.

A sample `values.yaml` is shown below (
The full list of values can be seen [here](/chart/values.yaml)).

```yaml
eventStore:
  clusterSize: 3

proxy:
  hosts:
    '0': 'a.esdb.example.com'
    '1': 'b.esdb.example.com'
    '2': 'c.esdb.example.com'
    
  service:
    #Where acme HTTP challenges are forwarded (via 307)
    acmeHost: https://ingress.example.com
    anotations:
      dns-name: esdb-ingress.example.com
 
eventstore:
  storage:
    data:
      class: storage-premium
    logs:
      class: storage-premium

#Set up automatic TLS certificates with ACME protocol. See cert-manager.io
certificate:
  create: true #NB: Requires the cert-manager.io CRDs
  issuer:
    name: letsencrypt
    type: ClusterIssuer
```

You will now be able to connect using connection string `esdb://admin:changeit@a.esdb.example.com:443,b.esdb.example.com:443,c.esdb.example.com:443?tls=true`. 

> Note that the `:443` is required since the ESDB client assumes port `2113` if not specified.


### Proxy
The chart makes use of a [reverse proxy](/EventStoreProxy) built with YARP.  This enables using arbitrary server TLS certificate (enabling automatic Let's Encrypt server certificates). The EventStore cluster itself still uses self-signed certificates internally.

The primary motivations are as follows:
- Eventstore permits anonymous access to certain endpoints such as `/ping`, `/gossip` etc.  It is undesirable to expose these publicly over the internet.
- EventStore requires all certificates to have the same common name. This is problematic when using automatic Lets Encrypt certificate with multiple domain names.

#### License
This project is licensed under the [MIT license](/LICENSE).