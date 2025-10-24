#!/bin/bash
set -euo pipefail

touch errors.log

find Content.Tests/DMProject/Tests -type f -name "*.dm" | while read -r file; do
	echo "Running dm.sh on $file"
	tools/ci/dm.sh -DBYOND_UNIT_TEST=$file Content.Tests/DMProject/environment.dme  
	echo "Running $file"
	DreamDaemon Content.Tests/DMProject/environment.dmb -once -close -trusted -verbose -invisible
done

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
