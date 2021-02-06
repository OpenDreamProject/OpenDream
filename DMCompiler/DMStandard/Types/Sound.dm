/sound
	var/file = null
	var/repeat = 0
	var/wait = 0
	var/channel = 0
	var/volume = 100
	var/frequency = 0
	var/falloff = 1
	var/x
	var/y
	var/z

	proc/New(file, repeat=0, wait, channel, volume)
		src.file = file
		src.repeat = repeat
		if (wait != null) src.wait = wait
		if (channel != null) src.channel = channel
		if (volume != null) src.volume = volume