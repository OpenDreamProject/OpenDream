using OpenDreamServer.Dream.Objects;
using System;
using System.Collections.Generic;
using System.Reflection;

namespace OpenDreamServer.Dream.Procs {
    class DreamProc {
        public DreamProc SuperProc = null;

        private Func<DreamObject, DreamObject, DreamProcArguments, DreamValue> _runAction;
        private List<string> _argumentNames;
        private Dictionary<string, DreamValue> _defaultArgumentValues;

        public DreamProc(byte[] bytecode, List<string> argumentNames = null) {
            _argumentNames = (argumentNames != null) ? argumentNames : new List<string>();
            _runAction = (DreamObject instance, DreamObject usr, DreamProcArguments arguments) => {
                return new DreamProcInterpreter(this, bytecode).Run(instance, usr, SuperProc, arguments, _argumentNames);
            };
        }

        public DreamProc(Func<DreamObject, DreamObject, DreamProcArguments, DreamValue> nativeProc) {
            _runAction = (DreamObject instance, DreamObject usr, DreamProcArguments arguments) => {
                for (int i = 0; i < _argumentNames.Count; i++) {
                    string argumentName = _argumentNames[i];

                    if (arguments.GetArgument(i, argumentName).Value == null) {
                        if (_defaultArgumentValues != null && _defaultArgumentValues.TryGetValue(argumentName, out DreamValue defaultValue)) {
                            arguments.NamedArguments.Add(argumentName, defaultValue);
                        }
                    }
                }

                return nativeProc(instance, usr, arguments);
            };

            _argumentNames = new List<string>();
            _defaultArgumentValues = null;

            List<Attribute> attributes = new(nativeProc.GetInvocationList()[0].Method.GetCustomAttributes());
            List<Attribute> parameterAttributes = attributes.FindAll(attribute => attribute is DreamProcParameterAttribute);
            foreach (Attribute attribute in parameterAttributes) {
                DreamProcParameterAttribute parameterAttribute = (DreamProcParameterAttribute)attribute;

                _argumentNames.Add(parameterAttribute.Name);
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
