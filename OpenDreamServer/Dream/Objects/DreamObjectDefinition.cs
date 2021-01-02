﻿using OpenDreamServer.Dream.Objects.MetaObjects;
using OpenDreamServer.Dream.Procs;
using OpenDreamShared.Dream;
using System;
using System.Collections.Generic;
using System.Reflection;

namespace OpenDreamServer.Dream.Objects {
    class DreamObjectDefinition {
        public DreamPath Type;
        public IDreamMetaObject MetaObject = null;
        public Dictionary<string, DreamProc> Procs { get; private set; } = new Dictionary<string, DreamProc>();
        public Dictionary<string, DreamProc> OverridingProcs { get; private set; } = new Dictionary<string, DreamProc>();
        public Dictionary<string, DreamValue> Variables { get; private set; } = new Dictionary<string, DreamValue>();
        public Dictionary<string, DreamGlobalVariable> GlobalVariables { get; private set; } = new Dictionary<string, DreamGlobalVariable>();

        //DreamObject variables that need instantiated at object creation
        public Dictionary<string, (DreamPath, DreamProcArguments)> RuntimeInstantiatedVariables = new Dictionary<string, (DreamPath, DreamProcArguments)>();
        public List<(string VariableName, List<(DreamValue Index, DreamValue Value)> Values)> RuntimeInstantiatedLists = new List<(string, List<(DreamValue, DreamValue)>)>();

        private DreamObjectDefinition _parentObjectDefinition = null;

        public DreamObjectDefinition(DreamPath type) {
            Type = type;
        }

        public DreamObjectDefinition(DreamPath type, DreamObjectDefinition parentObjectDefinition) {
            Type = type;
            _parentObjectDefinition = parentObjectDefinition;

            foreach (KeyValuePair<string, DreamValue> variable in parentObjectDefinition.Variables) {
                Variables.Add(variable.Key, variable.Value);
            }

            foreach (KeyValuePair<string, DreamGlobalVariable> globalVariable in parentObjectDefinition.GlobalVariables) {
                GlobalVariables.Add(globalVariable.Key, globalVariable.Value);
            }

            foreach (KeyValuePair<string, (DreamPath, DreamProcArguments)> runtimeInstantiatedVariable in parentObjectDefinition.RuntimeInstantiatedVariables) {
                RuntimeInstantiatedVariables.Add(runtimeInstantiatedVariable.Key, runtimeInstantiatedVariable.Value);
            }

            foreach ((string, List<(DreamValue, DreamValue)>) runtimeInstantiatedList in parentObjectDefinition.RuntimeInstantiatedLists) {
                RuntimeInstantiatedLists.Add((runtimeInstantiatedList.Item1, runtimeInstantiatedList.Item2));
            }
        }

        public void SetVariableDefinition(string variableName, DreamValue value) {
            Variables[variableName] = value;
        }

        public void SetProcDefinition(string procName, DreamProc proc) {
            if (HasProc(procName)) {
                proc.SuperProc = GetProc(procName);
                OverridingProcs[procName] = proc;
            } else {
                Procs[procName] = proc;
            }
        }

        public void SetNativeProc(Func<DreamProcScope, DreamProcArguments, DreamValue> nativeProc) {
            List<Attribute> attributes = new(nativeProc.GetInvocationList()[0].Method.GetCustomAttributes());
            DreamProcAttribute procAttribute = (DreamProcAttribute)attributes.Find(attribute => attribute is DreamProcAttribute);
            if (procAttribute == null) throw new ArgumentException();

            Procs[procAttribute.Name] = new DreamProc(nativeProc);
        }

        public DreamProc GetProc(string procName) {
            if (OverridingProcs.TryGetValue(procName, out DreamProc proc)) {
                return proc;
            } else if (Procs.TryGetValue(procName, out proc)) {
                return proc;
            } else if (_parentObjectDefinition != null) {
                return _parentObjectDefinition.GetProc(procName);
            } else {
                throw new Exception("Object type '" + Type + "' does not have a proc named '" + procName + "'");
            }
        }

        public bool HasProc(string procName) {
            if (Procs.ContainsKey(procName)) {
                return true;
            } else if (_parentObjectDefinition != null) {
                return _parentObjectDefinition.HasProc(procName);
            } else {
                return false;
            }
        }

        public bool HasVariable(string variableName) {
            return Variables.ContainsKey(variableName);
        }

        public bool HasGlobalVariable(string globalVariableName) {
            return GlobalVariables.ContainsKey(globalVariableName);
        }

        public DreamGlobalVariable GetGlobalVariable(string globalVariableName) {
            if (!HasGlobalVariable(globalVariableName)) {
                throw new Exception("Object type '" + Type + "' does not have a global variable named '" + globalVariableName + "'");
            }

            return GlobalVariables[globalVariableName];
        }

        public bool IsSubtypeOf(DreamPath path) {
            if (Type.IsDescendantOf(path)) return true;
            else if (_parentObjectDefinition != null) return _parentObjectDefinition.IsSubtypeOf(path);
            else return false;
        }
    }
}
