## EventStore helm chart

This is a helm chart which deploys a secure `eventstore` cluster in kubernetes and exposes the nodes via an `Ingress` resource.

### Getting started

```shell
helm repo add eventstore https://jeremy-morren.github.io/eventstore-kubernetes/
helm install esdb eventstore/eventstore --namespace esdb --create-namespace -f values.yaml
```

> :bangbang: Huge gotcha: If you use 'eventstore' as the namespace or release name, it will result in environment variables thus prefixed, causing eventstore configuration to go wild.

A sample `values.yaml` is shown below (the full list of values can be seen [here](es-kubernetes/values.yaml)).

```yaml
ingress:
  class: nginx
  annotations:
    nginx.ingress.kubernetes.io/backend-protocol: "GRPC"

deployment:
  hosts:
    '0': a.esdb.example.com
    '1': a.esdb.example.com
    '2': a.esdb.example.com

  eventstore:
    storage:
      class: premium
      size: 10Gi

    config:
      shared:
        EnableAtomPubOverHTTP: 'true'

```

You will now be able to connect using connection string `esdb://admin:changeit@a.esdb.example.com:443,b.esdb.example.com:443,c.esdb.example.com:443?tls=true`. 

> Note that the `:443` is required since the ESDB client assumes port `2113` if not specified.


### Proxy
The chart makes use of a [reverse proxy](/EventStoreProxy) built with YARP (available at [jeremysv/eventstoreproxy](https://hub.docker.com/repository/docker/jeremysv/eventstoreproxy)).  It is exposed behind an ingress resource, thus allowing HTTP connections.
The primary motivations are as follows:
- Eventstore permits anonymous access to certain endpoints such as `/stats`, `/gossip` etc (see [Docs - New Users](https://developers.eventstore.com/server/v20.10/security.html#new-users).  It is undesirable to expose these publicly over the internet. As such, the proxy requires `Basic` auth on **all** endpoints except `/info`, `/ping` and `/web` (which are required for clients to work correctly).
- EventStore requires all certificates to have the same common name. This is problematic when using automatic Lets Encrypt certificate with multiple domain names.
- EventStore exposes HTTPS, which doesn't play that well inside a kubernetes cluster.

The proxy runs as a sidecar container and provides the following features:
- Exposes an `HTTP2` endpoint (non-SSL) for use by the ingress controller
- Enables CORS for all public URLs

Because EventStore uses gRPC, the ingress controller must be configured to use `HTTP2` as the backend protocol. E.g. for nginx, this requires the `nginx.ingress.kubernetes.io/backend-protocol: "GRPC"` annotation.

#### License
This project is licensed under the [MIT license](/LICENSE).