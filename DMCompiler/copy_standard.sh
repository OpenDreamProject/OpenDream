#!/bin/sh
set -xe

SCRIPT=$(readlink -f "$0")
SCRIPTPATH=$(dirname "$SCRIPT")

if [ -d "$SCRIPTPATH/bin/Debug/net8.0/DMStandard" ]; then
	cp -r $SCRIPTPATH/DMStandard $SCRIPTPATH/bin/Debug/net8.0/DMStandard
else
	mkdir -p $SCRIPTPATH/bin/Debug/net8.0/DMStandard
	cp -r $SCRIPTPATH/DMStandard $SCRIPTPATH/bin/Debug/net8.0/DMStandard
fi

if [ -d "$SCRIPTPATH/bin/Release/net8.0/DMStandard" ]; then
	cp -r $SCRIPTPATH/DMStandard $SCRIPTPATH/bin/Release/net8.0/DMStandard
else
	mkdir -p $SCRIPTPATH/bin/Release/net8.0/DMStandard
	cp -r $SCRIPTPATH/DMStandard $SCRIPTPATH/bin/Release/net8.0/DMStandard
fi
