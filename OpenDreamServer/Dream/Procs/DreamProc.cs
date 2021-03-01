using OpenDreamServer.Dream.Objects;
using OpenDreamShared.Dream.Procs;
using System;
using System.Collections.Generic;
using System.Reflection;

namespace OpenDreamServer.Dream.Procs {
    class DreamProc {
        public DreamProc SuperProc = null;
        public List<string> ArgumentNames;
        public List<DMValueType> ArgumentTypes;

        private Func<DreamObject, DreamObject, DreamProcArguments, DreamValue> _runAction;
        private Dictionary<string, DreamValue> _defaultArgumentValues;

        public DreamProc(byte[] bytecode, List<string> argumentNames = null, List<DMValueType> argumentTypes = null) {
            ArgumentNames = (argumentNames != null) ? argumentNames : new List<string>();
            ArgumentTypes = (argumentTypes != null) ? argumentTypes : new List<DMValueType>();
            _runAction = (DreamObject instance, DreamObject usr, DreamProcArguments arguments) => {
                return new DreamProcInterpreter(this, bytecode).Run(instance, usr, SuperProc, arguments, ArgumentNames);
            };
        }

        public DreamProc(Func<DreamObject, DreamObject, DreamProcArguments, DreamValue> nativeProc) {
            _runAction = (DreamObject instance, DreamObject usr, DreamProcArguments arguments) => {
                for (int i = 0; i < ArgumentNames.Count; i++) {
                    string argumentName = ArgumentNames[i];

                    if (arguments.GetArgument(i, argumentName).Value == null) {
                        if (_defaultArgumentValues != null && _defaultArgumentValues.TryGetValue(argumentName, out DreamValue defaultValue)) {
                            arguments.NamedArguments.Add(argumentName, defaultValue);
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
