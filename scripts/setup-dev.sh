#!/bin/bash

# Development Setup Script for TODO Application
# This script sets up the development environment

set -e  # Exit on any error

echo "🚀 Setting up TODO Application Development Environment"
echo "====================================================="

# Colors for output
RED='\033[0;31m'
GREEN='\033[0;32m'
YELLOW='\033[1;33m'
BLUE='\033[0;34m'
NC='\033[0m' # No Color

# Function to print colored output
print_status() {
    echo -e "${BLUE}[INFO]${NC} $1"
}

print_success() {
    echo -e "${GREEN}[SUCCESS]${NC} $1"
}

print_warning() {
    echo -e "${YELLOW}[WARNING]${NC} $1"
}

print_error() {
    echo -e "${RED}[ERROR]${NC} $1"
}

# Check prerequisites
check_prerequisites() {
    print_status "Checking prerequisites..."
    
    # Check Node.js
    if command -v node &> /dev/null; then
        NODE_VERSION=$(node --version)
        print_success "Node.js found: $NODE_VERSION"
        
        # Check if version is 20.x or higher
        if [[ $NODE_VERSION =~ v([0-9]+)\. ]]; then
            MAJOR_VERSION=${BASH_REMATCH[1]}
            if [ "$MAJOR_VERSION" -lt 20 ]; then
                print_warning "Node.js version $NODE_VERSION found. Recommended: 20.x or higher"
            fi
        fi
    else
        print_error "Node.js is not installed. Please install Node.js 20.x or higher"
        exit 1
    fi
    
    # Check npm
    if command -v npm &> /dev/null; then
        NPM_VERSION=$(npm --version)
        print_success "npm found: v$NPM_VERSION"
    else
        print_error "npm is not installed"
        exit 1
    fi
    
    # Check .NET
    if command -v dotnet &> /dev/null; then
        DOTNET_VERSION=$(dotnet --version)
        print_success ".NET found: $DOTNET_VERSION"
    else
        print_warning ".NET 8 SDK is not installed. Backend development will not be available."
        print_status "To install .NET 8 SDK, visit: https://dotnet.microsoft.com/download"
    fi
}

# Setup frontend
setup_frontend() {
    print_status "Setting up Angular frontend..."
    
    cd frontend/todo-app
    
    # Install dependencies
    print_status "Installing frontend dependencies..."
    npm install
    
    # Verify Angular CLI
    if command -v ng &> /dev/null; then
        print_success "Angular CLI is available"
    else
        print_status "Installing Angular CLI globally..."
        npm install -g @angular/cli@19
    fi
    
    print_success "Frontend setup completed"
    cd ../..
}

# Setup backend
setup_backend() {
    if ! command -v dotnet &> /dev/null; then
        print_warning "Skipping backend setup - .NET SDK not available"
        return
    fi
    
    print_status "Setting up .NET backend..."
    
    cd backend/TodoApi
    
    # Restore packages
    print_status "Restoring backend packages..."
    dotnet restore
    
    # Build project
    print_status "Building backend project..."
    dotnet build
    
    print_success "Backend setup completed"
    cd ../..
    
    # Setup test project
    print_status "Setting up backend tests..."
    cd backend/TodoApi.Tests
    dotnet restore
    dotnet build
    print_success "Backend tests setup completed"
    cd ../..
}

# Setup E2E tests
setup_e2e_tests() {
    print_status "Setting up end-to-end tests..."
    
    cd e2e-tests
    
    # Install dependencies
    print_status "Installing E2E test dependencies..."
    npm install
    
    # Install Playwright browsers
    print_status "Installing Playwright browsers..."
    npx playwright install
    
    print_success "E2E tests setup completed"
    cd ..
}

# Create necessary directories
create_directories() {
    print_status "Creating necessary directories..."
    
    mkdir -p reports/coverage
    mkdir -p logs
    
    print_success "Directories created"
}

# Create environment files
create_env_files() {
    print_status "Creating environment configuration files..."
    
    # Frontend environment
    if [ ! -f "frontend/todo-app/src/environments/environment.ts" ]; then
        mkdir -p frontend/todo-app/src/environments
        cat > frontend/todo-app/src/environments/environment.ts << 'EOF'
export const environment = {
  production: false,
  apiUrl: 'http://localhost:5000/api',
  msalConfig: {
    auth: {
      clientId: '01d37875-07ee-427e-8be5-594fbe4c5632',
      authority: 'https://login.microsoftonline.com/52451440-c2a9-442f-8c20-8562d49f6846',
      redirectUri: 'http://localhost:4200/callback'
    }
  }
};
EOF
        print_success "Frontend environment file created"
    fi
    
    # Production environment
    if [ ! -f "frontend/todo-app/src/environments/environment.prod.ts" ]; then
        cat > frontend/todo-app/src/environments/environment.prod.ts << 'EOF'
export const environment = {
  production: true,
  apiUrl: 'http://localhost:5000/api',
  msalConfig: {
    auth: {
      clientId: '01d37875-07ee-427e-8be5-594fbe4c5632',
      authority: 'https://login.microsoftonline.com/52451440-c2a9-442f-8c20-8562d49f6846',
      redirectUri: 'http://localhost:4200/callback'
    }
  }
};
EOF
        print_success "Production environment file created"
    fi
}

# Display next steps
show_next_steps() {
    echo ""
    echo "=============================================="
    print_success "Development environment setup completed! 🎉"
    echo ""
    print_status "Next steps:"
    echo ""
    print_status "1. Start the backend API:"
    echo "   cd backend/TodoApi && dotnet run"
    echo ""
    print_status "2. Start the frontend application:"
    echo "   cd frontend/todo-app && npm start"
    echo ""
    print_status "3. Access the application:"
    echo "   Frontend: http://localhost:4200"
    echo "   Backend API: http://localhost:5000"
    echo "   Swagger UI: http://localhost:5000/swagger"
    echo ""
    print_status "4. Run tests:"
    echo "   All tests: ./scripts/test-all.sh"
    echo "   Frontend only: cd frontend/todo-app && npm test"
    echo "   Backend only: cd backend/TodoApi.Tests && dotnet test"
    echo "   E2E only: cd e2e-tests && npm test"
    echo ""
    print_status "5. Build for production:"
    echo "   Frontend: cd frontend/todo-app && npm run build:prod"
    echo "   Backend: cd backend/TodoApi && dotnet publish -c Release"
    echo ""
    print_status "6. Run with Docker:"
    echo "   Development: docker-compose -f docker-compose.dev.yml up"
    echo "   Production: docker-compose up"
    echo ""
}

# Main execution
main() {
    echo "Starting setup at $(date)"
    START_TIME=$(date +%s)
    
    check_prerequisites
    create_directories
    create_env_files
    setup_frontend
    setup_backend
    setup_e2e_tests
    
    END_TIME=$(date +%s)
    DURATION=$((END_TIME - START_TIME))
    
    show_next_steps
    print_status "Setup completed in ${DURATION} seconds"
}

# Handle script interruption
trap 'print_error "Setup interrupted"; exit 1' INT

# Run main function
main "$@"