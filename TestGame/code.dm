/turf
	icon = 'icons/turf.dmi'
	icon_state = "turf"

/turf/blue
	icon_state = "turf_blue"

/mob
	icon = 'icons/mob.dmi'
	icon_state = "mob"

	New()
		..()
		loc = locate(5, 5, 1)

	verb/tell_location()
		usr << "You are at ([x], [y], [z])"

	verb/say(message as text)
		var/list/viewers = viewers()

		for (var/mob/viewer in viewers)
			viewer << "[ckey] says: \"[message]\""

	verb/say_loud()
		var/msg = input("Please put the message you want to say loudly.", "Say Loud", "Hello!")
		world << "[ckey] says loudly: \"[msg]\""

	verb/move_up()
		step(src, UP)

	verb/move_down()
		step(src, DOWN)

	verb/roll_dice(dice as text)
		var/result = roll(dice)
		usr << "The total shown on the dice is: [result]"

	verb/test_alert()
		alert(usr, "Prepare to die.")
		usr << "prompt done"

	verb/input_num()
		var/v = input("A") as num
		usr << "you entered [v]"

	verb/test_browsersc()
		usr << browse_rsc('icons/mob.dmi')

	verb/test_browse()
		usr << browse({"
<!DOCTYPE html>
<html>
<head>
	<title>Foo</title>
	<style>
	body {
		background: red;
	}
	</style>
	<script>
	function foo(v) {
		document.getElementById("mark").innerHTML = v;
	}
	</script>
</head>
<body>
	<marquee id="mark">Honk</marquee>
	<a href="?honk=1">click me</a>
</body>
</html>"},"window=honk")

	verb/test_output()
		usr << output("help sec griffing me", "honk.browser:foo")

/*
	verb/test_mult()
		usr << browse({"
<!DOCTYPE html>
<html>
<head>
	<title>Foo</title>
	<script>
	function foo(v) {
		document.getElementById("mark").innerHTML = v;
	}
	</script>
</head>
<body>
	1
	<table>
		<tr>
			<td><input id="1a"></td>
			<td><input id="1d"></td>
			<td>0</td>
		</tr>
		<tr>
			<td><input id="1b"></td>
			<td><input id="1e"></td>
			<td>0</td>
		</tr>
		<tr>
			<td><input id="1c"></td>
			<td><input id="1f"></td>
			<td>1</td>
		</tr>
	</table>

	2
	<table>
		<tr>
			<td><input id="2a" value=1></td>
			<td><input id="2d" value=0></td>
			<td>0</td>
		</tr>
		<tr>
			<td><input id="2b" value=0></td>
			<td><input id="2e" value=1></td>
			<td>0</td>
		</tr>
		<tr>
			<td><input id="2c" value=0></td>
			<td><input id="2f" value=0></td>
			<td>1</td>
		</tr>
	</table>

	<input id="factor" value="0.5">

	result:
	<button onclick="calc()">foo</button>

	<!--
	<table>
		<tr>
			<td><span id="ra" value=1></td>
			<td><span id="rd" value=0></td>
			<td>0</td>
		</tr>
		<tr>
			<td><span id="rb" value=0></td>
			<td><span id="re" value=1></td>
			<td>0</td>
		</tr>
		<tr>
			<td><span id="rc" value=0></td>
			<td><span id="rf" value=0></td>
			<td>1</td>
		</tr>
	</table>
	-->

	<script>

	function calc()
	{
		var a1 = document.getElementById("1a").value;
		var b1 = document.getElementById("1b").value;
		var c1 = document.getElementById("1c").value;
		var d1 = document.getElementById("1d").value;
		var e1 = document.getElementById("1e").value;
		var f1 = document.getElementById("1f").value;
		var a2 = document.getElementById("2a").value;
		var b2 = document.getElementById("2b").value;
		var c2 = document.getElementById("2c").value;
		var d2 = document.getElementById("2d").value;
		var e2 = document.getElementById("2e").value;
		var f2 = document.getElementById("2f").value;
		var factor = document.getElementById("factor").value;

		window.location = "?calcMatrix=1" +
			"&1a=" + a1 +
			"&1b=" + b1 +
			"&1c=" + c1 +
			"&1d=" + d1 +
			"&1e=" + e1 +
			"&1f=" + f1 +
			"&2a=" + a2 +
			"&2b=" + b2 +
			"&2c=" + c2 +
			"&2d=" + d2 +
			"&2e=" + e2 +
			"&2f=" + f2 +
			"&factor=" + factor;
	}

	</script>

</body>
</html>
"}, "window=mul")

	verb/test_invert()
		var/matrix/m = matrix(1,2,3,4,5,6)
		for (var/i = 0; i < 100000; i++)
			m = m.Invert()

		usr << "[m.a] [m.d] 0"
		usr << "[m.b] [m.e] 0"
		usr << "[m.c] [m.f] 0"
*/


/mob/Stat()
	statpanel("Status", "CPU: [world.cpu]")
	stat("time", world.time)

/client/Click(var/atom/A)
	..()
	Move(A, get_dir(mob, A))

/world/New()
	..()
	world.log << "World loaded!"

/client/Topic(href,href_list,hsrc)
	usr << href
	usr << json_encode(href_list)

/*
	if (href_list["calcMatrix"])
		var a1 = text2num(href_list["1a"])
		var b1 = text2num(href_list["1b"])
		var c1 = text2num(href_list["1c"])
		var d1 = text2num(href_list["1d"])
		var e1 = text2num(href_list["1e"])
		var f1 = text2num(href_list["1f"])
		var a2 = text2num(href_list["2a"])
		var b2 = text2num(href_list["2b"])
		var c2 = text2num(href_list["2c"])
		var d2 = text2num(href_list["2d"])
		var e2 = text2num(href_list["2e"])
		var f2 = text2num(href_list["2f"])

		var/matrix/one = matrix(a1, b1, c1, d1, e1, f1)
		var/matrix/two = matrix(a2, b2, c2, d2, e2, f2)

		var/matrix/result = one.Interpolate(two, text2num(href_list["factor"]))

		usr << "[result.a] [result.d] 0"
		usr << "[result.b] [result.e] 0"
		usr << "[result.c] [result.f] 0"
*/
	..()
