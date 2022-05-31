#!/usr/bin/env sh

if [ -d /certs/ca ]; then
  #Trust root eventstore certs
  cp /certs/ca/*.crt /usr/local/share/ca-certificates && update-ca-certificates
fi

#Certs are mounted at /certs/public

#Generate self-signed certificate if necessary
if [ ! -f /certs/public/tls.crt ]; then 
  openssl req -x509 -newkey rsa:2048 -out /tls/tls.crt -keyout /tls/tls.key \
    -subj "/CN=EventStoreDB Fake Certificate" -nodes
else
  cp /certs/public/* /tls
fi 
chmod 440 /tls/*

dotnet EventStoreProxy.dll