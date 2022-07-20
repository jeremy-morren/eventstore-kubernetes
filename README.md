## EventStore helm chart

This is a helm chart which deploys a secure `eventstore` cluster in kubernetes.

Use the following commands to install

```shell
helm repo add eventstore https://jeremy-morren.github.io/eventstore-kubernetes/
helm install esdb eventstore/eventstore --namespace esdb --create-namespace -f values.yaml
```

> :bangbang: Huge gotcha: If you use 'eventstore' as the namespace or name, it will result in environment variables thus prefixed, causing eventstore configuration to go wild.

A sample `values.yaml` is shown below (
The full list of values can be seen [here](es-kubernetes/values.yaml)).

```yaml
eventstore:
  clusterSize: 3

  storage:
    class: premium
    size: 10Gi

  config:
    shared:
      EnableAtomPubOverHTTP: 'false'
      
proxy:
  hosts:
    '0': 'a.esdb.example.com'
    '1': 'b.esdb.example.com'
    '2': 'c.esdb.example.com'
    
  ingress:
    annotations:
      'kubernetes.io/ingress.class': haproxy
 
```

You will now be able to connect using connection string `esdb://admin:changeit@a.esdb.example.com:443,b.esdb.example.com:443,c.esdb.example.com:443?tls=true`. 

> Note that the `:443` is required since the ESDB client assumes port `2113` if not specified.


### Proxy
The chart makes use of a [reverse proxy](/EventStoreProxy) built with YARP.  It is exposed behind an ingress resource, thus allowing HTTP connections.
The primary motivations are as follows:
- Eventstore permits anonymous access to certain endpoints such as `/stats`, `/gossip` etc (see [Docs - New Users](https://developers.eventstore.com/server/v20.10/security.html#new-users).  It is undesirable to expose these publicly over the internet. As such, the proxy requires `Basic` auth on **all** endpoints except `/info`, `/ping` and `/web`.
- EventStore requires all certificates to have the same common name. This is problematic when using automatic Lets Encrypt certificate with multiple domain names. This chart generates internal SSL certificates with the `es-gencert-cli` image that are used by the eventstore containers. The root CA cert is trusted by the proxy, whil

#### License
This project is licensed under the [MIT license](/LICENSE).