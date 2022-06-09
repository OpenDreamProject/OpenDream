/icon
	parent_type = /datum
	var/icon

	New(icon, icon_state, dir, frame, moving)

	proc/Blend(icon, function = ICON_ADD, x = 1, y = 1)
		set opendream_unimplemented = TRUE
		CRASH("/icon.Blend() is not implemented")

	proc/Crop(x1, y1, x2, y2)
		set opendream_unimplemented = TRUE
		CRASH("/icon.Crop() is not implemented")

	proc/DrawBox(rgb, x1, y1, x2 = x1, y2 = y1)
		set opendream_unimplemented = TRUE
		CRASH("/icon.DrawBox() is not implemented")

	proc/Flip(dir)
		set opendream_unimplemented = TRUE
		CRASH("/icon.Flip() is not implemented")

	proc/GetPixel(x, y, icon_state, dir = 0, frame = 0, moving = -1)
		set opendream_unimplemented = TRUE
		CRASH("/icon.GetPixel() is not implemented")

	proc/Height()

	proc/IconStates(mode = 0)
		return icon_states(src, mode)

	proc/Insert(new_icon, icon_state, dir, frame, moving, delay)

	proc/MapColors(...)
		set opendream_unimplemented = TRUE
		CRASH("/icon.MapColors() is not implemented")

	proc/Scale(width, height)
		set opendream_unimplemented = TRUE
		CRASH("/icon.Scale() is not implemented")

	proc/SetIntensity(r, g = r, b = r)
		set opendream_unimplemented = TRUE
		CRASH("/icon.SetIntensity() is not implemented")

	proc/Shift(dir, offset, wrap = 0)
		set opendream_unimplemented = TRUE
		CRASH("/icon.Shift() is not implemented")

	proc/SwapColor(old_rgb, new_rgb)
		set opendream_unimplemented = TRUE
		CRASH("/icon.SwapColor() is not implemented")

	proc/Turn(angle)
		set opendream_unimplemented = TRUE
		CRASH("/icon.Turn() is not implemented")

	proc/Width()

proc/icon(...)
	return new /icon(arglist(args))
