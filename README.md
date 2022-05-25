## EventStore helm chart

This is a helm chart which deploys a secure `eventstore` cluster in kubernetes.

Use the following commands to install

```shell
helm repo add es-kubernetes https://jeremy-morren.github.io/es-kubernetes/
helm install esdb es-kubernetes --namespace esdb --create-namespace -f values.yaml
```

> It may take a while to complete, due to the need to create an EventStore CA certificate. Please be patient.

Before installing, you need an ingress controller which supports TLS passthrough.  A basic `values.yaml` should look as follows:

```yaml
clusterSize: 3

proxy:
  hosts:
    '0': 'a.esdb.domain.com'
    '1': 'b.esdb.domain.com'
    '2': 'c.esdb.domain.com'
  ingress:
    annotations:
      cert-manager.io/cluster-issuer: letsencrypt
      kubernetes.io/ingress.class: haproxy
 
eventstore:
  storage:
    data:
      class: storage-premium
    logs:
      class: storage-premium
```

The full list of values can be seen [here.](/chart/values.yaml)

### Proxy
The chart makes use of a [reverse proxy](/EventStoreProxy) built with YARP with the following features:
- Using arbitrary server TLS certificate (enabling automatic Let's Encrypt server certificates). The EventStore cluster itself still uses self-signed certificates internally.
- Certificate authentication (using ES-TrustedAuth)
- CLI for managing certificates

The primary motivations are as follows:
- Eventstore permits anonymous access to certain endpoints such as `/info`, `/gossip` etc.  It is undesirable to expose these publicly over the internet.
- Certificate authentication is easier to add to `appsettings.json` (only a path needed), so that passwords don't need to be in the configuration directly.

To create/use certificates, exec into the `proxy` container on any node and use the `client-certs` utility.

> :bangbang: The eventstore process must bind to `0.0.0.0:2113`, meaning that any application **inside** the cluster can access directly using port `2113` and `ES-TrustedAuth` header. The chart includes network policies that defend against this, however your cluster must support it.

#### License
This project is licensed under the [MIT license](/LICENSE).