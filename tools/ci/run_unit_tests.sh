#!/bin/bash
set -euo pipefail

touch errors.log
base="Content.Tests/DMProject/environment.dme"
testsfailed=0

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
			testsfailed=1
			continue		
		fi
	fi

	echo "Running $relative"
	touch errors.log
	DreamDaemon Content.Tests/DMProject/environment.dmb -once -close -trusted -verbose -invisible
		
	if [[ -s "errors.log" && $first_line == "// RUNTIME ERROR"* ]]	then #expected runtime error, should compile but then fail to run
		echo "Expected runtime error, test passed"
		rm errors.log
		continue
	else
		echo "Errors detected!"
		sed -i '/^[[:space:]]*$/d' ./errors.log
		cat errors.log
		echo "TEST FAILED: $relative"
		testsfailed=1
		rm errors.log
		continue
	fi

exit $testsfailed
done

