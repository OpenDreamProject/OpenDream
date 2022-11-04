using Robust.Shared.Serialization;
using Robust.Shared.ViewVariables;
using System;
using System.Collections.Generic;
using Robust.Shared.Maths;

namespace OpenDreamShared.Dream {
    /// DreamFilter is basically just an object describing type and vars so the client doesn't have to make a shaderinstance for shaders with the same params
    [Serializable, NetSerializable]
    public sealed class DreamFilter : IEquatable<DreamFilter> {
        /// Indicates this filter was used in the last render cycle, for shader caching purposes
        public bool used = false;
        [ViewVariables] public string? filter_type = null;
        /// Stores parameters, defaults, and mandatory flags for filters
        public static Dictionary<DreamPath, Dictionary<string, ValueTuple<Type, Boolean, Object>>> filterParameters; //filter type => dictionary(variable name => tuple(type, mandatory, default))
        public Dictionary<string, Object> parameters = new Dictionary<string, object>();

        static DreamFilter() {
            createVarEntry("alpha", "x", typeof(float), false, 0f);
            createVarEntry("alpha", "y", typeof(float), false, 0f);
            createVarEntry("alpha", "icon", typeof(Object), false, null); //icon type?
            createVarEntry("alpha", "render_source", typeof(string), false, null); //string that will require special processing
            createVarEntry("alpha", "flags", typeof(float), false, 0f);

            createVarEntry("angular_blur", "x", typeof(float), false, 0f);
            createVarEntry("angular_blur", "y", typeof(float), false, 0f);
            createVarEntry("angular_blur", "size", typeof(float), false, 1f);

            createVarEntry("bloom", "threshold", typeof(Color), false, Color.Black);
            createVarEntry("bloom", "size", typeof(float), false, 1f);
            createVarEntry("bloom", "offset", typeof(float), false, 1f);
            createVarEntry("bloom", "alpha", typeof(float), false, 255f);

            createVarEntry("blur", "size", typeof(float), false, 1f);

            createVarEntry("color", "color", typeof(Object), true, null); //color matrix TODO
            createVarEntry("color", "space", typeof(float), false, 0f); //default is FILTER_COLOR_RGB = 0

            createVarEntry("displace", "x", typeof(float), false, 0f);
            createVarEntry("displace", "y", typeof(float), false, 0f);
            createVarEntry("displace", "size", typeof(float), false, 1f);
            createVarEntry("displace", "icon", typeof(Object), false, null); //icon type?
            createVarEntry("displace", "render_source", typeof(string), false, null); //string that will require special processing

            createVarEntry("drop_shadow", "x", typeof(float), false, 1f);
            createVarEntry("drop_shadow", "y", typeof(float), false, -1f);
            createVarEntry("drop_shadow", "size", typeof(float), false, 1f);
            createVarEntry("drop_shadow", "offset", typeof(float), false, 0f);
            createVarEntry("drop_shadow", "color", typeof(Color), false, Color.Black.WithAlpha(128));

            createVarEntry("layer", "x", typeof(float), false, 0f);
            createVarEntry("layer", "y", typeof(float), false, 0f);
            createVarEntry("layer", "icon", typeof(Object), false, null); //icon type?
            createVarEntry("layer", "render_source", typeof(string), false, null); //string that will require special processing
            createVarEntry("layer", "flags", typeof(float), false, 0f); //default is FILTER_OVERLAY = 0
            createVarEntry("layer", "color", typeof(Color), false, Color.Black.WithAlpha(128)); //shit needs to be string or color matrix, because of course one has to be special
            createVarEntry("layer", "transform", typeof(Matrix3), false, Matrix3.Identity);
            createVarEntry("layer", "blend_mode", typeof(float), false, 0f);

            createVarEntry("motion_blur", "x", typeof(float), false, 0f);
            createVarEntry("motion_blur", "y", typeof(float), false, 0f);

            createVarEntry("outline", "size", typeof(float), false, 1f);
            createVarEntry("outline", "color", typeof(Color), false, Color.Black);
            createVarEntry("outline", "flags", typeof(float), false, 0f);

            createVarEntry("radial_blur", "x", typeof(float), false, 0f);
            createVarEntry("radial_blur", "y", typeof(float), false, 0f);
            createVarEntry("radial_blur", "size", typeof(float), false, 0.01f);

            createVarEntry("rays", "x", typeof(float), false, 0f);
            createVarEntry("rays", "y", typeof(float), false, 0f);
            createVarEntry("rays", "size", typeof(float), false, 16f); //defaults to half tile width
            createVarEntry("rays", "color", typeof(Color), false, Color.White);
            createVarEntry("rays", "offset", typeof(float), false, 0f);
            createVarEntry("rays", "density", typeof(float), false, 10f);
            createVarEntry("rays", "threshold", typeof(float), false, 0.5f);
            createVarEntry("rays", "factor", typeof(float), false, 0f);
            createVarEntry("rays", "flags", typeof(float), false, 1f); //defaults to FILTER_OVERLAY | FILTER_UNDERLAY

            createVarEntry("ripple", "x", typeof(float), false, 0f);
            createVarEntry("ripple", "y", typeof(float), false, 0f);
            createVarEntry("ripple", "size", typeof(float), false, 1f);
            createVarEntry("ripple", "repeat", typeof(float), false, 2f);
            createVarEntry("ripple", "radius", typeof(float), false, 0f);
            createVarEntry("ripple", "falloff", typeof(float), false, 1f);
            createVarEntry("ripple", "flags", typeof(float), false, 0f);

            createVarEntry("wave", "x", typeof(float), false, 0f);
            createVarEntry("wave", "y", typeof(float), false, 0f);
            createVarEntry("wave", "size", typeof(float), false, 1f);
            createVarEntry("wave", "offset", typeof(float), false, 0f);
            createVarEntry("wave", "flags", typeof(float), false, 0f);

            //no parameters for the greyscale filter
            filterParameters[DreamPath.Filter.AddToPath("greyscale")] = new Dictionary<string, ValueTuple<Type, bool, Object>>();

        }
        private static void createVarEntry(string filterType, string varName, Type varType, bool mandatory, Object defaultVal)
        {
            if(filterParameters == null)
                filterParameters = new();
            Dictionary<string, ValueTuple<Type, bool, Object>> varEntries;
            if(filterParameters.ContainsKey(DreamPath.Filter.AddToPath(filterType)))
                varEntries = filterParameters[DreamPath.Filter.AddToPath(filterType)];
            else
                varEntries = new();

            varEntries[varName] = new ValueTuple<Type, bool, object>(varType, mandatory, defaultVal);
            filterParameters[DreamPath.Filter.AddToPath(filterType)] = varEntries;
        }
        public DreamFilter() { }

        public override bool Equals(object obj) => obj is DreamFilter filter && Equals(filter);

        public bool Equals(DreamFilter filter) {
            if(filter.filter_type != filter_type) return false;
            if(this.parameters.Keys.Count != filter.parameters.Keys.Count) return false;
            foreach(string key in parameters.Keys)
            {
                if(!filter.parameters.ContainsKey(key)) return false;
                if(this.parameters[key] != filter.parameters[key]) return false;
            }
            return true;
        }

        public override int GetHashCode() {
            return ToString().GetHashCode();
        }

        public override string ToString()
        {
            string result = "[";
            if(filter_type != null) result += $"type: {filter_type}, ";
            foreach(string key in parameters.Keys)
                result += $"{key}: {parameters[key]}, ";
            result += "]";
            return result;
        }


    }


}
