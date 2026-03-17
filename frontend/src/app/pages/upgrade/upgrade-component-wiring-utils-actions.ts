import { HttpHeaders } from '@angular/common/http';

import { requestOptions, requestOptionsWithResponse } from './upgrade-api';

import { formatError } from './upgrade-errors';

import {
  parseDateUtc,
  serverNowDate,
  serverNowMs,
} from './upgrade-time-utils';

import { toAbsoluteUrl } from './upgrade-url-builders';

import { buildClientTraceHeaderArg } from './upgrade-curl-snippets';

export function clientTraceHeaderArgImpl(clientTraceId: string): string {
  return buildClientTraceHeaderArg(clientTraceId);
}

export function requestOptionsImpl(clientTraceId: string): { headers: HttpHeaders } | undefined {
  return requestOptions(clientTraceId);
}

export function requestOptionsWithResponseImpl(clientTraceId: string): { headers?: HttpHeaders; observe: 'response' } {
  return requestOptionsWithResponse(clientTraceId);
}

export function toAbsoluteUrlImpl(url: string): string {
  return toAbsoluteUrl(url);
}

export function formatErrorImpl(e: any, fallback: string): string {
  return formatError(e, fallback);
}

export function parseDateUtcImpl(v: string | null | undefined): Date | null {
  return parseDateUtc(v);
}

export function serverNowMsImpl(nowTick: number, serverNowOffsetMs: number): number {
  return serverNowMs(nowTick, serverNowOffsetMs);
}

export function serverNowDateImpl(nowTick: number, serverNowOffsetMs: number): Date {
  return serverNowDate(nowTick, serverNowOffsetMs);
}
