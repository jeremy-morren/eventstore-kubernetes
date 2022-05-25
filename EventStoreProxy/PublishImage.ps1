param(
    [Parameter(Mandatory)][string]$Repository,
    [string]$Name = "eventstore-proxy",
    [string]$Tag = "latest"
)

$name = "$Repository/${name}:$Tag"

docker build -t $name $PSScriptRoot
docker push $name
