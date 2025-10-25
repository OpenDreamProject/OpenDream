#!/bin/bash
set -euo pipefail

touch summary.log
base="Content.Tests/DMProject/environment.dme"
testsfailed=0
byondcrashes=0
testspassed=0

while read -r file; do
	first_line=$(head -n 1 "$file" || echo "")
	second_line=$((head -n 2 "$file" | tail -n 1) || echo "")
	relative=$(realpath --relative-to="$(dirname "$base")" "$file")
	if [[ $first_line == "// NOBYOND"* || $second_line == "// NOBYOND"* ]]; then
		#skip this one, it won't work in byond
		echo "Skipping $relative due to NOBYOND mark"
		continue
	fi
	if [[ $first_line == "// IGNORE"* ]]; then
		#skip this one, it won't work in byond
		echo "Skipping $relative due to IGNORE mark"
		continue
	fi
	

	echo "Compiling $relative"
	if ! tools/ci/dm.sh -I\"$relative\" $base; then
		if [[ $first_line == "// COMPILE ERROR"* || $first_line == "//COMPILE ERROR"* ]] then	#expected compile error, should fail to compile
			echo "Expected compile failure, test passed"
			testspassed=$((testspassed + 1))
			continue
		else
			echo "TEST FAILED: $relative"
			echo "TEST FAILED: $relative" >> summary.log
			testsfailed=$((testsfailed + 1))
			continue		
		fi
	else
		if [[ $first_line == "// COMPILE ERROR"* || $first_line == "//COMPILE ERROR"* ]] then	#expected compile error, should fail to compile
			echo "TEST FAILED: Expected compile failure"
			echo "TEST FAILED: $relative" >> summary.log
			testsfailed=$((testsfailed + 1))
			continue
		fi
	fi

	echo "Running $relative"
	touch Content.Tests/DMProject/errors.log
	if ! DreamDaemon Content.Tests/DMProject/environment.dmb -once -close -trusted -verbose -invisible; then
		echo "TEST FAILED: BYOND CRASHED!"
		echo "TEST FAILED: $relative" >> summary.log
		byondcrashes=$((byondcrashes+1))
		sed -i '/^[[:space:]]*$/d' Content.Tests/DMProject/errors.log
		cat Content.Tests/DMProject/errors.log
		rm Content.Tests/DMProject/errors.log
	fi
	if [ -s "Content.Tests/DMProject/errors.log" ]; then
		if [[ $first_line == "// RUNTIME ERROR"* || $first_line == "//RUNTIME ERROR"* ]]	then #expected runtime error, should compile but then fail to run
			echo "Expected runtime error, test passed"
			rm Content.Tests/DMProject/errors.log
			testspassed=$((testspassed + 1))
			continue
		else
			echo "Errors detected!"
			sed -i '/^[[:space:]]*$/d' Content.Tests/DMProject/errors.log
			cat Content.Tests/DMProject/errors.log
			echo "TEST FAILED: $relative"
			rm Content.Tests/DMProject/errors.log
			echo "TEST FAILED: $relative" >> summary.log
			testsfailed=$((testsfailed + 1))
			continue
		fi
	else
		echo "Test passed: $relative"
		testspassed=$((testspassed + 1))
	fi
done < <(find Content.Tests/DMProject/Tests -type f -name "*.dm")


echo "--------------------------------------------------------------------------------"
echo "Test Summary"
echo "--------------------------------------------------------------------------------"
echo "passed: $testspassed, failed: $testsfailed, BYOND crashes: $byondcrashes"
echo "--------------------------------------------------------------------------------"
echo "failed tests:"
cat summary.log
exit $testsfailed

