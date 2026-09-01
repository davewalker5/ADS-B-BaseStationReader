#!/usr/bin/env bash

if [[ $# -ne 1 ]]; then
    echo Usage: $(basename "$0") .NET-RID
    exit 1
fi

PROJECT_FOLDER=$( cd "$( dirname "$0" )/.." && pwd )
PROJECT_FILE="$PROJECT_FOLDER/BaseStationReader.Terminal/BaseStationReader.Terminal.csproj"
CONFIG_FILE="$PROJECT_FOLDER/config/terminal/appsettings.$1.json"
PUBLISH_FOLDER="$PROJECT_FOLDER/published/terminal/$1"
MY_APPS_BASE_FOLDER=$(~/Scripts/get-my-apps-folder.sh)
MY_APPS_FOLDER=$MY_APPS_BASE_FOLDER/BaseStationReader/terminal

echo ""
echo "Target OS      : $1"
echo "Project        : $PROJECT_FILE"
echo "Settings       : $CONFIG_FILE"
echo "Publish Folder : $PUBLISH_FOLDER"
echo "Target Folder  : $MY_APPS_FOLDER"
echo ""

echo Publishing to $PUBLISH_FOLDER ...
rm -fr $PUBLISH_FOLDER
dotnet publish $PROJECT_FILE -c Release -r $1 --self-contained -o $PUBLISH_FOLDER
cp $CONFIG_FILE $PUBLISH_FOLDER/appsettings.json

RID=$(~/Scripts/get-dotnet-rid.sh)
if [ $1 != $RID ] ; then
    exit 0
fi

echo Copying to $MY_APPS_FOLDER ...
rm -fr $MY_APPS_FOLDER
cp -r $PUBLISH_FOLDER $MY_APPS_FOLDER
