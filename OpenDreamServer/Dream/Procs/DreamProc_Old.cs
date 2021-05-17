using OpenDreamServer.Dream.Objects;
using OpenDreamShared.Dream.Procs;
using System;
using System.Collections.Generic;
using System.Reflection;

namespace OpenDreamServer.Dream.Procs {
    class DreamProc_Old {
        public DreamProc_Old SuperProc = null;
        public List<string> ArgumentNames;
        public List<DMValueType> ArgumentTypes;

        private Func<DreamObject, DreamObject, DreamProcArguments, DreamValue> _runAction;
        private Dictionary<string, DreamValue> _defaultArgumentValues;

        public DreamProc_Old(byte[] bytecode, List<string> argumentNames = null, List<DMValueType> argumentTypes = null) {
            ArgumentNames = (argumentNames != null) ? argumentNames : new List<string>();
            ArgumentTypes = (argumentTypes != null) ? argumentTypes : new List<DMValueType>();
            _runAction = (DreamObject instance, DreamObject usr, DreamProcArguments arguments) => {
                return new DreamProcInterpreter(this, instance, usr, arguments, bytecode).Run();
            };
        }

        public DreamProc_Old(Func<DreamObject, DreamObject, DreamProcArguments, DreamValue> nativeProc) {
            _runAction = (DreamObject instance, DreamObject usr, DreamProcArguments arguments) => {
                if (_defaultArgumentValues != null) {
                    foreach (KeyValuePair<string, DreamValue> defaultArgumentValue in _defaultArgumentValues) {
                        int argumentIndex = ArgumentNames.IndexOf(defaultArgumentValue.Key);

                        if (arguments.GetArgument(argumentIndex, defaultArgumentValue.Key) == DreamValue.Null) {
                            arguments.NamedArguments.Add(defaultArgumentValue.Key, defaultArgumentValue.Value);
                        }
                    }
                }

                return nativeProc(instance, usr, arguments);
            };

            ArgumentNames = new List<string>();
            _defaultArgumentValues = null;

            List<Attribute> attributes = new(nativeProc.GetInvocationList()[0].Method.GetCustomAttributes());
            List<Attribute> parameterAttributes = attributes.FindAll(attribute => attribute is DreamProcParameterAttribute);
            foreach (Attribute attribute in parameterAttributes) {
                DreamProcParameterAttribute parameterAttribute = (DreamProcParameterAttribute)attribute;

                ArgumentNames.Add(parameterAttribute.Name);
                if (parameterAttribute.DefaultValue != default) {
                    if (_defaultArgumentValues == null) _defaultArgumentValues = new Dictionary<string, DreamValue>();

                    _defaultArgumentValues.Add(parameterAttribute.Name, new DreamValue(parameterAttribute.DefaultValue));
                }
            }
        }

        public DreamValue Run(DreamObject instance, DreamProcArguments arguments, DreamObject usr = null) {
            return _runAction(instance, usr, arguments);
        }
    }
}
