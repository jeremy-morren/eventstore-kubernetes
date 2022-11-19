#!/bin/bash

#Generates the Helm package .tgz in charts/ dir

SCRIPT_DIR=$( cd -- "$( dirname -- "${BASH_SOURCE[0]}" )" &> /dev/null && pwd )

rm "$SCRIPT_DIR/index.yaml" 2> /dev/null

rm -rf "$SCRIPT_DIR/charts" 2> /dev/null

mkdir "$SCRIPT_DIR/charts" 

helm package "$SCRIPT_DIR/esdb" --destination "$SCRIPT_DIR/charts"

helm repo index "$SCRIPT_DIR/charts" --url https://jeremy-morren.github.io/eventstore-kubernetes/charts 

mv "$SCRIPT_DIR/charts/index.yaml" "$SCRIPT_DIR"
