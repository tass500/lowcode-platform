import type { HttpResponse } from '@angular/common/http';

export function toPromiseFromHandlers<T>(register: (handlers: {
  next: (v: T) => void;
  error: (e: any) => void;
}) => void): Promise<T> {
  return new Promise<T>((resolve, reject) => {
    register({
      next: (v) => resolve(v),
      error: (e) => reject(e),
    });
  });
}

export function toPromiseFromResponseHandlers<T>(register: (handlers: {
  next: (res: HttpResponse<T>) => void;
  error: (e: any) => void;
}) => void): Promise<HttpResponse<T>> {
  return new Promise<HttpResponse<T>>((resolve, reject) => {
    register({
      next: (res) => resolve(res),
      error: (e) => reject(e),
    });
  });
}
