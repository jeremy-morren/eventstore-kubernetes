param(
    [Parameter(Mandatory)][string]$Repository,
    [string]$Name = "eventstore-proxy",
    [string]$Tag = "latest"
)

$name = "$Repository/${name}:$Tag"

docker build -t $name (Join-Path $PSScriptRoot "EventStoreProxy")
docker push $name
