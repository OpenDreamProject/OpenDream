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
	else
		if [[ $first_line == "// COMPILE ERROR"* ]] then	#expected compile error, should fail to compile
			echo "TEST FAILED: Expected compile failure"
			testsfailed=1
		fi
	fi

	echo "Running $relative"
	touch Content.Tests/DMProject/errors.log
	if ! DreamDaemon Content.Tests/DMProject/environment.dmb -once -close -trusted -verbose -invisible; then
		echo "TEST FAILED: BYOND CRASHED!"
		testsfailed=1
		sed -i '/^[[:space:]]*$/d' Content.Tests/DMProject/errors.log
		cat Content.Tests/DMProject/errors.log
		rm Content.Tests/DMProject/errors.log
	fi
	if [ -s "Content.Tests/DMProject/errors.log" ]; then
		if [[ $first_line == "// RUNTIME ERROR"* ]]	then #expected runtime error, should compile but then fail to run
			echo "Expected runtime error, test passed"
			rm Content.Tests/DMProject/errors.log
			continue
		else
			echo "Errors detected!"
			sed -i '/^[[:space:]]*$/d' Content.Tests/DMProject/errors.log
			cat Content.Tests/DMProject/errors.log
			echo "TEST FAILED: $relative"
			testsfailed=1
			rm Content.Tests/DMProject/errors.log
			continue
		fi
	else
		echo "Test passed: $relative"
	fi
done

exit $testsfailed

