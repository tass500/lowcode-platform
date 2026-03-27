{{- define "lowcode-platform.name" -}}
{{- default .Chart.Name .Values.nameOverride | trunc 63 | trimSuffix "-" }}
{{- end }}

{{- define "lowcode-platform.fullname" -}}
{{- if .Values.fullnameOverride }}
{{- .Values.fullnameOverride | trunc 63 | trimSuffix "-" }}
{{- else }}
{{- printf "%s-%s" .Release.Name .Chart.Name | trunc 63 | trimSuffix "-" }}
{{- end }}
{{- end }}

{{- define "lowcode-platform.backend.fullname" -}}
{{- printf "%s-backend" (include "lowcode-platform.fullname" .) }}
{{- end }}

{{- define "lowcode-platform.frontend.fullname" -}}
{{- printf "%s-frontend" (include "lowcode-platform.fullname" .) }}
{{- end }}

{{- define "lowcode-platform.chart" -}}
{{- printf "%s-%s" .Chart.Name .Chart.Version | replace "+" "_" }}
{{- end }}

{{/*
Kubernetes Secret name for backend sensitive env (JWT + optional SQL tenant connection).
*/}}
{{- define "lowcode-platform.backend.secretName" -}}
{{- if .Values.backend.secrets.existingSecret }}
{{- .Values.backend.secrets.existingSecret }}
{{- else if .Values.backend.secrets.create }}
{{- printf "%s-secret" (include "lowcode-platform.backend.fullname" .) }}
{{- else }}
{{- "" }}
{{- end }}
{{- end }}

{{- define "lowcode-platform.backend.pvcName" -}}
{{- printf "%s-data" (include "lowcode-platform.backend.fullname" .) }}
{{- end }}
