#!/bin/bash

# Function to display the menu
show_menu() {
    clear
    echo "================================================"
    echo "1. Start project (dotnet watch)"
    echo "2. Create new migration"
    echo "3. Apply migrations"
    echo "4. Remove, unapply latest migration, create new migration and apply migration"
    echo "5. Deploy bicep files"
    echo "6. Run all tests (useful for integration tests)"
    echo "7. Pull Git submodules"
    echo "8. Exit"
    echo "================================================"
}

apply_migration() {
    dotnet ef database update \
        --startup-project ./Brainy.WebApi \
        --project ./Brainy.Infrastructure
}

create_new_migration() {
    read -r -p "What is the name of the migration: " migrationName
    dotnet ef migrations add \
        --startup-project ./Brainy.WebApi \
        --project ./Brainy.Infrastructure  \
        "$migrationName"
}

deploy_bicep() {
    cd ./Brainy.Infrastructure/Biceps || exit
    # Used to force the use of the installed version.
    az config set bicep.use_binary_from_path=true
    az login
    az deployment sub create --location NorwayEast --template-file main.bicep
    cd - || exit
}

show_menu
read -r -p "Please select an option: " choice

case $choice in
    1)
        dotnet watch --project ./Brainy.WebApi
        ;;
    2)
        create_new_migration
        ;;
    3)
        apply_migration
        ;;
    4)
        dotnet ef migrations remove --force \
            --startup-project ./Brainy.WebApi \
            --project ./Brainy.Infrastructure
        create_new_migration
        apply_migration
        ;;
    5)
        deploy_bicep
        ;;
    6)
        dotnet test
        ;;
    7)
        git submodule update --init --recursive
        ;;
    8)
        exit 0
        ;;
    *)
        echo "❌ Invalid option."
        echo
        ;;
esac

