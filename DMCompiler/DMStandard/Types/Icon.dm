/icon
	parent_type = /datum
	var/icon

	New(icon, icon_state, dir, frame, moving)

	proc/Blend(icon, function = ICON_ADD, x = 1, y = 1)

	proc/Crop(x1, y1, x2, y2)
		set opendream_unimplemented = TRUE

	proc/DrawBox(rgb as color|null, x1 as num, y1 as num, x2 = x1 as num|null, y2 = y1 as num|null)

	proc/Flip(dir)
		set opendream_unimplemented = TRUE

	proc/GetPixel(x, y, icon_state, dir = 0, frame = 0, moving = -1)

	proc/Height()

	proc/IconStates(mode = 0)
		return icon_states(src, mode)

	proc/Insert(new_icon, icon_state, dir, frame, moving, delay)

	proc/MapColors(...)
		set opendream_unimplemented = TRUE

	proc/Scale(width as num, height as num)

	proc/SetIntensity(r as num, g = r as num|null, b = r as num|null)

	proc/Shift(dir, offset, wrap = 0)
		set opendream_unimplemented = TRUE

	proc/SwapColor(old_rgb, new_rgb)
		set opendream_unimplemented = TRUE

	proc/Turn(angle)
		set opendream_unimplemented = TRUE

	proc/Width()

proc/icon(icon, icon_state, dir, frame, moving)
	return new /icon(icon, icon_state, dir, frame, moving)
