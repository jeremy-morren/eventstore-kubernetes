{{- define "backup.config" -}}

EventStore:
  Namespace: {{ .Release.Namespace }}
  Pods:
    {{ range $i, $e := until ($.Values.deployment.clusterSize | int) }}
    '{{ (index $.Values.deployment.hosts ($i | toString)) }}': '{{ include "esdb.fullname" $ }}-{{ $i }}'
    {{ end }}

Backup:
  DataDirectory: /data/db
  TempDirectory: /tmp

Kestrel:
  Endpoints:
    Https:
      Url: 'https://+:443/'
      Certificate:
        Path: /certs/backup.crt
        KeyPath: /certs/backup.key

{{- end }}