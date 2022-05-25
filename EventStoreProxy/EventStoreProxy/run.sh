#!/usr/bin/env sh

cp /certs/ca/*.crt /usr/local/share/ca-certificates && update-ca-certificates

#Generate fake certificate if it does not exist at /certs
if [ ! -f /certs/public/tls.crt ]; then 
  openssl req -x509 -newkey rsa:2048 \
      -subj "/CN=Fake EventStoreDB certificate" \
      -addext "subjectAltName=DNS:localhost" \
      -addext "basicConstraints=critical,CA:true,pathlen:1" \
      -addext "extendedKeyUsage=critical,clientAuth,serverAuth" \
      -nodes -days 365 -out /certs/tls.crt -keyout /certs/tls.key
  #Trust it so that healthchecks do not fail  
  cp /certs/tls.crt /usr/local/share/ca-certificates && update-ca-certificates
else
  cp /certs/public/* /certs
fi

dotnet EventStoreProxy.dll