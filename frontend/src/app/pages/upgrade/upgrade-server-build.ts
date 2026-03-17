import { HttpResponse } from '@angular/common/http';

export type ServerBuildInfo = {
  capturedFrom: string;
  version: string;
  revision: string;
  environment: string;
};

export function tryExtractServerBuildFromHeaders(args: {
  res: HttpResponse<any>;
  capturedFrom: string;
}): ServerBuildInfo | null {
  const version = (args.res.headers.get('X-LCP-Server-Version') ?? '').trim();
  const revision = (args.res.headers.get('X-LCP-Server-Revision') ?? '').trim();
  const environment = (args.res.headers.get('X-LCP-Server-Environment') ?? '').trim();

  if (!version && !revision && !environment)
    return null;

  return {
    capturedFrom: args.capturedFrom,
    version: version || 'unknown',
    revision: revision || 'unknown',
    environment: environment || 'unknown',
  };
}

export function serverBuildHeaderLines(serverBuild: ServerBuildInfo): string[] {
  return [
    `X-LCP-Server-Version: ${serverBuild.version}`,
    `X-LCP-Server-Revision: ${serverBuild.revision}`,
    `X-LCP-Server-Environment: ${serverBuild.environment}`,
    `(capturedFrom=${serverBuild.capturedFrom})`,
  ];
}
