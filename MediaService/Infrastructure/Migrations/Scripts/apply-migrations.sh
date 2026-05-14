#!/bin/bash

# =============================================================================
# Migration Application Script
# =============================================================================
# This script applies database migrations in sequential order
#
# File permissions (executable):
#   chmod 755 apply-migrations.sh
#   or
#   chmod +x apply-migrations.sh
#
# Usage:
#   ./scripts/apply-migrations.sh [options]
#
# Options:
#   --dry-run       Show what migrations would be applied without executing them
#   --help          Show this help
#
# Examples:
#   ./scripts/apply-migrations.sh
#   ./scripts/apply-migrations.sh --dry-run
# =============================================================================

set -e  # Exit on error

# Colors for output
RED='\033[0;31m'
GREEN='\033[0;32m'
YELLOW='\033[1;33m'
BLUE='\033[0;34m'
NC='\033[0m' # No Color

# Configuration
MIGRATIONS_DIR="../"
DB_NAME="${DB_NAME:-db}"
DB_USER="${DB_USER:-user}"
DB_HOST="${DB_HOST:-localhost}"
DB_PORT="${DB_PORT:-5432}"

# Functions
print_header() {
    echo -e "${BLUE}================================================${NC}"
    echo -e "${BLUE} Migration Tool${NC}"
    echo -e "${BLUE}================================================${NC}"
    echo ""
}

print_success() {
    echo -e "${GREEN}✓${NC} $1"
}

print_error() {
    echo -e "${RED}✗${NC} $1"
}

print_warning() {
    echo -e "${YELLOW}⚠${NC} $1"
}

print_info() {
    echo -e "${BLUE}ℹ${NC} $1"
}

show_help() {
    cat << EOF
Usage: $0 [options]

Options:
  --dry-run       Show what migrations would be applied without executing them
  --help          Show this help

Environment variables:
  DB_NAME         Database name (default: db)
  DB_USER         PostgreSQL user (default: user)
  DB_HOST         PostgreSQL host (default: localhost)
  DB_PORT         PostgreSQL port (default: 5432)

Examples:
  # Apply all migrations
  $0

  # Preview what migrations would be applied
  $0 --dry-run

  # Use custom environment variables
  DB_NAME=my_db DB_USER=myuser $0

EOF
}

# Check if psql is installed
check_dependencies() {
    if ! command -v psql &> /dev/null; then
        print_error "psql is not installed. Please install PostgreSQL client."
        exit 1
    fi
}

# Test database connection
test_connection() {
    print_info "Testing database connection..."
    if PGPASSWORD=$PGPASSWORD psql -h $DB_HOST -p $DB_PORT -U $DB_USER -d $DB_NAME -c "SELECT 1;" > /dev/null 2>&1; then
        print_success "Connection successful to $DB_NAME"
    else
        print_error "Could not connect to database $DB_NAME"
        print_info "Make sure PostgreSQL is running and credentials are correct"
        exit 1
    fi
}

# Get list of available migrations
get_available_migrations() {
    find $MIGRATIONS_DIR -name "*.sql" -not -name "*_rollback.sql" | sort | xargs -n 1 basename
}

# Apply a single migration
apply_migration() {
    local migration_file=$1
    local migration_name=$(basename "$migration_file")

    print_info "Applying: $migration_name"

    if PGPASSWORD=$PGPASSWORD psql -h $DB_HOST -p $DB_PORT -U $DB_USER -d $DB_NAME -f "$migration_file" > /dev/null 2>&1; then
        print_success "Migration applied: $migration_name"
        return 0
    else
        print_error "Error applying: $migration_name"
        return 1
    fi
}

# Main function to apply all migrations
apply_all_migrations() {
    local dry_run=$1
    local applied_count=0

    print_info "Searching for migrations in: $MIGRATIONS_DIR"
    echo ""

    for migration_file in $(find $MIGRATIONS_DIR -name "*.sql" -not -name "*_rollback.sql" | sort); do
        local migration_name=$(basename "$migration_file")

        if [ "$dry_run" = true ]; then
            print_warning "Found: $migration_name"
            ((applied_count++))
        else
            if apply_migration "$migration_file"; then
                ((applied_count++))
            else
                print_error "Aborting due to migration error"
                exit 1
            fi
        fi
    done

    echo ""
    print_info "Summary:"
    echo "  - Migrations: $applied_count"
}

# Ask user for backup confirmation
ask_backup_confirmation() {
    echo -e "${YELLOW}Do you want to create a backup before applying migrations?${NC}"
    read -p "Enter (y/n): " -n 1 -r
    echo ""
    if [[ $REPLY =~ ^[Yy]$ ]]; then
        return 0
    else
        return 1
    fi
}

# Create backup
create_backup() {
    local backup_dir="./backups"
    local timestamp=$(date +%Y%m%d_%H%M%S)
    local backup_file="${backup_dir}/backup_${DB_NAME}_${timestamp}.sql"

    mkdir -p "$backup_dir"

    print_info "Creating backup at: $backup_file"

    if PGPASSWORD=$PGPASSWORD pg_dump -h $DB_HOST -p $DB_PORT -U $DB_USER $DB_NAME > "$backup_file" 2>/dev/null; then
        print_success "Backup created successfully"
        echo "  Location: $backup_file"
    else
        print_warning "Could not create backup (continuing anyway)"
    fi

    echo ""
}

# Main script logic
main() {
    local dry_run=false

    # Parse arguments
    while [[ $# -gt 0 ]]; do
        case $1 in
            --dry-run)
                dry_run=true
                shift
                ;;
            --help)
                show_help
                exit 0
                ;;
            *)
                print_error "Unknown option: $1"
                show_help
                exit 1
                ;;
        esac
    done

    print_header

    # Check dependencies
    check_dependencies

    # Test database connection
    test_connection
    echo ""

    if [ "$dry_run" = true ]; then
        # Dry run mode
        print_info "DRY RUN MODE: No changes will be applied"
        echo ""
        apply_all_migrations true
    else
        # Normal mode
        print_info "NORMAL MODE: Applying migrations"
        echo ""

        # Ask for backup confirmation
        if ask_backup_confirmation; then
            # Create backup before applying migrations
            create_backup
        else
            print_warning "Skipping backup"
            echo ""
        fi

        # Apply migrations
        apply_all_migrations false
    fi

    echo ""
    print_success "Process completed!"
}

# Run main function
main "$@"
