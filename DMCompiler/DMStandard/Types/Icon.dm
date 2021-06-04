/icon
	parent_type = /datum

	New(icon, icon_state, dir, frame, moving)

	proc/Blend(icon, function = ICON_ADD, x = 1, y = 1)
		CRASH("/icon.Blend() is not implemented")
	
	proc/Crop(x1, y1, x2, y2)
		CRASH("/icon.Crop() is not implemented")

	proc/DrawBox(rgb, x1, y1, x2 = x1, y2 = y1)
		CRASH("/icon.DrawBox() is not implemented")

	proc/Flip(dir)
		CRASH("/icon.Flip() is not implemented")
	
	proc/GetPixel(x, y, icon_state, dir = 0, frame = 0, moving = -1)
		CRASH("/icon.GetPixel() is not implemented")

	proc/Height()
		CRASH("/icon.Height() is not implemented")

	proc/IconStates(mode = 0)
		CRASH("/icon.IconStates() is not implemented")

	proc/Insert(new_icon, icon_state, dir, frame, moving, delay)
		CRASH("/icon.Insert() is not implemented")

	proc/MapColors(...)
		CRASH("/icon.MapColors() is not implemented")

	proc/Scale(width, height)
		CRASH("/icon.Scale() is not implemented")

	proc/SetIntensity(r, g = r, b = r)
		CRASH("/icon.SetIntensity() is not implemented")

	proc/Shift(dir, offset, wrap = 0)
		CRASH("/icon.Shift() is not implemented")

	proc/SwapColor(old_rgb, new_rgb)
		CRASH("/icon.SwapColor() is not implemented")

	proc/Turn(angle)
		CRASH("/icon.Turn() is not implemented")

	proc/Width()
		CRASH("/icon.Width() is not implemented")

proc/icon(...)
	return new /icon(arglist(args))