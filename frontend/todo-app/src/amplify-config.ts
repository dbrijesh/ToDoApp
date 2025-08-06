export const amplifyConfig = {
  Auth: {
    Cognito: {
      userPoolId: 'us-east-1_aibygKCIA',
      userPoolClientId: '6iv9saf42n4aft5pverjpjpaq6',
      region: 'us-east-1',
      loginWith: {
        oauth: {
          domain: 'us-east-1aibygkcia.auth.us-east-1.amazoncognito.com',
          scopes: ['openid'],
          redirectSignIn: ['http://localhost:4200/'],
          redirectSignOut: ['http://localhost:4200/'],
          responseType: 'code' as const
        }
      }
    }
  }
};