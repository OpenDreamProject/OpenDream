#!/bin/bash
set -euo pipefail

touch errors.log
base="Content.Tests/DMProject/environment.dme"

find Content.Tests/DMProject/Tests -type f -name "*.dm" | while read -r file; do
	

	relative=$(realpath --relative-to="$(dirname "$base")" "$file")
	echo "Running dm.sh on $relative"
	tools/ci/dm.sh "-DBYOND_UNIT_TEST=\"$relative\"" $base
	echo "Running $relative"
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
