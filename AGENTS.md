No code comment

Philosophy: Zero cyclomatic complexity systems, strict zero-allocation hot paths, maximum Burst compliance,
Zero Complexity Validation: Is the system's OnUpdate CC=1? If not, extract the logic into a pure function and write a test for that pure function.
Self-documenting names, clear structure, and well-named systems replace every comment. If you feel the urge to write a comment, rename the variable, extract the method, or redesign the structure until the comment is unnecessary.

Follow DOD. AND Data structure follows Database normalization technices.
Mehtord may never have a return statement. Follow TryXOut Pattarn in case we need bool. OUT over return;
Goal is to run test mainly. Its test driven development.

Create Files in 6 places:

[Scripts](Assets/Scripts/Scripts)
[Scripts.Authoring](Assets/Scripts/Scripts.Authoring)
[Scripts.Data](Assets/Scripts/Scripts.Data)
[Scripts.Debug](Assets/Scripts/Scripts.Debug)
[Scripts.Editor](Assets/Scripts/Scripts.Editor)
[Scripts.Tests](Assets/Scripts/Scripts.Tests)

Check:

[ScriptStructure.md](ScriptStructure.md)
[SceneHierarchy.md](SceneHierarchy.md)

Editor: UITOOLKIT DEVELOIPER UI INGAME.
Debug: GIZMOS