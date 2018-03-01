# Unity FSM Code Generator
C# code generation tool for finite state machines in Unity that can also parse a declarative PlayMaker FSM. Also has a super boring name.

## Motivation

Coding a finite state machine by hand is tedious. And error prone. Usually we just get lazy and throw a bunch of if-then-else statements in a function and hope for the best.

There are great libraries for building an FSM in code (see <https://github.com/dotnet-state-machine/stateless>), but I still find the machine definitions hard to understand despite the syntactic sugar.

Thankfully there is a great visual FSM designer on the Unity asset store: [PlayMaker](https://www.assetstore.unity3d.com/en/#!/content/368).

Sure it's great, you can write custom actions and visually script gameplay -- cool, 100% worth the $65 trust me. But it doesn't help you at all when you absolutely and unequivocally need FSM logic in code (debuggability, performance, portability, versioning, etc etc). 

Wouldn't it be great if you could visually design this:

![A telephone FSM](https://github.com/justonia/UnityFSMCodeGenerator/raw/master/Docs/telephone_playmaker_fsm_v02.PNG)

And through some hand-wavy magic end up with this:

![C# code](https://github.com/justonia/UnityFSMCodeGenerator/raw/master/Docs/telephone_code_1_v02.png)

And not be forced by hand to write this:

![Tedious C# code](https://github.com/justonia/UnityFSMCodeGenerator/raw/master/Docs/telephone_code_2_v02.png)

Just to call your fancy well-architected code like this:

![Look ma', interfaces](https://github.com/justonia/UnityFSMCodeGenerator/raw/master/Docs/telephone_code_3_v02.png)

**Well now you can!**

## Gory Details

This library contains a collection of custom PlayMaker actions you can use to define a *declarative* FSM -- i.e. one that defines the logic of what you want do and when, but does not itself contain the "how". That sounds kind of pointless, but in practice it means you let the FSM deal with all the annoying if-then-else state management and you write the fun stuff.

The FSMs generated by this library are themselves completely stateless. You provide interface implementations and a place to store the state (see Examples/Telephone/TelephoneFSM.Generated.cs:IContext and Examples/Telephone/Telephone.cs), call SendEvent in response to external stimuli, and like boring magic your methods get called when they should.

Once a C# FSM is generated from a PlayMaker FSM, you can copy and paste and share the C# FSM code as it has zero dependencies on PlayMaker. Additionally, if you hate yourself and are too cheap to spend $65 on PlayMaker, you can manually create the data model used by the code generator. I wouldn't recommend it though.

## Examples

WIP: See the telephone example under Examples/Telephone. The TelephoneFSM.prefab has a PlayMakerCodeGenerator script on it that creates the TelephoneFSM.Generated.cs file.

**Note:** You will need your own copy of PlayMaker to be able to modify the visual FSM, but the example will work perfectly fine without it. 

## Status

Alpha, but functional.

Transitions, enter/exit callbacks, internal action callbacks, and code generation by introspection into a PlayMaker FSM all working.

Still needs much more documentation, unit tests, support for IgnoreEventAction, better error handling in the UI, potentially support for delegate arguments (maybe), and a bunch of other stuff.
