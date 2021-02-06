/list
	var/len

	proc/New(Size)
		if (Size != null) len = Size

	proc/Add(Item1)
	proc/Copy(Start = 1, End = 0)
	proc/Cut(Start = 1, End = 0)
	proc/Find(Elem, Start = 1, End = 0)
	proc/Insert(Index, Item1)
	proc/Join(Glue, Start = 1, End = 0)
	proc/Remove(Item1)
	proc/Swap(Index1, Index2)