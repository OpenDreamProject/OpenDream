#!/bin/bash
set -euo pipefail

touch errors.log
base="Content.Tests/DMProject/environment.dme"

find Content.Tests/DMProject/Tests -type f -name "*.dm" | while read -r file; do
	first_line=$(head -n 1 "$file" || echo "")
	second_line=$((head -n 2 "$file" | tail -n 1) || echo "")
	relative=$(realpath --relative-to="$(dirname "$base")" "$file")
	if [[ $first_line == "// NOBYOND"* || $second_line == "// NOBYOND"* ]]; then
		#skip this one, it won't work in byond
		echo "Skipping $relative due to NOBYOND mark"
		continue
	fi

	echo "Compiling $relative"
	if ! tools/ci/dm.sh -I\"$relative\" $base; then
		if [[ $first_line == "// COMPILE ERROR"* ]] then	#expected compile error, should fail to compile
			echo "Expected compile failure, test passed"
			continue
		else
			echo "TEST FAILED: $relative"
			continue		
		fi
	fi

	echo "Running $relative"
	if ! DreamDaemon Content.Tests/DMProject/environment.dmb -once -close -trusted -verbose -invisible; then
		if [[ $first_line == "// RUNTIME ERROR"* ]]	then #expected runtime error, should compile but then fail to run
			echo "Expected runtime error, test passed"
			continue
		else
			echo "TEST FAILED: $relative"
			continue
		fi
	fi
	
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
