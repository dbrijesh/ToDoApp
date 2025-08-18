# Authentication Architecture: NAM & Azure Entra Integration with Cognito

## Overview

This document describes the authentication architecture for the TODO application, which supports dual identity providers (NAM SAML and Azure Entra SAML) through AWS Cognito User Pool federation.

## Architecture Diagram

```
┌─────────────────┐    ┌─────────────────┐
│   NAM Identity  │    │  Azure Entra ID │
│    Provider     │    │   (Azure AD)    │
│  (IDP-Initiated)│    │ (SP-Initiated)  │
└─────────┬───────┘    └─────────┬───────┘
          │                      │
          │ Direct SAML          │ SAML Request/Response
          │ Assertion            │ (via user browser)
          │                      │
          ▼                      ▼
┌─────────────────────────────────────────────┐
│           AWS Cognito User Pool             │
│                                             │
│  ┌─────────────────┐ ┌─────────────────────┐│
│  │   NAM-SAML      │ │    AzureSAML        ││
│  │   Provider      │ │    Provider         ││
│  │   Client ID: N  │ │    Client ID: A     ││
│  │  (IDP-Initiated)│ │  (SP-Initiated)     ││
│  └─────────────────┘ └─────────────────────┘│
│                                             │
│         ┌─────────────────────────┐         │
│         │    OAuth2 Endpoints    │         │
│         │  /oauth2/authorize      │         │
│         │  /oauth2/token          │         │
│         └─────────────────────────┘         │
└─────────────────┬───────────────────────────┘
                  │
                  │ OAuth2/OIDC Tokens
                  │ (Different Client IDs)
                  ▼
┌─────────────────────────────────────────────┐
│            Angular Frontend                 │
│  ┌─────────────────────────────────────────┐│
│  │         amplify-config.ts               ││
│  │  • Dual Client ID Configuration        ││
│  │  • Provider Detection Logic            ││
│  │  • Dynamic Token Management            ││
│  └─────────────────────────────────────────┘│
│  ┌─────────────────────────────────────────┐│
│  │         app.component.ts                ││
│  │  • SP-Initiated: signInWithRedirect    ││
│  │  • IDP-Initiated: Manual Token Exchange││
│  │  • Provider-Specific Token Storage     ││
│  └─────────────────────────────────────────┘│
└─────────────────┬───────────────────────────┘
                  │
                  │ Authenticated API Calls
                  │ (Bearer Token)
                  ▼
┌─────────────────────────────────────────────┐
│            Backend API                      │
│            (Express.js)                     │
│        • JWT Token Validation              │
│        • Cognito Public Key Verification   │
└─────────────────────────────────────────────┘
```

## Authentication Flows

### 1. Azure Entra Authentication Flow (SP-Initiated)

**Service Provider (Angular App) initiates the authentication flow:**

```
User → Angular App → Cognito → Azure Entra → Cognito → Angular App
  1. User clicks "Login with Azure" button in Angular app
  2. Angular calls signInWithRedirect({ provider: { custom: 'AzureSAML' } })
  3. Amplify redirects user to Cognito OAuth2 authorize endpoint:
     https://domain.auth.region.amazoncognito.com/oauth2/authorize?
     client_id=AZURE_CLIENT_ID&identity_provider=AzureSAML&...
  4. Cognito generates SAML AuthnRequest and redirects to Azure Entra
  5. User sees Azure login page and authenticates with corporate credentials
  6. Azure Entra validates user and sends SAML Response to Cognito ACS URL
  7. Cognito validates SAML assertion and creates user session
  8. Cognito redirects back to Angular app with OAuth2 authorization code
  9. Amplify automatically exchanges authorization code for JWT tokens
  10. Tokens stored with Azure client ID key in localStorage
  11. Angular app detects successful authentication and loads user profile
```

**Key Characteristics:**
- **SP-Initiated**: Angular app starts the authentication flow
- **Automatic Token Handling**: Amplify manages the complete OAuth2 flow
- **Client ID**: Uses Azure-specific client ID for token storage
- **Redirect Flow**: Standard OAuth2 authorization code flow

### 2. NAM SAML Authentication Flow (IDP-Initiated)

**Identity Provider (NAM) initiates the authentication flow:**

```
User → NAM Portal → Cognito → Angular App
  1. User accesses NAM SSO portal (https://access.webdev.bank.com)
  2. User sees available applications and clicks "TODO App" link
  3. NAM generates SAML assertion and initiates SSO to Cognito:
     POST to https://domain.auth.region.amazoncognito.com/saml2/idpresponse
  4. Cognito validates SAML assertion from NAM identity provider
  5. Cognito creates user session and redirects to Angular app with:
     - Authorization code in URL parameter
     - Optional state parameter for CSRF protection
  6. Angular app detects authorization code in URL and handles manually:
     - Identifies this as NAM flow (IDP-initiated)
     - Exchanges authorization code for JWT tokens using NAM client ID
     - Makes direct POST to /oauth2/token endpoint with NAM_CLIENT_ID
     - Parses user information from ID token (sub, email, custom claims)
     - Stores tokens with NAM-specific client ID key in localStorage
  7. Angular app completes authentication and loads user profile
```

**Key Characteristics:**
- **IDP-Initiated**: NAM starts the authentication flow from their portal
- **Manual Token Handling**: Angular manually exchanges authorization code
- **Client ID**: Uses NAM-specific client ID for token exchange and storage
- **Direct Portal Access**: Users access via NAM SSO portal, not Angular app
- **Custom Token Exchange**: Bypasses Amplify's automatic token handling

## Detailed Authentication Flow Diagrams

### Azure Entra SP-Initiated Flow

```
┌─────────────┐    ┌──────────────┐    ┌─────────────┐    ┌──────────────┐
│    User     │    │ Angular App  │    │   Cognito   │    │ Azure Entra  │
└──────┬──────┘    └──────┬───────┘    └──────┬──────┘    └──────┬───────┘
       │                  │                   │                  │
       │ 1. Click Login   │                   │                  │
       ├─────────────────►│                   │                  │
       │                  │                   │                  │
       │                  │ 2. signInWithRedirect               │
       │                  │    (AzureSAML)    │                  │
       │                  ├──────────────────►│                  │
       │                  │                   │                  │
       │                  │                   │ 3. SAML AuthnReq │
       │                  │                   ├─────────────────►│
       │                  │                   │                  │
       │ 4. Redirect to Azure Login Page      │                  │
       │◄─────────────────────────────────────┼─────────────────◄│
       │                  │                   │                  │
       │ 5. Enter Credentials                 │                  │
       ├─────────────────────────────────────────────────────────►│
       │                  │                   │                  │
       │                  │                   │ 6. SAML Response │
       │                  │                   │◄─────────────────┤
       │                  │                   │                  │
       │ 7. Redirect with Auth Code           │                  │
       │◄─────────────────────────────────────┤                  │
       │                  │                   │                  │
       │                  │ 8. Auth Code      │                  │
       │                  │    (Auto-handled) │                  │
       │                  │◄──────────────────┤                  │
       │                  │                   │                  │
       │                  │ 9. Exchange for   │                  │
       │                  │    JWT Tokens     │                  │
       │                  │◄─────────────────►│                  │
       │                  │                   │                  │
       │ 10. Authenticated│                   │                  │
       │◄─────────────────┤                   │                  │
```

### NAM IDP-Initiated Flow

```
┌─────────────┐    ┌──────────────┐    ┌─────────────┐    ┌──────────────┐
│    User     │    │  NAM Portal  │    │   Cognito   │    │ Angular App  │
└──────┬──────┘    └──────┬───────┘    └──────┬──────┘    └──────┬───────┘
       │                  │                   │                  │
       │ 1. Access Portal │                   │                  │
       ├─────────────────►│                   │                  │
       │                  │                   │                  │
       │ 2. Click TODO App│                   │                  │
       ├─────────────────►│                   │                  │
       │                  │                   │                  │
       │                  │ 3. Generate SAML  │                  │
       │                  │    Assertion      │                  │
       │                  ├──────────────────►│                  │
       │                  │                   │                  │
       │ 4. Redirect with Auth Code           │                  │
       │◄─────────────────────────────────────┤                  │
       │                  │                   │                  │
       │                  │                   │ 5. Detect Auth   │
       │                  │                   │    Code in URL   │
       │                  │                   │◄─────────────────┤
       │                  │                   │                  │
       │                  │                   │ 6. Manual Token  │
       │                  │                   │    Exchange      │
       │                  │                   │    (NAM Client ID)│
       │                  │                   │◄────────────────►│
       │                  │                   │                  │
       │ 7. Authenticated & Load Profile      │                  │
       │◄─────────────────────────────────────────────────────────┤
       │                  │                   │                  │
```

## Key Differences: SP-Initiated vs IDP-Initiated

| Aspect | Azure Entra (SP-Initiated) | NAM (IDP-Initiated) |
|--------|----------------------------|---------------------|
| **Flow Start** | User clicks login in Angular app | User clicks app link in NAM portal |
| **Token Exchange** | Automatic via Amplify | Manual implementation |
| **Client ID Usage** | Azure client ID throughout | NAM client ID for token exchange |
| **User Entry Point** | Angular application URL | NAM SSO portal |
| **Redirect Handling** | Amplify handles automatically | Custom code detects auth code |
| **Token Storage** | Amplify + Azure client ID key | Custom + NAM client ID key |
| **SAML Flow** | AuthnRequest → Response | Direct assertion (no request) |
| **Implementation** | `signInWithRedirect()` | `handleAuthorizationCode()` |

## Component Architecture

### Frontend Components

#### 1. amplify-config.ts
```typescript
// Configuration Management
- amplifyConfig: Base Amplify configuration
- clientIdConfig: Provider-specific client IDs
  * AzureSAML: Azure client ID
  * NAM-SAML: NAM client ID  
  * default: Fallback client ID

// Helper Functions
- getClientIdForProvider(provider): Returns correct client ID
- detectProviderFromToken(idToken): Auto-detects provider from token claims
```

#### 2. app.component.ts
```typescript
// Authentication Methods
- login(): Azure SAML authentication
- loginWithNAM(): NAM portal redirect
- handleAuthorizationCode(): NAM token exchange
- setAmplifyTokens(): Dynamic token storage
- getAuthHeaders(): API authorization headers

// Provider Detection Logic
- Explicit provider for NAM (IDP-initiated)
- Token-based detection for Azure (SP-initiated)
- Fallback to default for unknown scenarios
```

### Backend Integration

#### API Authentication
- All API requests include Bearer token in Authorization header
- Backend validates JWT tokens against Cognito public keys
- User context extracted from token claims (sub, email, etc.)

## Token Management Strategy

### Client ID Separation
```
localStorage Keys:
├── CognitoIdentityServiceProvider.{AZURE_CLIENT_ID}.LastAuthUser
├── CognitoIdentityServiceProvider.{AZURE_CLIENT_ID}.{username}.accessToken
├── CognitoIdentityServiceProvider.{AZURE_CLIENT_ID}.{username}.idToken
├── CognitoIdentityServiceProvider.{NAM_CLIENT_ID}.LastAuthUser
├── CognitoIdentityServiceProvider.{NAM_CLIENT_ID}.{username}.accessToken
└── CognitoIdentityServiceProvider.{NAM_CLIENT_ID}.{username}.idToken
```

### Provider Detection Methods
1. **Explicit Provider**: Known during authentication flow (NAM IDP-initiated)
2. **Token Claims**: Extract from ID token payload
   - `custom:identity_provider`
   - `identities[0].providerName`
   - `iss` (issuer) field analysis
3. **Fallback**: Default client ID for unknown scenarios

## Security Considerations

### SAML Configuration
- **Azure Entra**: SP-initiated SAML 2.0
  - Assertion Consumer Service (ACS) URL points to Cognito
  - Attribute mapping for user claims
  
- **NAM SAML**: IDP-initiated SAML 2.0
  - Direct SSO link from NAM portal
  - Custom attribute mappings for bank user attributes

### Token Security
- Tokens stored in localStorage with provider-specific keys
- JWT validation on backend against Cognito public keys
- Token refresh handled by Amplify automatically
- Session timeout based on token expiry

## Configuration Requirements

### Cognito User Pool Setup
```
Identity Providers:
├── AzureSAML (SP-Initiated)
│   ├── Client ID: {AZURE_CLIENT_ID}
│   ├── SAML Provider: Azure Entra
│   ├── SSO URL: Azure SAML endpoint
│   ├── Signing Certificate: Azure certificate
│   └── Attribute Mapping: email, given_name, family_name
└── NAM-SAML (IDP-Initiated)
    ├── Client ID: {NAM_CLIENT_ID}
    ├── SAML Provider: NAM Identity
    ├── Assertion Consumer Service: Cognito ACS URL
    ├── Signing Certificate: NAM certificate
    └── Attribute Mapping: Custom bank attributes (employee_id, department)

OAuth2 Configuration:
├── /oauth2/authorize endpoint (used by Azure SP-initiated)
├── /oauth2/token endpoint (used by both for token exchange)
└── /saml2/idpresponse endpoint (used by NAM IDP-initiated)
```

### Environment Variables
```typescript
// amplify-config.ts
clientIdConfig = {
  AzureSAML: process.env.AZURE_CLIENT_ID || 'default_azure_id',
  'NAM-SAML': process.env.NAM_CLIENT_ID || 'default_nam_id',
  default: process.env.DEFAULT_CLIENT_ID || 'fallback_id'
}
```

## Deployment Considerations

### Development Environment
- Single Cognito User Pool with test identity providers
- Local Angular development server
- Mock backend API for testing

### Production Environment
- Separate Cognito User Pools per environment
- HTTPS-only token exchange
- Production SAML certificates and endpoints
- Monitoring and logging for authentication events

## Troubleshooting Guide

### Common Issues
1. **Token Storage Conflicts**: Different client IDs prevent token conflicts
2. **Provider Detection Failures**: Fallback to default client ID
3. **SAML Attribute Mapping**: Verify claim mappings in Cognito
4. **Cross-Origin Issues**: Configure CORS for Cognito endpoints

### Logging Strategy
- Provider detection events
- Client ID selection logic
- Token exchange success/failure
- Authentication flow completion

## Future Enhancements

### Potential Improvements
1. **Dynamic Provider Registration**: Runtime addition of new identity providers
2. **Enhanced Token Caching**: Optimize token storage and retrieval
3. **Multi-Factor Authentication**: Additional security layers
4. **Session Management**: Advanced session timeout handling
5. **Audit Logging**: Comprehensive authentication event logging

---

*This architecture supports seamless dual identity provider integration while maintaining security and user experience standards.*
