#!/bin/bash
set -euo pipefail

touch errors.log
DreamDaemon Content.IntegrationTets/DMProject/environment.m.dmb -once -close -trusted -verbose -invisible
if [ -s "errors.log" ]
then
	echo "Errors detected!"
	sed -i '/^[[:space:]]*$/d' ./errors.log
	cat errors.log
	exit 1
else
	echo "No errors detected."
	exit 0
fi
