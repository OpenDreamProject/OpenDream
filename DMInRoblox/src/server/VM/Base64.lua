--Taken (and slightly modified) from https://gist.github.com/Reselim/40d62b17d138cc74335a1b0709e19ce2

local FILLER_CHARACTER = 61

local alphabet = {}
local indexes = {}

for index = 65, 90 do table.insert(alphabet, index) end -- A-Z
for index = 97, 122 do table.insert(alphabet, index) end -- a-z
for index = 48, 57 do table.insert(alphabet, index) end -- 0-9

table.insert(alphabet, 43) -- +
table.insert(alphabet, 47) -- /

for index, character in pairs(alphabet) do
	indexes[character] = index
end

local function buildString(values)
	local output = {}

	for index = 1, #values, 4096 do
		table.insert(output, string.char(
			unpack(values, index, math.min(index + 4096 - 1, #values))
			))
	end

	return table.concat(output, "")
end

local Base64 = {}

function Base64.encode(input)
	local output = {}

	for index = 1, #input, 3 do
		local C1, C2, C3 = string.byte(input, index, index + 2)

		local A = bit32.rshift(C1, 2)
		local B = bit32.lshift(bit32.band(C1, 3), 4) + bit32.rshift(C2 or 0, 4)
		local C = bit32.lshift(bit32.band(C2 or 0, 15), 2) + bit32.rshift(C3 or 0, 6)
		local D = bit32.band(C3 or 0, 63)

		output[#output + 1] = alphabet[A + 1]
		output[#output + 1] = alphabet[B + 1]
		output[#output + 1] = C2 and alphabet[C + 1] or FILLER_CHARACTER
		output[#output + 1] = C3 and alphabet[D + 1] or FILLER_CHARACTER
	end

	return buildString(output)
end

function Base64.decode(input)
	local output = {}

	for index = 1, #input, 4 do
		local C1, C2, C3, C4 = string.byte(input, index, index + 3)

		local I1 = indexes[C1] - 1
		local I2 = indexes[C2] - 1
		local I3 = (indexes[C3] or 1) - 1
		local I4 = (indexes[C4] or 1) - 1

		local A = bit32.lshift(I1, 2) + bit32.rshift(I2, 4)
		local B = bit32.lshift(bit32.band(I2, 15), 4) + bit32.rshift(I3, 2)
		local C = bit32.lshift(bit32.band(I3, 3), 6) + I4

		output[#output + 1] = A
		if C3 ~= FILLER_CHARACTER then output[#output + 1] = B end
		if C4 ~= FILLER_CHARACTER then output[#output + 1] = C end
	end

	return output
end

return Base64