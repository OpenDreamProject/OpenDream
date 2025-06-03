// Keep this in line with List.dm

/alist
	var/len
	var/const/type = /alist

	// The only difference from /list's definition as far as I can tell
	// It takes an arglist of key/value pairs instead of a size arg
	proc/New(items)

	proc/Add(Item1)
	proc/Copy(Start = 1, End = 0)
	proc/Cut(Start = 1, End = 0)
	proc/Find(Elem, Start = 1, End = 0)
	proc/Insert(Index, Item1)
	proc/Join(Glue as text|null, Start = 1 as num, End = 0 as num) as text
	proc/Remove(Item1)
	proc/RemoveAll(Item1)
	proc/Swap(Index1, Index2)
	proc/Splice(Start = 1 as num, End = 0 as num, Item1, ...) as null
