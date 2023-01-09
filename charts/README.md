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
    nginx.ingress.kubernetes.io/backend-protocol: "GRPCS"

deployment:
  hosts:
    '0': a.esdb.example.com
    '1': b.esdb.example.com
    '2': c.esdb.example.com

  eventstore:
    storage:
      class: premium
      size: 10Gi

    config:
      shared:
        RunProjections: All
        EnableAtomPubOverHTTP: 'true'

```

You will now be able to connect using connection string `esdb://admin:changeit@a.esdb.example.com:443,b.esdb.example.com:443,c.esdb.example.com:443?tls=true`. 

> Note that the `:443` is required since the ESDB client assumes port `2113` if not specified.

#### License
This project is licensed under the [MIT license](/LICENSE).