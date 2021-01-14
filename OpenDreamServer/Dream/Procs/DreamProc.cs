using OpenDreamServer.Dream.Objects;
using System;
using System.Collections.Generic;
using System.Reflection;

namespace OpenDreamServer.Dream.Procs {
    class DreamProc {
        public DreamProc SuperProc = null;

        private Func<DreamProcScope, DreamProcArguments, DreamValue> _runAction;
        private List<string> _argumentNames;
        private Dictionary<string, DreamValue> _defaultArgumentValues;

        public DreamProc(byte[] bytecode, List<string> argumentNames) {
            _argumentNames = argumentNames;
            _runAction = (DreamProcScope scope, DreamProcArguments arguments) => {
                return new DreamProcInterpreter(this, bytecode).Run(scope, arguments);
            };
        }

        public DreamProc(Func<DreamProcScope, DreamProcArguments, DreamValue> nativeProc) {
            _runAction = nativeProc;
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
            DreamProcScope scope = new DreamProcScope(instance, usr);

            for (int i = 0; i < _argumentNames.Count; i++) {
                string argumentName = _argumentNames[i];

                if (arguments.NamedArguments.TryGetValue(argumentName, out DreamValue argumentValue)) {
                    scope.CreateVariable(argumentName, argumentValue);
                } else if (i < arguments.OrderedArguments.Count) {
                    scope.CreateVariable(argumentName, arguments.OrderedArguments[i]);
                } else if (_defaultArgumentValues != null && _defaultArgumentValues.TryGetValue(argumentName, out DreamValue defaultValue)) {
                    scope.CreateVariable(argumentName, defaultValue);
                } else {
                    scope.CreateVariable(argumentName, new DreamValue((DreamObject)null));
                }
            }

            scope.SuperProc = SuperProc;
            return _runAction(scope, arguments);
        }
    }
}
