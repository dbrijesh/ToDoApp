#!/bin/bash

# Development Stop Script for TODO Application
# This script stops both frontend and backend services

echo "🛑 Stopping TODO Application Development Services"
echo "================================================="

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

# Function to kill process by PID
kill_process() {
    local pid=$1
    local name=$2
    
    if [ ! -z "$pid" ] && kill -0 $pid 2>/dev/null; then
        print_status "Stopping $name (PID: $pid)..."
        kill $pid
        
        # Wait for process to stop
        for i in {1..10}; do
            if ! kill -0 $pid 2>/dev/null; then
                print_success "$name stopped successfully"
                return 0
            fi
            sleep 1
        done
        
        # Force kill if still running
        print_warning "Force killing $name..."
        kill -9 $pid 2>/dev/null || true
        print_success "$name force stopped"
    else
        print_warning "$name is not running (PID: $pid)"
    fi
}

# Function to kill processes by port
kill_by_port() {
    local port=$1
    local name=$2
    
    local pids=$(lsof -ti:$port 2>/dev/null || true)
    
    if [ ! -z "$pids" ]; then
        print_status "Found processes on port $port: $pids"
        for pid in $pids; do
            kill_process $pid "$name on port $port"
        done
    else
        print_status "No processes found on port $port"
    fi
}

# Stop services using saved PIDs
stop_by_pids() {
    if [ -f ".dev-pids" ]; then
        print_status "Reading saved process IDs..."
        source .dev-pids
        
        kill_process "$BACKEND_PID" "Backend"
        kill_process "$FRONTEND_PID" "Frontend"
        
        # Remove PID file
        rm -f .dev-pids
        print_success "PID file removed"
    else
        print_warning "No saved PIDs found (.dev-pids file missing)"
    fi
}

# Stop services by port (fallback)
stop_by_ports() {
    print_status "Stopping services by port..."
    
    kill_by_port 5000 "Backend"
    kill_by_port 4200 "Frontend"
}

# Stop any node/dotnet processes related to the project
stop_project_processes() {
    print_status "Checking for project-related processes..."
    
    # Find and kill ng serve processes
    local ng_pids=$(pgrep -f "ng serve" 2>/dev/null || true)
    if [ ! -z "$ng_pids" ]; then
        print_status "Found ng serve processes: $ng_pids"
        for pid in $ng_pids; do
            kill_process $pid "ng serve"
        done
    fi
    
    # Find and kill dotnet run processes
    local dotnet_pids=$(pgrep -f "dotnet run" 2>/dev/null || true)
    if [ ! -z "$dotnet_pids" ]; then
        print_status "Found dotnet run processes: $dotnet_pids"
        for pid in $dotnet_pids; do
            kill_process $pid "dotnet run"
        done
    fi
}

# Clean up log files
cleanup_logs() {
    print_status "Cleaning up log files..."
    
    if [ -f "logs/frontend.log" ]; then
        > logs/frontend.log
        print_status "Frontend log cleared"
    fi
    
    if [ -f "logs/backend.log" ]; then
        > logs/backend.log
        print_status "Backend log cleared"
    fi
}

# Main execution
main() {
    print_status "Stopping all development services..."
    
    # Try different methods to stop services
    stop_by_pids
    stop_by_ports
    stop_project_processes
    
    # Clean up
    cleanup_logs
    
    print_success "All services stopped successfully! ✅"
    print_status "Development environment is now clean"
}

# Run main function
main "$@"