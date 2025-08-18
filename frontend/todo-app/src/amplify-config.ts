export const clientIdConfig = {
  AzureSAML: '6iv9saf42n4aft5pverjpjpaq6', // Azure-specific client ID
  'NAM-SAML': 'nam_client_id_placeholder',   // NAM-specific client ID
  default: '6iv9saf42n4aft5pverjpjpaq6'      // Fallback client ID
};

export const amplifyConfig = {
  Auth: {
    Cognito: {
      userPoolId: 'us-east-1_aibygKCIA',
      userPoolClientId: clientIdConfig.AzureSAML, // Use Azure client ID for Amplify (since Azure uses Amplify's built-in flow)
      region: 'us-east-1',
      loginWith: {
        oauth: {
          domain: 'us-east-1aibygkcia.auth.us-east-1.amazoncognito.com',
          scopes: ['openid'],
          redirectSignIn: ['http://localhost:4200/'],
          redirectSignOut: ['http://localhost:4200/'],
          responseType: 'code' as const,
          providers: [
            { custom: 'AzureSAML' },
            { custom: 'NAM-SAML' }
          ]
        }
      }
    }
  }
};

export function getClientIdForProvider(provider?: string): string {
  if (!provider) return clientIdConfig.default;
  return clientIdConfig[provider as keyof typeof clientIdConfig] || clientIdConfig.default;
}

export function detectProviderFromToken(idToken?: string): string {
  if (!idToken) return 'default';
  
  try {
    const payload = JSON.parse(atob(idToken.split('.')[1]));
    const identityProvider = payload['custom:identity_provider'] || payload.identities?.[0]?.providerName;
    
    // Map provider names to our configuration keys
    if (identityProvider?.includes('Azure') || identityProvider?.includes('azure')) {
      return 'AzureSAML';
    } else if (identityProvider?.includes('NAM') || identityProvider?.includes('nam')) {
      return 'NAM-SAML';
    }
    
    // Fallback: check issuer or other token claims
    const iss = payload.iss;
    if (iss?.includes('nam') || iss?.includes('webdev.bank.com')) {
      return 'NAM-SAML';
    }
    
    return 'default';
  } catch (error) {
    console.warn('Could not parse ID token to detect provider:', error);
    return 'default';
  }
}
