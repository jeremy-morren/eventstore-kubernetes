#!/usr/bin/env bash

#ca-certs mounted at /certs/ca
#node-certs mounted at /certs/node
setup create-node \
  --ca-cert /certs/ca/ca.crt \
  --ca-key /certs/ca/ca.key \
  --out /certs/node \
  --dns-names "localhost,127.0.0.1,$HOSTNAME" \
  --ip-addresses "127.0.0.1,$POD_IP"
  
#Gen options
setup gen-options --cluster-size "$CLUSTER_SIZE" \
  --hostname "$HOSTNAME" \
  --gossip-port "$GOSSIP_PORT" \
  --pod-ip "$POD_IP" > node-options

echo "ExtHostAdvertiseAs: $HOSTNAME" >> node-options

conf=/config/node/eventstore.conf
#Configured options are at /config/static
#Combine all options files into a single YAML file
setup combine-options \
  node-options \
  "/config/static/cluster" \
  "/config/static/shared" \
  "/config/static/$HOSTNAME" > $conf

chmod 440 $conf

#Display options for debugging
echo
cat $conf