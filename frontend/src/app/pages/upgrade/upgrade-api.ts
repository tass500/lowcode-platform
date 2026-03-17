import { HttpClient, HttpHeaders, HttpResponse } from '@angular/common/http';

export function requestOptions(clientTraceId: string): { headers: HttpHeaders } | undefined {
  const t = (clientTraceId ?? '').trim();
  if (!t) return undefined;
  return { headers: new HttpHeaders({ 'X-Trace-Id': t }) };
}

export function requestOptionsWithResponse(clientTraceId: string): { headers?: HttpHeaders; observe: 'response' } {
  const base = requestOptions(clientTraceId);
  const headers = base?.headers;
  return headers ? { headers, observe: 'response' } : { observe: 'response' };
}

export function applyServerTimeFromBody(body: any, source: string, applyServerTimeUtc: (v: string | null | undefined, source: string) => void) {
  const anyBody: any = body as any;
  applyServerTimeUtc(anyBody?.serverTimeUtc, source);
}

export function applyServerTimeFromResponse<T>(res: HttpResponse<T>, source: string, applyServerTimeUtc: (v: string | null | undefined, source: string) => void) {
  const anyBody: any = (res?.body ?? null) as any;
  applyServerTimeUtc(anyBody?.serverTimeUtc, source);
}

export function httpGetWithServerTime<T>(args: {
  http: HttpClient;
  url: string;
  source: string;
  clientTraceId: string;
  applyServerTimeUtc: (v: string | null | undefined, source: string) => void;
  handlers: {
    next: (body: T) => void;
    error: (e: any) => void;
  };
}) {
  args.http.get<T>(args.url, requestOptions(args.clientTraceId)).subscribe({
    next: (body) => {
      args.handlers.next(body);
      applyServerTimeFromBody(body, args.source, args.applyServerTimeUtc);
    },
    error: args.handlers.error
  });
}

export function httpGetResponseWithServerTime<T>(args: {
  http: HttpClient;
  url: string;
  source: string;
  clientTraceId: string;
  applyServerTimeUtc: (v: string | null | undefined, source: string) => void;
  handlers: {
    next: (res: HttpResponse<T>) => void;
    error: (e: any) => void;
  };
}) {
  args.http.get<T>(args.url, requestOptionsWithResponse(args.clientTraceId)).subscribe({
    next: (res) => {
      args.handlers.next(res);
      applyServerTimeFromResponse(res, args.source, args.applyServerTimeUtc);
    },
    error: args.handlers.error
  });
}

export function httpPostWithServerTime<TRes, TBody>(args: {
  http: HttpClient;
  url: string;
  body: TBody;
  source: string;
  clientTraceId: string;
  applyServerTimeUtc: (v: string | null | undefined, source: string) => void;
  handlers: {
    next: (body: TRes) => void;
    error: (e: any) => void;
  };
}) {
  args.http.post<TRes>(args.url, args.body, requestOptions(args.clientTraceId)).subscribe({
    next: (resBody) => {
      args.handlers.next(resBody);
      applyServerTimeFromBody(resBody, args.source, args.applyServerTimeUtc);
    },
    error: args.handlers.error
  });
}
