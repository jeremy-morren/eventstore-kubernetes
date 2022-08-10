param(
    [Parameter(Mandatory)][string]$Repository,
    [string]$Name = "eventstoreproxy",
    [string]$Tag = "latest"
)

$name = "$Repository/${name}:$Tag"

docker build -t $name $PSScriptRoot
docker push $name
