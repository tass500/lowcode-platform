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
