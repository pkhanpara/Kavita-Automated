#! /bin/bash
set -e

outputFolder='_output'

# Initialize flags
RUNTIME_ONLY=false
UI_ONLY=false
RID=""

# Parse command line arguments
while [[ $# -gt 0 ]]; do
    case $1 in
        --runtime-only)
            RUNTIME_ONLY=true
            shift
            ;;
        --ui-only)
            UI_ONLY=true
            shift
            ;;
        *)
            # Check if it's a runtime identifier (not a flag)
            if [[ ! "$1" =~ ^-- ]]; then
                RID="$1"
            fi
            shift
            ;;
    esac
done

CheckRequirements()
{
    if ! command -v npm &> /dev/null
    then
        echo "Warning!!! npm not found, it is required for building Kavita!"
    fi
    if ! command -v dotnet &> /dev/null
    then
        echo "Warning!!! dotnet not found, it is required for building Kavita!"
    fi
}

ProgressStart()
{
    echo "Start '$1'"
}

ProgressEnd()
{
    echo "Finish '$1'"
}


Build()
{
    ProgressStart 'Build'

    rm -rf $outputFolder

    slnFile=Kavita.sln

    dotnet clean $slnFile -c Release

    if [[ -z "$RID" ]];
    then
        dotnet msbuild -restore $slnFile -p:Configuration=Release -p:Platform="Any CPU" 
    else
        dotnet msbuild -restore $slnFile -p:Configuration=Release -p:Platform="Any CPU" -p:RuntimeIdentifiers=$RID 
    fi

    ProgressEnd 'Build'
}

BuildUI()
{
    ProgressStart 'Building UI'
    echo 'Removing old wwwroot'
    rm -rf Kavita.Server/wwwroot/*
    cd UI/Web/ || exit
    echo 'Installing web dependencies'
    npm ci
    echo 'Building UI'
    npm run prod
    echo 'Copying back to Kavita wwwroot'
    mkdir -p ../../Kavita.Server/wwwroot
    cp -R dist/browser/* ../../Kavita.Server/wwwroot
    cd ../../ || exit
    ProgressEnd 'Building UI'
}

Package()
{
    local runtime="$1"
    local lOutputFolder=../_output/"$runtime"/Kavita

    ProgressStart "Creating $runtime Package"

    # TODO: Use no-restore? Because Build should have already done it for us
    echo "Building"
    cd Kavita.Server
    echo dotnet publish -c Release --self-contained --runtime $runtime -o "$lOutputFolder" 
    dotnet publish -c Release --self-contained --runtime $runtime -o "$lOutputFolder" 

    echo "Recopying wwwroot due to bug"
    cp -R ./wwwroot/* $lOutputFolder/wwwroot

    echo "Removing EF Core design-time folders"
    rm -rf "$lOutputFolder"/BuildHost-net472
    rm -rf "$lOutputFolder"/BuildHost-netcore

    echo "Removing cache-long from config"
    rm -rf "$lOutputFolder"/config/cache-long

    echo "Copying Install information"
    cp ../INSTALL.txt "$lOutputFolder"/README.txt

    echo "Copying LICENSE"
    cp ../LICENSE "$lOutputFolder"/LICENSE.txt

    echo "Renaming Kavita.Server -> Kavita"
    if [ $runtime == "win-x64" ] || [ $runtime == "win-x86" ]
    then
        mv "$lOutputFolder"/Kavita.Server.exe "$lOutputFolder"/Kavita.exe
    else
        mv "$lOutputFolder"/Kavita.Server "$lOutputFolder"/Kavita
    fi

    mkdir -p $lOutputFolder/config
    echo "Copying appsettings.json"
    cp config/appsettings.json $lOutputFolder/config/appsettings-init.json

    echo "Creating tar"
    cd ../$outputFolder/"$runtime"/
    tar -czvf ../kavita-$runtime.tar.gz Kavita


    ProgressEnd "Creating $runtime Package"
}


# Main execution logic based on flags
if [[ "$UI_ONLY" == "true" ]]; then
    # Build UI only (no .NET build or packaging)
    echo "========================================="
    echo "Mode: UI Only"
    echo "========================================="
    CheckRequirements
    BuildUI
    echo "========================================="
    echo "UI build completed successfully!"
    echo "========================================="
elif [[ "$RUNTIME_ONLY" == "true" ]]; then
    # Build only specified runtime (skip UI rebuild and full .NET build)
    echo "========================================="
    echo "Mode: Runtime Only"
    echo "Runtime: $RID"
    echo "========================================="
    
    if [[ -z "$RID" ]]; then
        echo "Error: Please specify a runtime identifier with --runtime-only"
        echo "Usage: ./build.sh --runtime-only linux-x64"
        exit 1
    fi
    
    CheckRequirements
    # Skip BuildUI() to use existing UI
    echo "Skipping UI rebuild (using existing build)"
    dir=$PWD
    Package "$RID"
    cd "$dir"
    echo "========================================="
    echo "Runtime package created successfully!"
    echo "========================================="
else
    # Default: Build UI + All runtimes (or specified RID)
    echo "========================================="
    echo "Mode: Full Build"
    echo "========================================="
    CheckRequirements
    BuildUI
    Build

    dir=$PWD

    if [[ -z "$RID" ]];
    then
        Package "win-x64"
        cd "$dir"
        Package "win-x86"
        cd "$dir"
        Package "linux-x64"
        cd "$dir"
        Package "linux-arm"
        cd="$dir"
        Package "linux-arm64"
        cd "$dir"
        Package "linux-musl-x64"
        cd "$dir"
        Package "osx-x64"
        cd "$dir"
        Package "osx-arm64"
        cd="$dir"
    else
        Package "$RID"
        cd "$dir"
    fi
    echo "========================================="
    echo "Full build completed successfully!"
    echo "========================================="
fi
