#!/bin/bash

# Development Startup Script for TODO Application
# This script starts both frontend and backend in development mode

set -e  # Exit on any error

echo "🚀 Starting TODO Application in Development Mode"
echo "================================================"

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

# Function to check if port is in use
check_port() {
    if lsof -Pi :$1 -sTCP:LISTEN -t >/dev/null ; then
        return 0  # Port is in use
    else
        return 1  # Port is free
    fi
}

# Function to start backend
start_backend() {
    if ! command -v dotnet &> /dev/null; then
        print_warning "dotnet CLI not found - skipping backend startup"
        return
    fi
    
    if check_port 5000; then
        print_warning "Port 5000 is already in use - backend may already be running"
        return
    fi
    
    print_status "Starting .NET backend on port 5000..."
    
    cd backend/TodoApi
    
    # Check if project exists
    if [ ! -f "TodoApi.csproj" ]; then
        print_error "Backend project not found. Please run setup-dev.sh first."
        exit 1
    fi
    
    # Start backend in background
    nohup dotnet run > ../../logs/backend.log 2>&1 &
    BACKEND_PID=$!
    
    print_status "Backend starting with PID: $BACKEND_PID"
    
    # Wait for backend to start
    print_status "Waiting for backend to start..."
    for i in {1..30}; do
        if curl -s http://localhost:5000/swagger > /dev/null 2>&1; then
            print_success "Backend is running at http://localhost:5000"
            print_status "Swagger UI available at http://localhost:5000/swagger"
            break
        fi
        sleep 2
        if [ $i -eq 30 ]; then
            print_error "Backend failed to start within 60 seconds"
            exit 1
        fi
    done
    
    cd ../..
}

# Function to start frontend
start_frontend() {
    if check_port 4200; then
        print_warning "Port 4200 is already in use - frontend may already be running"
        return
    fi
    
    print_status "Starting Angular frontend on port 4200..."
    
    cd frontend/todo-app
    
    # Check if project exists
    if [ ! -f "package.json" ]; then
        print_error "Frontend project not found. Please run setup-dev.sh first."
        exit 1
    fi
    
    # Check if node_modules exists
    if [ ! -d "node_modules" ]; then
        print_status "Installing frontend dependencies..."
        npm install
    fi
    
    # Start frontend in background
    nohup npm start > ../../logs/frontend.log 2>&1 &
    FRONTEND_PID=$!
    
    print_status "Frontend starting with PID: $FRONTEND_PID"
    
    # Wait for frontend to start
    print_status "Waiting for frontend to start..."
    for i in {1..60}; do
        if curl -s http://localhost:4200 > /dev/null 2>&1; then
            print_success "Frontend is running at http://localhost:4200"
            break
        fi
        sleep 2
        if [ $i -eq 60 ]; then
            print_error "Frontend failed to start within 120 seconds"
            exit 1
        fi
    done
    
    cd ../..
}

# Function to save PIDs
save_pids() {
    echo "BACKEND_PID=$BACKEND_PID" > .dev-pids
    echo "FRONTEND_PID=$FRONTEND_PID" >> .dev-pids
    print_status "Process IDs saved to .dev-pids"
}

# Function to show status
show_status() {
    echo ""
    echo "=============================================="
    print_success "TODO Application is running! 🎉"
    echo ""
    print_status "Services:"
    echo "  📱 Frontend:  http://localhost:4200"
    echo "  🔧 Backend:   http://localhost:5000"
    echo "  📚 Swagger:   http://localhost:5000/swagger"
    echo ""
    print_status "Logs:"
    echo "  📄 Frontend:  tail -f logs/frontend.log"
    echo "  📄 Backend:   tail -f logs/backend.log"
    echo ""
    print_status "To stop the application:"
    echo "  ./scripts/stop-dev.sh"
    echo ""
    print_status "To run tests:"
    echo "  ./scripts/test-all.sh"
    echo ""
}

# Create logs directory
mkdir -p logs

# Main execution
main() {
    print_status "Starting development environment..."
    
    start_backend
    start_frontend
    save_pids
    show_status
    
    print_status "Both services are running in the background"
    print_warning "Use ./scripts/stop-dev.sh to stop all services"
}

# Handle script interruption
cleanup() {
    print_warning "Startup interrupted"
    if [ ! -z "$BACKEND_PID" ]; then
        kill $BACKEND_PID 2>/dev/null || true
    fi
    if [ ! -z "$FRONTEND_PID" ]; then
        kill $FRONTEND_PID 2>/dev/null || true
    fi
    exit 1
}

trap cleanup INT

# Run main function
main "$@"