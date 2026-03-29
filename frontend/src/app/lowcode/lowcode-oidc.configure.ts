import { OAuthService } from 'angular-oauth2-oidc';
import type { SpaOidcConfig } from './lowcode-oidc.types';

export function spaOidcRedirectUri(cfg: SpaOidcConfig): string {
  const path = cfg.redirectPath.startsWith('/') ? cfg.redirectPath : `/${cfg.redirectPath}`;
  return `${window.location.origin}${path}`;
}

export function configureOAuthForSpa(oauth: OAuthService, cfg: SpaOidcConfig): void {
  oauth.configure({
    issuer: cfg.authority,
    clientId: cfg.clientId,
    redirectUri: spaOidcRedirectUri(cfg),
    responseType: 'code',
    scope: cfg.scope,
    oidc: true,
    requestAccessToken: true,
    strictDiscoveryDocumentValidation: false,
  });
}
