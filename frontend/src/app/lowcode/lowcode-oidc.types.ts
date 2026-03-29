export type SpaOidcConfig = {
  authority: string;
  clientId: string;
  scope: string;
  redirectPath: string;
  tenantClaimSources: string[];
};
