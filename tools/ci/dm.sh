#!/bin/bash

retval=1

#windows
if [[ `uname` == MINGW* ]]
then
	dm=""

	if hash dm.exe 2>/dev/null
	then
		dm='dm.exe'
	elif [[ -a '/c/Program Files (x86)/BYOND/bin/dm.exe' ]]
	then
		dm='/c/Program Files (x86)/BYOND/bin/dm.exe'
	elif [[ -a '/c/Program Files/BYOND/bin/dm.exe' ]]
	then
		dm='/c/Program Files/BYOND/bin/dm.exe'
	fi

	if [[ $dm == "" ]]
	then
		echo "Couldn't find the DreamMaker executable, aborting."
		exit 3
	fi

	"$dm" "$@" 2>&1 | tee result.log
	retval=$?
	if ! grep '\- 0 errors, 0 warnings' result.log
	then
		retval=1 #hard fail, due to warnings or errors
	fi
else
	if hash DreamMaker 2>/dev/null
	then
		DreamMaker -max_errors 0 "$@" 2>&1 | tee result.log
		retval=$?
		if ! grep '\- 0 errors, 0 warnings' result.log
		then
			retval=1 #hard fail, due to warnings or errors
		fi
	else
		echo "Couldn't find the DreamMaker executable, aborting."
		exit 3
	fi
fi

rm $dmepath.dme

exit $retval
