# Todo Application with AWS Cognito SAML Authentication

A full-stack todo application with Angular frontend, .NET Core backend, and AWS Cognito SAML authentication integrated with Azure Entra ID.

## Architecture

```
User → Azure Entra ID (SAML) → AWS Cognito → Angular Frontend ↔ .NET Backend
```

## Features

- **Frontend**: Angular 19 with AWS Amplify SDK
- **Backend**: .NET 8 Web API with JWT validation
- **Authentication**: SAML federation between Azure Entra ID and AWS Cognito
- **Containerization**: Docker support for both frontend and backend
- **CORS**: Configured for cross-origin requests

## Authentication Flow

1. User clicks login → Redirects to Cognito Hosted UI
2. Cognito redirects to Azure Entra ID SAML endpoint
3. User authenticates with Azure credentials
4. Azure sends SAML assertion to Cognito
5. Cognito issues JWT token → User redirected to application
6. Frontend receives JWT token → Backend validates token for API access

## Configuration

### AWS Cognito Setup

**User Pool**: `us-east-1_aibygKCIA`
**App Client**: `6iv9saf42n4aft5pverjpjpaq6`
**Domain**: `us-east-1aibygkcia.auth.us-east-1.amazoncognito.com`

**SAML Identity Provider**: `AzureSAML`
- **Metadata URL**: Azure Enterprise Application federation metadata
- **Attribute Mapping**: 
  - `email` → `http://schemas.xmlsoap.org/ws/2005/05/identity/claims/emailaddress`

### Azure Entra ID Setup

**Enterprise Application**: `TodoApp-SAML`
- **Entity ID**: `urn:amazon:cognito:sp:us-east-1_aibygKCIA`
- **Reply URL**: `https://us-east-1aibygkcia.auth.us-east-1.amazoncognito.com/saml2/idpresponse`

## Development

### Prerequisites

- Node.js 20+ (for frontend)
- .NET 8 SDK (for backend)
- Docker (optional)

### Start Development Environment

```bash
# Using scripts
./scripts/start-dev.sh

# Or manually
cd frontend/todo-app && npm start
cd backend/TodoApi && dotnet run
```

### Build Application

```bash
# Frontend
cd frontend/todo-app
npm run build

# Backend  
cd backend/TodoApi
dotnet build
```

### Docker Deployment

```bash
# Build and run with Docker Compose
docker-compose up --build

# Or individual services
docker build -t todo-frontend ./frontend/todo-app
docker build -t todo-backend ./backend/TodoApi
```

## API Endpoints

All API endpoints require valid Cognito JWT token in Authorization header.

- `GET /api/todos` - Get all todos
- `POST /api/todos` - Create todo
- `PUT /api/todos/{id}` - Update todo
- `DELETE /api/todos/{id}` - Delete todo

## Environment Variables

### Backend (.NET)

```
AWS__Region=us-east-1
AWS__UserPoolId=us-east-1_aibygKCIA  
AWS__AppClientId=6iv9saf42n4aft5pverjpjpaq6
ASPNETCORE_URLS=http://+:5000
```

### Frontend (Angular)

Configuration in `src/amplify-config.ts`:

```typescript
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
```

## Testing

```bash
# Run all tests
./scripts/test-all.sh

# Frontend tests
cd frontend/todo-app && npm test

# Backend tests
cd backend && dotnet test

# E2E tests
cd e2e-tests && npm test
```

## Troubleshooting

### Common Issues

1. **SAML Authentication Fails**: Check Azure Enterprise Application user assignments
2. **JWT Validation Fails**: Verify Cognito configuration matches backend settings
3. **CORS Issues**: Ensure backend CORS policy allows frontend origin

### Debug Steps

1. Check browser console for authentication errors
2. Verify SAML response contains required attributes  
3. Check backend logs for JWT validation issues
4. Ensure all URLs and IDs match between Azure, Cognito, and application

## Security

- JWT tokens validated against Cognito public keys
- CORS configured for specific origins only
- SAML assertions signed and encrypted
- No sensitive data logged in production

## License

MIT License