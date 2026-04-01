import { HttpClient, provideHttpClient, withInterceptors } from '@angular/common/http';
import { APP_INITIALIZER, ApplicationConfig } from '@angular/core';
import { provideRouter } from '@angular/router';
import { provideOAuthClient } from 'angular-oauth2-oidc';

import { routes } from './app.routes';
import { apiAuthInterceptor } from './lowcode/api-auth.interceptor';
import { BffAuthStateService } from './lowcode/bff-auth-state.service';

export function bffMetaAppInitializer(bff: BffAuthStateService, http: HttpClient): () => Promise<void> {
  return () => bff.loadMeta(http);
}

export const appConfig: ApplicationConfig = {
  providers: [
    provideRouter(routes),
    provideHttpClient(withInterceptors([apiAuthInterceptor])),
    {
      provide: APP_INITIALIZER,
      useFactory: bffMetaAppInitializer,
      deps: [BffAuthStateService, HttpClient],
      multi: true,
    },
    provideOAuthClient(),
  ],
};
