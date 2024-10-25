/list
	var/len
	var/const/type = /list

	proc/New(Size)

	proc/Add(Item1) as null
	proc/Copy(Start = 1, End = 0) as /list
	proc/Cut(Start = 1, End = 0) as num
	proc/Find(Elem, Start = 1, End = 0) as num
	proc/Insert(Index, Item1) as num
	proc/Join(Glue as text|null, Start = 1 as num, End = 0 as num) as text
	proc/Remove(Item1) as num
	proc/RemoveAll(Item1) as num
	proc/Swap(Index1, Index2) as null

	proc/Splice(Start=1,End=0, ...) as null
		set opendream_unimplemented = TRUE
