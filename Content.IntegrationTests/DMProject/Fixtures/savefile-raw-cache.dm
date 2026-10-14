/proc/raw_cache_seed(path)
    var/savefile/F = new(path)
    F["value"] << 73
    F = null
    return 17

/proc/raw_cache_open(path)
    return new/savefile(path)

/proc/raw_cache_read(savefile/F)
    var/value
    F["value"] >> value
    return value

/proc/raw_cache_reject(path)
    try
        var/savefile/F = new(path)
        F = null
        return 0
    catch(var/exception/failure)
        if(!findtext(failure.name, "Error parsing savefile"))
            throw failure
        return 17

/proc/raw_cache_deleted(path)
    var/savefile/F = new(path)
    var/value
    F["value"] >> value
    if(!fdel(path))
        throw "RAW_CACHE_DELETE_FAILED"
    F = null
    return value

/proc/raw_cache_file(path)
    return file(path)

/proc/raw_cache_text(path)
    return file2text(path)
