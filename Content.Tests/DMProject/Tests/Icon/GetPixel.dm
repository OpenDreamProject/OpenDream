/proc/RunTest()
	var/icon/I = icon('hanoi.dmi')
	// ASSERT(I.GetPixel(5,5) == "#ff0000")
	var/list/target_pixels = list("#e8083f", "#ff0000", "#6aee1d", "#f1b004", "#ffa308", "#ff34e2", "#6aeeff",\
			"#f4f14d", "#fbed01", "#757121", "#c07760", "#cc7d66", "#c98164", "#5fed26", "#43fc13", "#43fc13", "#43fc13",\
			"#43fc13",	"#43fc13", "#48fa16", "#bf885e", "#625230", "#4d4b26", "#4c5234", "#09f5f3", "#00ffff", "#00ffff","#f44dfb",\
			"#2c14ff", "#001bf9", "#00ef58", "#f74d04")
	for(var/x in 1 to 32)
		world.log << I.GetPixel(x,16,"target")
		ASSERT(I.GetPixel(x,16,"target") == target_pixels[x])