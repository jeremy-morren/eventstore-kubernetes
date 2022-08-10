#!/usr/bin/env sh

if [ -d /certs/ca ]; then
  #Trust root eventstore certs
  cp /certs/ca/*.crt /usr/local/share/ca-certificates && update-ca-certificates
fi

dotnet EventStoreProxy.dll