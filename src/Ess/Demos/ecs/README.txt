Unity ECS. The made code is a struct and an ECS component. A Burst job drives it.

Use it
  1. Open a Unity 7 project that has these packages:
       com.unity.entities 7.0.0, com.unity.burst 2.0.0, com.unity.collections 7.0.0,
       com.unity.mathematics 1.4.0, com.unity.test-framework 1.9.0
  2. Copy this whole folder into the project's Assets folder.
  3. Window > General > Test Runner > EditMode > Run All.
  4. Expect 3 passed.

What the tests do
  The machine is added to an entity as a component.
  A [BurstCompile] IJobEntity sends it events, step by step.
  The same steps run in plain code. Both must agree, through a choice and history.
  A job without Burst proves the "ran under Burst" check can fail.

Files
  PlayerSm.plantuml            the diagram. BaseList makes the struct an IComponentData.
  PlayerSm.cs                  made by ess. A struct. Do not edit.
  PlayerSmEcsTests.cs          the system, the job and the tests. Read it top to bottom.
  EssStructProbe.Tests.asmdef  the test assembly.

Rules for your own machines
  Pass the machine by ref. A copy dispatches on the copy.
  A new (zero) machine has stateId ROOT. Call Start() once when you see that.
  Actions in the diagram must be Burst safe: numbers and bools in vars, no strings, no lists.
