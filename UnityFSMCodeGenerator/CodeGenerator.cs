// MIT License
// 
// Unity FSM Code Generator - github.com/justonia/UnityFSMCodeGenerator
//
// Copyright (c) 2018 Justin Larrabee 
// 
// Permission is hereby granted, free of charge, to any person obtaining a copy
// of this software and associated documentation files (the "Software"), to deal
// in the Software without restriction, including without limitation the rights
// to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
// copies of the Software, and to permit persons to whom the Software is
// furnished to do so, subject to the following conditions:
// 
// The above copyright notice and this permission notice shall be included in all
// copies or substantial portions of the Software.
// 
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
// IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
// FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
// AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
// LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
// OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE
// SOFTWARE.

using System.Collections;
using System.Collections.Generic;
using Type=System.Type;
using MethodInfo=System.Reflection.MethodInfo;

namespace UnityFSMCodeGenerator
{
    public class CodeGenerator
    {
        private CodeGeneratorOptions options; 

        public class Output
        {
            public string code;
        }

        public CodeGenerator(CodeGeneratorOptions options)
        {
            this.options = options;
        }

        public Output Generate(FsmModel model, string className)
        {
            var output = new Output();

            var sb = new System.Text.StringBuilder();
            sb.Append(header);

            if (options.commentedOut) {
                sb.Append("/*\n");
            }

            var indentLevel = 0;

            // Add namespace
            if (!string.IsNullOrEmpty(options._namespace)) {
                sb.Append(namespaceTemplate.Replace("{{ns}}", options._namespace));
                indentLevel = options.padding;
            }

            // Build variable names
            var varNames = BuildVariableNames(model);

            // Build class.
            // This is very inefficient with string.Replace, but I did not want to introduce
            // a dependency on an external templating library.
            sb.Append(PostIndent(
                clsStartTemplate
                    .Replace("{{description}}", model.description != null ? "// " + model.description.Replace("\n", "") : "")
                    .Replace("{{cls}}", className)
                    .Replace("{{implementinterfaces}}", GetImplementInterfaces(model, varNames))
                    .Replace("{{genprefab}}", model.fromPrefab != null ? model.fromPrefab.Replace("\"", "") : "")
                    .Replace("{{genguid}}", model.prefabGuid != null ? model.prefabGuid.Replace("\"", "") : "")
                    .Replace("{{startstate}}", GetStartState(model, varNames))
                    .Replace("{{newdefaultcontext}}", GetNewDefaultContext(model, varNames))
                    .Replace("{{defaultcontextinterfaces}}", GetDefaultContextInterfaces(model, varNames))
                    .Replace("{{states}}", GetStates(model))
                    .Replace("{{events}}", GetEvents(model))
                    .Replace("{{icontext}}", GetIContext(model, varNames))
                    .Replace("{{singleinternalsendEvent}}", GetSingleInternalSendEvent(model, varNames))
                    .Replace("{{handleinternalactions}}", GetHandleInternalActions(model, varNames))
                    .Replace("{{checkignoreevents}}", GetIgnoreEvents(model, varNames))
                    .Replace("{{dispatchonenter}}", GetDispatchOnEnter(model, varNames))
                    .Replace("{{dispatchonexit}}", GetDispatchOnExit(model, varNames))
                    .Replace("{{introspectionsupport}}", GetIntrospectionSupport(model, varNames))
                    .Replace("{{debugsupport}}", GetDebugSupport(model, varNames)),
                indentLevel));
            
            // Close namespace
            if (!string.IsNullOrEmpty(options._namespace)) {
                sb.Append("}\n");
            }

            if (options.commentedOut) {
                sb.Append("*/\n");
            }

            output.code = sb.ToString();

            return output;
        }

        private class VarNames
        {
            public Dictionary<Type, string> interfaceArgs;
            public Dictionary<Type, string> interfaceContextCall;
            public Dictionary<string, string> stateNameToEnum;
            public Dictionary<string, string> eventNameToEnum;
            public Dictionary<MethodInfo, string> methodInvoke;
        }

        private string GetDebugSupport(FsmModel model, VarNames varNames)
        {
            if (!options.enableDebugSupport) {
                return "";
            }

            return PostIndent(debugSupportTemplate,
                //.Replace("{{statelookups}}", stateLookups)
                options.padding);
        }

        private string GetIntrospectionSupport(FsmModel model, VarNames varNames)
        {
            if (!options.enableIntrospectionSupport) {
                return "";
            }

            var sb = new System.Text.StringBuilder();
            var sb2 = new System.Text.StringBuilder();
            // Make two dictionary initializer entries for introspection lookup 
            // { "Hung Up", "HungUp" },
            // { "Hung Up", (object)State.HungUp},
            //
            for (int i = 0; i < model.states.Count; i++) {
                var state = model.states[i];
                // { "Hung Up", "HungUp" },
                PreIndent(sb, options.padding);
                sb.Append("{ State.");
                sb.Append(varNames.stateNameToEnum[state.name]);
                sb.Append(", \"");
                sb.Append(state.name);
                sb.Append("\" },\n");

                // { "Hung Up", (object)State.HungUp},
                PreIndent(sb2, options.padding);
                sb2.Append("{ \"");
                sb2.Append(state.name);
                sb2.Append("\", ");
                sb2.Append("State.");
                sb2.Append(varNames.stateNameToEnum[state.name]);
                sb2.Append(" },\n");
            }

            var stateLookups = sb.ToString();
            var stringToEnumState = sb2.ToString();

            // Make list initializer of state names
            sb = new System.Text.StringBuilder();
            for (int i = 0; i < model.states.Count; i++) {
                var state = model.states[i];
                PreIndent(sb, options.padding);
                sb.Append("\"");
                sb.Append(state.name);
                sb.Append("\",\n");
            }

            return PostIndent(introspectionSupportTemplate
                .Replace("{{statelookups}}", stateLookups)
                .Replace("{{stateenumlookup}}", stringToEnumState)
                .Replace("{{statelist}}", sb.ToString()),
                options.padding);
        }

        private string GetImplementInterfaces(FsmModel model, VarNames varNames)
        {
            // This is gross, should add a list of interfaces and make it cleaner
            var sb = new System.Text.StringBuilder();
            bool addedPrefix = false;
            if (options.enableIntrospectionSupport) {
                addedPrefix = true;
                sb.Append(",\n");
                PreIndent(sb, options.padding);
                sb.Append("UnityFSMCodeGenerator.IFsmIntrospectionSupport");
            }

            if (options.enableDebugSupport) {
                if (!addedPrefix) {
                    sb.Append(",\n");
                }

                if (options.enableIntrospectionSupport) {
                    sb.Append(",\n");
                }

                PreIndent(sb, options.padding);
                sb.Append("UnityFSMCodeGenerator.IFsmDebugSupport");
            }

            return sb.ToString();
        }

        private string GetStartState(FsmModel model, VarNames varNames)
        {
            foreach (var state in model.states) {
                if (state.isStart) {
                    return "public const State START_STATE = State." + varNames.stateNameToEnum[state.name];
                }
            }
            return "";
        }

        private string GetDispatchOnExit(FsmModel model, VarNames varNames)
        {
            // Build state switch
            var sb = new System.Text.StringBuilder();
            foreach (var state in model.states) {
                sb.Append(dispatchStateCaseTemplate
                    .Replace("{{name}}", varNames.stateNameToEnum[state.name])
                    .Replace("{{methodcalls}}", GetDispatchMethodCalls(model, state, varNames, state.onExit)));
            }

            return PostIndent(dispatchOnExitTemplate
                .Replace("{{states}}", sb.ToString()), 
                options.padding);
        }

        private string GetDispatchOnEnter(FsmModel model, VarNames varNames)
        {
            // Build state switch
            var sb = new System.Text.StringBuilder();
            foreach (var state in model.states) {
                sb.Append(dispatchStateCaseTemplate
                    .Replace("{{name}}", varNames.stateNameToEnum[state.name])
                    .Replace("{{methodcalls}}", GetDispatchMethodCalls(model, state, varNames, state.onEnter)));
            }

            return PostIndent(dispatchOnEnterTemplate
                .Replace("{{onenterbreakpoint}}", options.enableDebugSupport ? PostIndent(debugSupportOnEnterTemplate, options.padding) : "")
                .Replace("{{states}}", sb.ToString()), 
                options.padding);
        }

        private string GetDispatchMethodCalls(FsmModel model, FsmStateModel state, VarNames varNames, List<FsmOnEnterExitModel> calls)
        {
            var sb = new System.Text.StringBuilder();
            if (calls.Count > 0) {
                sb.Append("\n");
            }
            for (int i = 0; i < calls.Count; i++) {
                var call = calls[i];
                PreIndent(sb, options.padding * 2);
                sb.Append("context.");
                sb.Append(varNames.interfaceContextCall[call._delegate._interface]);
                sb.Append(".");
                sb.Append(varNames.methodInvoke[call._delegate.method]);
                sb.Append(";");
                if (i != calls.Count - 1) {
                    sb.Append("\n");
                }
            }
            return sb.ToString();
        }

        private string GetSingleInternalSendEvent(FsmModel model, VarNames varNames)
        {
            // Build state switch
            var sb = new System.Text.StringBuilder();
            foreach (var state in model.states) {
                bool needDefaultBreak = false;
                sb.Append(sendInternalEventStateCaseTemplate
                    .Replace("{{name}}", varNames.stateNameToEnum[state.name])
                    .Replace("{{transitions}}", GetTransitions(model, state, varNames, out needDefaultBreak))
                    .Replace("{{defaultbreak}}", needDefaultBreak ? PostIndent(defaultBreakEventHandlerTemplate, options.padding * 2, false) : ""));
            }

            return PostIndent(sendInternalEventBaseTemplate
                .Replace("{{states}}", sb.ToString()), 
                options.padding);
        }

        private string GetIgnoreEvents(FsmModel model, VarNames varNames)
        {
            // Build state switch
            var sb = new System.Text.StringBuilder();
            int count = 0;
            foreach (var state in model.states) {
                if (state.ignoreEvents.Count == 0) {
                    continue;
                }

                count++;

                sb.Append(ignoreEventsStateCaseTemplate
                    .Replace("{{name}}", varNames.stateNameToEnum[state.name])
                    .Replace("{{ignoreevents}}", GetStateIgnoreEvents(model, state, varNames))
                    .Replace("{{defaultbreak}}", ""));
            }

            if (count == 0) {
                return PostIndent(ignoreEventsNoneTemplate, options.padding);
            }

            return PostIndent(ignoreEventsTemplate
                .Replace("{{states}}", sb.ToString()), 
                options.padding);
        }

        private string GetStateIgnoreEvents(FsmModel model, FsmStateModel state, VarNames varNames)
        {
            var sb = new System.Text.StringBuilder();
            int count = 0;
            foreach (var ignore in state.ignoreEvents) {
                count++;
                            
                sb.Append(PostIndent(ignoreEventTrue
                    .Replace("{{event}}", varNames.eventNameToEnum[ignore.name]),
                    options.padding * 2, false));
            }

            return sb.ToString();
        }

        private string GetHandleInternalActions(FsmModel model, VarNames varNames)
        {
            bool haveActions = false;
            foreach (var state in model.states) {
                if (state.internalActions.Count > 0) {
                    haveActions = true;
                    break;
                }
            }

            if (!haveActions) {
                return PostIndent(noInternalActionsTemplate, options.padding);
            }

            // Build state switch
            var sb = new System.Text.StringBuilder();
            foreach (var state in model.states) {
                if (state.internalActions.Count == 0) {
                    continue;
                }

                sb.Append(internalActionsStateCaseTemplate
                    .Replace("{{name}}", varNames.stateNameToEnum[state.name])
                    .Replace("{{internalactions}}", GetStateInternalActions(model, state, varNames))
                    .Replace("{{defaultbreak}}", ""));
            }

            return PostIndent(internalActionsTemplate
                .Replace("{{states}}", sb.ToString()), 
                options.padding);
        }

        private string GetStateInternalActions(FsmModel model, FsmStateModel state, VarNames varNames)
        {
            var sb = new System.Text.StringBuilder();
            int count = 0;
            foreach (var action in state.internalActions) {
                count++;
                            
                sb.Append(PostIndent(internalActionsEventTemplate
                    .Replace("{{event}}", varNames.eventNameToEnum[action.evt.name])
                    .Replace("{{methodcall}}", MakeMethodCall(action._delegate, varNames)),
                    options.padding * 2, false));
            }

            return sb.ToString();
        }

        private string MakeMethodCall(FsmDelegateMethod _delegate, VarNames varNames)
        {
            var sb = new System.Text.StringBuilder();
            sb.Append("context.");
            sb.Append(varNames.interfaceContextCall[_delegate._interface]);
            sb.Append(".");
            sb.Append(varNames.methodInvoke[_delegate.method]);
            return sb.ToString();
        }

        private string GetTransitions(FsmModel model, FsmStateModel state, VarNames varNames, out bool needDefaultBreak)
        {
            var sb = new System.Text.StringBuilder();
            int count = 0;
            foreach (var transition in state.transitions) {
                count++;
                sb.Append(PostIndent(stateTransitionTemplate
                    .Replace("{{event}}", varNames.eventNameToEnum[transition.evt.name])
                    .Replace("{{tostate}}", varNames.stateNameToEnum[transition.to.name]),
                    options.padding * 2, false));
            }

            needDefaultBreak = count != model.events.Count;

            return sb.ToString();
        }

        private VarNames BuildVariableNames(FsmModel model)
        {
            Dictionary<Type, string> interfaceArgs = new Dictionary<Type, string>();
            Dictionary<Type, string> interfaceContextCall = new Dictionary<Type, string>();

            // TODO: conflict resolution between same name interfaces in different namespaces
            foreach (var iface in model.context.requiredInterfaces) {
                if (iface.Name[0] == 'I') {
                    interfaceArgs[iface] = iface.Name.Substring(1, 1).ToLower() + iface.Name.Substring(2, iface.Name.Length-2);
                    interfaceContextCall[iface] = iface.Name.Substring(1, 1).ToUpper() + iface.Name.Substring(2, iface.Name.Length-2);
                }
                else {
                    interfaceArgs[iface] = iface.Name.Substring(0, 1).ToLower() + iface.Name.Substring(1, iface.Name.Length-1);
                    interfaceContextCall[iface] = iface.Name.Substring(0, 1).ToUpper() + iface.Name.Substring(1, iface.Name.Length-1);
                }
            }

            Dictionary<MethodInfo, string> methodCalls = new Dictionary<MethodInfo, string>();
            foreach (var state in model.states) {
                foreach (var m in state.onEnter) {
                    methodCalls[m._delegate.method] = MakeMethodInvoke(m._delegate.method);
                }
                foreach (var m in state.onExit) {
                    methodCalls[m._delegate.method] = MakeMethodInvoke(m._delegate.method);
                }
                foreach (var m in state.internalActions) {
                    methodCalls[m._delegate.method] = MakeMethodInvoke(m._delegate.method);
                }
            }

            Dictionary<string, string> stateEnum = new Dictionary<string, string>(); 
            foreach (var state in model.states) {
                stateEnum[state.name] = state.name.Trim().Replace(" ", "");
            }

            Dictionary<string, string> eventEnum = new Dictionary<string, string>(); 
            foreach (var evt in model.events) {
                eventEnum[evt.name] = evt.name.Trim().Replace(" ", "");
            }

            return new VarNames{
                interfaceArgs = interfaceArgs,
                interfaceContextCall = interfaceContextCall,
                stateNameToEnum = stateEnum,
                eventNameToEnum = eventEnum,
                methodInvoke = methodCalls,
            };
        }

        private string GetNewDefaultContext(FsmModel model, VarNames varNames)
        {
            var sb = new System.Text.StringBuilder();
            int count = 0;
            foreach (var kvp in varNames.interfaceArgs) {
                PreIndent(sb, options.padding);
                sb.Append(kvp.Key.FullName);
                sb.Append(" ");
                sb.Append(kvp.Value);
                if (count != varNames.interfaceArgs.Count-1) {
                    sb.Append(",\n");
                }
                else {
                    sb.Append(",");
                }
                count++;
            }

            var _params = sb.ToString();

            sb = new System.Text.StringBuilder();
            foreach (var kvp in varNames.interfaceArgs) {
                sb.Append(varNames.interfaceContextCall[kvp.Key]);
                sb.Append(" = ");
                sb.Append(kvp.Value);
                sb.Append(", ");
            }

            var args = sb.ToString();

            return PostIndent(newDefaultContextTemplate
                .Replace("{{params}}", _params)
                .Replace("{{args}}", args),
                options.padding);
        }

        private string GetDefaultContextInterfaces(FsmModel model, VarNames varNames)
        {
            var sb = new System.Text.StringBuilder();
            foreach (var kvp in varNames.interfaceArgs) {
                sb.Append("public ");
                sb.Append(kvp.Key.FullName);
                sb.Append(" ");
                sb.Append(varNames.interfaceContextCall[kvp.Key]);
                sb.Append(" { get; set; }\n");
            }
            return PostIndent(sb.ToString(), options.padding * 2, false);
        }

        private string MakeMethodInvoke(MethodInfo method)
        {
            // TODO: will get more complex when supporting calls with arguments
            return method.Name + "()";
        }

        private string GetIContext(FsmModel model, VarNames varNames)
        {
            var sb = new System.Text.StringBuilder();
            for (int i = 0; i < model.context.requiredInterfaces.Count; i++) {
                var iface = model.context.requiredInterfaces[i];

                PreIndent(sb, options.padding * 2);

                sb.Append(iface.FullName);
                sb.Append(" ");
                sb.Append(varNames.interfaceContextCall[iface]); 
                sb.Append(" { get; }");

                if (i+1 != model.context.requiredInterfaces.Count) {
                    sb.Append("\n");
                }
            }
            return sb.ToString();
        }

        private string GetEvents(FsmModel model)
        {
            var sb = new System.Text.StringBuilder();
            for (int i = 0; i < model.events.Count; i++) {
                var evt = model.events[i];
                PreIndent(sb, options.padding * 2);
                sb.Append(evt.name.Trim().Replace(" ", ""));
                sb.Append(",");
                if (i+1 != model.events.Count) {
                    sb.Append("\n");
                }
            }
            return sb.ToString();
        }

        private string GetStates(FsmModel model)
        {
            var sb = new System.Text.StringBuilder();
            for (int i = 0; i < model.states.Count; i++) {
                var state = model.states[i];
                PreIndent(sb, options.padding * 2);
                sb.Append(state.name.Trim().Replace(" ", ""));
                sb.Append(",");
                if (i+1 != model.states.Count) {
                    sb.Append("\n");
                }
            }
            return sb.ToString();
        }

        private void PreIndent(System.Text.StringBuilder sb, int indent)
        {
            for (int i = 0; i < indent; i++) {
                sb.Append(" ");
            }
        }

        private string PostIndent(string input, int indent, bool finalNewline = true)
        {
            if (indent == 0) {
                return input;
            }

            var lines = input.Split('\n');
            var sb = new System.Text.StringBuilder();
            for (int i = 0; i < lines.Length; i++) {
                var line = lines[i];
                PreIndent(sb, indent);
                sb.Append(line);
                if (finalNewline || i != lines.Length-1) {
                    sb.Append("\n");
                }
            }

            return sb.ToString();
        }

        private readonly string header = @"//
// Auto-generated by Unity FSM Code Generator:
//     https://github.com/justonia/UnityFSMCodeGenerator
//
// ** Do not modify, changes will be overwritten. **
//

using System.Collections;
using System.Collections.Generic;
";
        private readonly string namespaceTemplate = @"
namespace {{ns}}
{
";

        private readonly string clsStartTemplate = @"{{description}}
public class {{cls}} :  UnityFSMCodeGenerator.BaseFsm{{implementinterfaces}}
{
    public readonly static string GeneratedFromPrefab = ""{{genprefab}}"";
    public readonly static string GeneratedFromGUID = ""{{genguid}}"";
    
    public enum State
    {
{{states}}
    }

    {{startstate}};

    public enum Event
    {
{{events}}
    }

    public interface IContext : UnityFSMCodeGenerator.IFsmContext
    {
        State State { get; set; }
{{icontext}}
    }

    #region Public Methods

    public IContext Context { get { return context; }}
    
    // {{cls}} is completely stateless when events are not firing. Bind() sets
    // the current context but does nothing else until you call SendEvent().
    // Instances of this class may be re-used and shared by calling Bind() in-between
    // invocations of SendEvent().
    public void Bind(IContext context)
    {
        if (isFiring) {
            throw new System.InvalidOperationException(""Cannot call {{cls}}.Bind(IContext) while events are in-progress"");
        }

        this.context = context;
    }

    // Send an event, possibly triggering a transition, an internal action, or an 
    // exception if the event is not handled in the current state. If an event is in
    // process of firing, the event is queued and then sent once firing is done.
    public void SendEvent(Event _event)
    {
        if (eventPool.Count == 0) {
            eventPool.Enqueue(new QueuedEvent());
        }
        var queuedEvent = eventPool.Dequeue();
        queuedEvent._event = _event;
        InternalSendEvent(queuedEvent);
    }
{{newdefaultcontext}}
    
    // Convenience so you can use the State enum in a Dictionary and not worry about
    // garbage creation via boxing: new Dictionary<State, Foo>(new StateComparer());
    public struct StateComparer : IEqualityComparer<State>
    {
        public bool Equals(State x, State y) { return x == y; }
        public int GetHashCode(State obj) { return obj.GetHashCode(); }
    }

    #endregion

    #region Private Variables
       
    public override UnityFSMCodeGenerator.IFsmContext BaseContext { get { return context; }}
    
    private class QueuedEvent
    {
        public Event _event;
    }

    readonly Queue<QueuedEvent> eventQueue = new Queue<QueuedEvent>();
    readonly Queue<QueuedEvent> eventPool = new Queue<QueuedEvent>();
    private bool isFiring;
    private IContext context;

    private class DefaultContext : IContext
    {
        public State State { get; set; }
{{defaultcontextinterfaces}}
    }

    #endregion

    #region Private Methods
    
    private void InternalSendEvent(QueuedEvent _event)
    {
        if (isFiring) {
            eventQueue.Enqueue(_event);
            return;
        }

        try {
            isFiring = true;

            SingleInternalSendEvent(_event);

            while (eventQueue.Count > 0) {
                var queuedEvent = eventQueue.Dequeue();
                SingleInternalSendEvent(queuedEvent);
                eventPool.Enqueue(queuedEvent);
            }
        }
        finally {
            isFiring = false;
            eventQueue.Clear();
        }
    }

{{singleinternalsendEvent}}
    
{{handleinternalactions}}

{{checkignoreevents}}

    private void SwitchState(State from, State to)
    {
        context.State = to;
        DispatchOnExit(from);
        DispatchOnEnter(to);
    }
    
    private bool TransitionTo(State state, State from)
    {
        // TODO: Guard conditions might hook in here
        return true;
    }

{{dispatchonenter}}
{{dispatchonexit}}
    #endregion
{{introspectionsupport}}
{{debugsupport}}
}
";

    private readonly string introspectionSupportTemplate = @"
#region IFsmIntrospectionSupport

string IFsmIntrospectionSupport.GeneratedFromPrefabGUID { get { return GeneratedFromGUID; }}

private Dictionary<State, string> introspectionStateLookup = new Dictionary<State, string>(new StateComparer()){
{{statelookups}}};
private List<string> introspectionStringStates = new List<string>(){
{{statelist}}};
private Dictionary<string, State> stateNameToStateLookup = new Dictionary<string, State>(){
{{stateenumlookup}}};

string UnityFSMCodeGenerator.IFsmIntrospectionSupport.State { get { return context != null ? introspectionStateLookup[context.State] : null; }}

string UnityFSMCodeGenerator.IFsmIntrospectionSupport.StateFromEnumState(object state) { return introspectionStateLookup[(State)state]; }

List<string> UnityFSMCodeGenerator.IFsmIntrospectionSupport.AllStates { get { return introspectionStringStates; }}

object UnityFSMCodeGenerator.IFsmIntrospectionSupport.EnumStateFromString(string stateName) { return stateNameToStateLookup[stateName]; }

#endregion";

    private readonly string debugSupportTemplate = @"
#region IFsmDebugSupport

private UnityFSMCodeGenerator.BreakpointAction onBreakpointSet = null;
private UnityFSMCodeGenerator.BreakpointAction onBreakpointHit = null;
private UnityFSMCodeGenerator.BreakpointsResetAction onBreakpointsReset = null;
private HashSet<State> onEnterBreakpoints = new HashSet<State>(new StateComparer());

event UnityFSMCodeGenerator.BreakpointAction UnityFSMCodeGenerator.IFsmDebugSupport.OnBreakpointSet { add { onBreakpointSet += value; } remove { onBreakpointSet -= value; }}
event UnityFSMCodeGenerator.BreakpointAction UnityFSMCodeGenerator.IFsmDebugSupport.OnBreakpointHit { add { onBreakpointHit += value; } remove { onBreakpointHit -= value; }}
event UnityFSMCodeGenerator.BreakpointsResetAction UnityFSMCodeGenerator.IFsmDebugSupport.OnBreakpointsReset { add { onBreakpointsReset += value; } remove { onBreakpointsReset -= value; }}

int UnityFSMCodeGenerator.IFsmDebugSupport.OnEnterBreakpointCount { get { return onEnterBreakpoints.Count; }}

void UnityFSMCodeGenerator.IFsmDebugSupport.SetOnEnterBreakpoint(object _state)
{
    var state = (State)_state;
    onEnterBreakpoints.Add(state);
    if (onBreakpointSet != null) {
        onBreakpointSet(this, _state);
    }
}

void UnityFSMCodeGenerator.IFsmDebugSupport.ResetBreakpoints()
{
    onEnterBreakpoints.Clear();
    if (onBreakpointsReset != null) {
        onBreakpointsReset(this);
    }
}

#endregion";

    private readonly string debugSupportOnEnterTemplate = @"
if (onEnterBreakpoints.Contains(state)) {
    UnityEngine.Debug.LogFormat(""{0}.OnEnter breakpoint triggered for state: {1}"", GetType().Name, state.ToString());
    if (onBreakpointHit != null) {
        onBreakpointHit(this, state);
    }
    // NOTE: This is not the same as setting a breakpoint in Visual Studio. This method
    // will continue executing and the editor will pause at some point later in the frame.
    UnityEngine.Debug.Break();
}";

    private readonly string sendInternalEventBaseTemplate = @"
private void SingleInternalSendEvent(QueuedEvent _eventData)
{
    Event _event = _eventData._event;
    State from = context.State;

    switch (context.State) {{{states}}
    }
}";

    private readonly string sendInternalEventStateCaseTemplate = @"
    case State.{{name}}:
        switch (_event) {{{transitions}}{{defaultbreak}}
        }
        break;
";

    private readonly string stateTransitionTemplate = @"
case Event.{{event}}:
    if (TransitionTo(State.{{tostate}}, from)) {
        SwitchState(from, State.{{tostate}});
    }
    break;";

    /*
    private readonly string defaultBreakTemplate = @"
default:
    break;";
    */

    private readonly string defaultBreakEventHandlerTemplate = @"
default:
    if (!HandleInternalActions(from, _event) && !IsEventIgnored(from, _event)) {
        throw new System.Exception(string.Format(""Unhandled event '{0}' in state '{1}'"", _event.ToString(), context.State.ToString()));
    }
    break;";

    private readonly string dispatchOnEnterTemplate = @"
private void DispatchOnEnter(State state)
{{{onenterbreakpoint}}
    switch (state) {{{states}}
    }
}";

    private readonly string dispatchOnExitTemplate = @"
private void DispatchOnExit(State state)
{
    switch (state) {{{states}}
    }
}";


    private readonly string dispatchStateCaseTemplate = @"
    case State.{{name}}:{{methodcalls}}
        break;";
    
    //
    // Internal Actions
    //
    
    private readonly string noInternalActionsTemplate = @"
private bool HandleInternalActions(State state, Event _event)
{
    // no states have internal actions, intentionally empty
    return false;
}";

    private readonly string internalActionsTemplate = @"
private bool HandleInternalActions(State state, Event _event)
{
    var handled = false;

    switch (state) {{{states}}
    }

    return handled;
}";
    
    private readonly string internalActionsStateCaseTemplate = @"
    case State.{{name}}:
        switch (_event) {{{internalactions}}{{defaultbreak}}
        }
        break;
";

    private readonly string internalActionsEventTemplate = @"
case Event.{{event}}:
    {{methodcall}};
    handled = true;
    break;";

    private readonly string newDefaultContextTemplate = @"
public static IContext NewDefaultContext(
{{params}}
    State startState = START_STATE)
{
    return new DefaultContext{
        State = startState,
        {{args}}
    };
}";
    
    private readonly string ignoreEventsTemplate = @"
private bool IsEventIgnored(State state, Event _event)
{
    var ignored = false;

    switch (state) {{{states}}
    }

    return ignored;
}";
    
    private readonly string ignoreEventsNoneTemplate = @"
private bool IsEventIgnored(State state, Event _event)
{
    return false;
}";

    private readonly string ignoreEventsStateCaseTemplate = @"
    case State.{{name}}:
        switch (_event) {{{ignoreevents}}{{defaultbreak}}
        }
        break;
";

    private readonly string ignoreEventTrue = @"
case Event.{{event}}:
    ignored = true;
    break;";

    }
}
