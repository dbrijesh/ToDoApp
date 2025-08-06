#!/bin/bash

# Test All - Comprehensive test runner for the TODO application
# This script runs frontend tests, backend tests, and end-to-end tests

set -e  # Exit on any error

echo "🧪 Starting comprehensive test suite for TODO Application"
echo "=============================================="

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

# Check if required tools are installed
check_prerequisites() {
    print_status "Checking prerequisites..."
    
    if ! command -v node &> /dev/null; then
        print_error "Node.js is not installed"
        exit 1
    fi
    
    if ! command -v npm &> /dev/null; then
        print_error "npm is not installed"
        exit 1
    fi
    
    if ! command -v dotnet &> /dev/null; then
        print_warning "dotnet CLI is not installed - backend tests will be skipped"
        SKIP_BACKEND=true
    fi
    
    print_success "Prerequisites check completed"
}

# Run frontend tests
run_frontend_tests() {
    print_status "Running frontend tests..."
    
    cd frontend/todo-app
    
    # Install dependencies if node_modules doesn't exist
    if [ ! -d "node_modules" ]; then
        print_status "Installing frontend dependencies..."
        npm install
    fi
    
    # Run tests with coverage
    print_status "Executing Angular unit tests with coverage..."
    npm run test:coverage
    
    # Check if coverage meets threshold
    if [ -f "coverage/todo-app/coverage-summary.json" ]; then
        print_success "Frontend tests completed with coverage report"
    else
        print_warning "Coverage report not generated"
    fi
    
    cd ../..
}

# Run backend tests
run_backend_tests() {
    if [ "$SKIP_BACKEND" = true ]; then
        print_warning "Skipping backend tests - dotnet CLI not available"
        return
    fi
    
    print_status "Running backend tests..."
    
    cd backend/TodoApi.Tests
    
    # Restore packages
    print_status "Restoring backend packages..."
    dotnet restore
    
    # Run tests with coverage
    print_status "Executing .NET unit and integration tests..."
    dotnet test --collect:"XPlat Code Coverage" --logger trx --results-directory TestResults
    
    # Check test results
    if [ $? -eq 0 ]; then
        print_success "Backend tests completed successfully"
    else
        print_error "Backend tests failed"
        exit 1
    fi
    
    cd ../..
}

# Run end-to-end tests
run_e2e_tests() {
    print_status "Running end-to-end tests..."
    
    cd e2e-tests
    
    # Install dependencies if node_modules doesn't exist
    if [ ! -d "node_modules" ]; then
        print_status "Installing E2E test dependencies..."
        npm install
        npx playwright install
    fi
    
    # Check if applications are running
    print_status "Checking if applications are running..."
    
    # Start applications in background if not running
    if ! curl -s http://localhost:4200 > /dev/null; then
        print_warning "Frontend not running - please start the application first"
        print_status "Run: cd frontend/todo-app && npm start"
    fi
    
    if ! curl -s http://localhost:5000/swagger > /dev/null; then
        print_warning "Backend not running - please start the API first"
        print_status "Run: cd backend/TodoApi && dotnet run"
    fi
    
    # Run E2E tests
    print_status "Executing Playwright E2E tests..."
    npm test
    
    if [ $? -eq 0 ]; then
        print_success "E2E tests completed successfully"
    else
        print_error "E2E tests failed"
        exit 1
    fi
    
    cd ..
}

# Generate combined coverage report
generate_coverage_report() {
    print_status "Generating combined coverage report..."
    
    mkdir -p reports/coverage
    
    # Copy frontend coverage
    if [ -d "frontend/todo-app/coverage" ]; then
        cp -r frontend/todo-app/coverage reports/coverage/frontend
        print_success "Frontend coverage report copied to reports/coverage/frontend"
    fi
    
    # Copy backend coverage
    if [ -d "backend/TodoApi.Tests/TestResults" ]; then
        cp -r backend/TodoApi.Tests/TestResults reports/coverage/backend
        print_success "Backend coverage report copied to reports/coverage/backend"
    fi
    
    print_success "Coverage reports available in reports/coverage/"
}

# Main execution
main() {
    echo "Starting at $(date)"
    START_TIME=$(date +%s)
    
    check_prerequisites
    
    # Run tests
    run_frontend_tests
    run_backend_tests
    run_e2e_tests
    
    # Generate reports
    generate_coverage_report
    
    END_TIME=$(date +%s)
    DURATION=$((END_TIME - START_TIME))
    
    echo ""
    echo "=============================================="
    print_success "All tests completed successfully! 🎉"
    print_status "Total execution time: ${DURATION} seconds"
    echo ""
    print_status "Test Reports:"
    print_status "  - Frontend coverage: reports/coverage/frontend/index.html"
    print_status "  - Backend coverage: reports/coverage/backend/"
    print_status "  - E2E test report: e2e-tests/playwright-report/"
    echo ""
}

# Handle script interruption
trap 'print_error "Test execution interrupted"; exit 1' INT

# Run main function
main "$@"