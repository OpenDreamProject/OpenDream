using OpenDreamRuntime.Objects;
using OpenDreamShared.Dream;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
namespace OpenDreamRuntime.Procs.Native;

/// <summary>
/// A container of procs that act as helpers for a few native procs.
/// </summary>
internal static class DreamProcNativeHelpers {
    /// <summary>
    /// This is a helper proc for oview, view, orange, and range to do their strange iteration with.<br/>
    /// BYOND has a very strange, kinda-spiralling iteration pattern for the above procs, <br/>
    /// pretty much like this: <br/>
    /// 13 15 17 19 24 <br/>
    /// 12 03 05 08 23 <br/>
    /// 11 02 00 07 22 <br/>
    /// 10 01 04 06 21 <br/>
    /// 09 14 16 18 20 <br/>
    /// <br/>
    /// This helper attempts to mimic this iteration pattern.
    /// NOTE: THIS DOES NOT ITERATE OVER THE CENTRE. THAT'S THE PROC'S JOB.
    /// </summary>
    /// <remarks>
    /// This proc tries to handle the rectangular case, but I am not totally confident that it's up to parity.
    /// </remarks>
    /// <returns>Turfs, as <see cref="DreamObject"/>s, in the correct, parity order for the above procs.</returns>
    static public IEnumerable<DreamObject> MakeViewSpiral(DreamObject center, ViewRange distance) {
        var mapMgr = IoCManager.Resolve<IDreamMapManager>();
        int centerX, centerY, centerZ;
        try { // Me being visibly lazy here
            centerX = center.GetVariable("x").MustGetValueAsInteger();
            centerY = center.GetVariable("y").MustGetValueAsInteger();
            centerZ = center.GetVariable("z").MustGetValueAsInteger();
        } catch (InvalidCastException) {
            yield break;
        }
        int WidthRange = (distance.Width - 1) >> 1; // TODO: Make rectangles work.
        int HeightRange = (distance.Height - 1) >> 1;
        int donutCount = Math.Max(WidthRange, HeightRange); 
        for(int d = 1; d <= donutCount; d++) { // for each donut
            int sideLength = d + d + 1;
            //The left column
            {
                int leftColumnX = centerX - d;
                int startingLeftColumnY = centerY - d;
                for (int i = 0; i < sideLength; ++i) {
                    if (mapMgr.TryGetTurfAt((leftColumnX, startingLeftColumnY + i), centerZ, out var turf)) {
                        yield return turf;
                    }
                }
            }
            //The criss-cross-apple-sauce
            {
                int crissCrossLength = sideLength - 2;
                int startingCrossX = centerX - d + 1;
                for(int i = 0; i < crissCrossLength; ++i) {
                    //the criss
                    if (mapMgr.TryGetTurfAt((startingCrossX+i, centerY - d), centerZ, out var crissTurf)) {
                        yield return crissTurf;
                    }
                    //the cross
                    if (mapMgr.TryGetTurfAt((startingCrossX + i, centerY + d), centerZ, out var crossTurf)) {
                        yield return crossTurf;
                    }
                }
            }
            //The right column
            {
                int rightColumnX = centerX + d;
                int startingRightColumnY = centerY - d;
                for (int i = 0; i < sideLength; ++i) {
                    if (mapMgr.TryGetTurfAt((rightColumnX, startingRightColumnY + i), centerZ, out var turf)) {
                        yield return turf;
                    }
                }
            }
        }
        yield break;
    }

    /// <summary>
    /// Resolves the arguments of view, oview, orange, and range procs, <br/>
    /// Since it's rather convoluted for a few reasons.
    /// </summary>
    /// <remarks>
    /// Arguments are optional and can be passed in any order.
    /// If a range argument is passed, like "11x4", then THAT is what we have to deal with.
    /// </remarks>
    /// <returns>The center (which may not be the turf), the distance along the x-axis, and the distance along the y-axis to iterate.</returns>
    static public (DreamObject, ViewRange) ResolveViewArguments(DreamObject usr, DreamProcArguments arguments) {
        if(arguments.ArgumentCount == 0) {
            return (usr, new ViewRange(5,5));
        }
        ViewRange range = new ViewRange(5,5);
        DreamObject center = usr;

        var args = arguments.GetAllArguments();
        foreach(var arg in args) {
            if(arg.TryGetValueAsDreamObject(out var centerObject)) {
                center = centerObject;
            } else if(arg.TryGetValueAsInteger(out int distValue)) {
                range = new ViewRange(distValue);
            } else if(arg.TryGetValueAsString(out var distString)) {
                range = new ViewRange(distString);
            } else {
                throw new Exception($"Invalid argument: {arg}");
            }
        }
            
        return (center, range);
    }

    /// <summary>
    /// Determines whether the first parameter is "visible" to the second parameter, according to BYOND's various rules on visibility.
    /// </summary>
    /// <remarks>
    /// <see langword="TODO:"/> This proc is DEFINITELY incomplete. <br/>
    /// </remarks>
    /// <returns>True if observer can see obj. False if not.</returns>
    static public bool IsObjectVisible(DreamObject obj, DreamObject observer) {
        if(obj == observer) // Not proven to be true, but makes intuitive sense.
            return true;

        if(!obj.IsSubtypeOf(DreamProcNativeRoot.ObjectTree.Atom)) {
            return false; // Can't see datums and nulls n stuff, I THINK???
        }
            
        // https://www.byond.com/docs/ref/#/atom/var/invisibility
        if (obj.TryGetVariable("invisiblity", out DreamValue invisibility)) {
            if(invisibility.TryGetValueAsFloat(out float invisibilityValue)) {
                // Ref says: "A value of 101 is absolutely invisible, no matter what"
                if(invisibilityValue == 101f) {
                    return false;
                }
                // Ref:
                // "This determines the object's level of invisibility."
                // "The corresponding mob variable see_invisible controls the maximum level of invisibility that the mob may see."
                if(observer.IsSubtypeOf(DreamProcNativeRoot.ObjectTree.Mob)) {
                    if(observer.TryGetVariable("see_invisible",out var maxInvisibility)) {
                        if(maxInvisibility.TryGetValueAsFloat(out float maxInvisibilityValue)) {
                            if(maxInvisibilityValue < invisibilityValue) {
                                return false;
                            }
                        }
                    }
                }
            }
        }
        return true;
    }

    /// <summary>
    /// Takes in a DreamList and tries to interpret it as representing a color matrix, for use in filters and /atom.color.
    /// </summary>
    /// <remarks>
    /// There are MANY different ways to initialize a color matrix, only some of which even appear in our targets: <br/>
    /// list(rr,rg,rb, gr,gg,gb, br,bg,bb) <br/>
    /// list(rr, rg, rb, gr, gg, gb, br, bg, bb, cr, cg, cb) <br/>
    /// list(rr, rg, rb, ra, gr, gg, gb, ga, br, bg, bb, ba, ar, ag, ab, aa) <br/>
    /// list(rr, rg, rb, ra, gr, gg, gb, ga, br, bg, bb, ba, ar, ag, ab, aa, cr, cg, cb, ca) <br/>
    /// list(rgb() or null, rgb() or null, rgb() or null, rgb() or null, rgb() or null) <br/>
    /// </remarks>
    /// <returns>True if the list was successfully parsed, false if not.</returns>
    public static bool TryParseColorMatrix(DreamList list, out ColorMatrix matrix) {
        matrix = ColorMatrix.Identity;
        var listArray = list.GetValues();
        try {
            switch (list.GetLength()) {
                case 0:
                    return true; // Just return the identity matrix. NOTE: Not sure if *exactly* parity.
                case 1: // 0 to 5 is the rgb() string spam: 
                case 2: // list(rgb() or null, rgb() or null, rgb() or null, rgb() or null, rgb() or null)
                case 3:
                case 4:
                case 5:
                    for (var i = 0; i < listArray.Count && i < 5; ++i) {
                        var listValue = listArray[i];
                        if (listValue.TryGetValueAsString(out var RGBString)) {
                            if (ColorHelpers.TryParseColor(RGBString, out var color, defaultAlpha: "00")) {
                                matrix.SetRow(i, color);
                            }
                        }
                    }
                    return true;
                case 9: // list(rr,rg,rb, gr,gg,gb, br,bg,bb)
                    for (var row = 0; row < listArray.Count && row < 3; ++row) {
                        var offset = row * 3;
                        matrix.SetRow(row, listArray[offset].MustGetValueAsFloat(),
                                           listArray[offset + 1].MustGetValueAsFloat(),
                                           listArray[offset + 2].MustGetValueAsFloat(),
                                           0f);
                    }
                    return true;
                case 12: // list(rr,rg,rb, gr,gg,gb, br,bg,bb, cr,cg,cb)
                    for (var row = 0; row < listArray.Count && row < 3; ++row) {
                        var offset = row * 3;
                        matrix.SetRow(row, listArray[offset].MustGetValueAsFloat(),
                                           listArray[offset + 1].MustGetValueAsFloat(),
                                           listArray[offset + 2].MustGetValueAsFloat(),
                                           0f);
                    }
                    //We skip over the alpha row in this one. It's kinda wonky.
                    matrix.SetRow(4, listArray[9].MustGetValueAsFloat(),
                                     listArray[10].MustGetValueAsFloat(),
                                     listArray[11].MustGetValueAsFloat(),
                                     0f);
                    return true;

                case 16: // list(rr, rg, rb, ra, gr, gg, gb, ga, br, bg, bb, ba, ar, ag, ab, aa)
                    for (var row = 0; row < listArray.Count && row < 4; ++row) {
                        var offset = row * 4;
                        matrix.SetRow(row, listArray[offset].MustGetValueAsFloat(),
                                           listArray[offset + 1].MustGetValueAsFloat(),
                                           listArray[offset + 2].MustGetValueAsFloat(),
                                           listArray[offset + 2].MustGetValueAsFloat());
                    }
                    return true;
                case 20: // list(rr, rg, rb, ra, gr, gg, gb, ga, br, bg, bb, ba, ar, ag, ab, aa, cr, cg, cb, ca)
                    for (var row = 0; row < listArray.Count && row < 5; ++row) {
                        var offset = row * 4;
                        matrix.SetRow(row, listArray[offset].MustGetValueAsFloat(),
                                           listArray[offset + 1].MustGetValueAsFloat(),
                                           listArray[offset + 2].MustGetValueAsFloat(),
                                           listArray[offset + 2].MustGetValueAsFloat());
                    }
                    return true;
                default:
                    return false;
            }
        } catch(InvalidCastException) { // Lets us use MustGet more liberally in here.
            return false;
        } catch(IndexOutOfRangeException) { // Trying to access stuff that should be there but isn't is also pretty catchable for us, here.
            return false;
        }
    }
}
