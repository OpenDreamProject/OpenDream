/list
	var/len
	var/const/type = /list

	proc/New(Size)

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
