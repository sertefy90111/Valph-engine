# Java gameplay scripting

Valph/Flax projects use Java as the default gameplay scripting language exposed by the editor. Existing C# and C++ projects remain supported and their source files are still recognized by the content system.

## What happens during a build

1. A script is created as `Source/<module>/<name>.java`.
2. The editor watches `.java` files and marks the scripts workspace dirty.
3. `Flax.Build` converts the Java gameplay subset into an intermediate `.Gen.cs` file.
4. The existing managed runtime compiles and loads the generated file, so hot reload, actor attachment, and cooked builds use the same pipeline as the rest of the engine.

Generated files live under the build intermediate directory and must not be edited or committed.

## Supported source shape

The translator intentionally covers the syntax needed by the engine templates rather than pretending to be a complete JVM implementation:

* `package`, `import`, classes, interfaces, `extends`, and `implements`;
* Java primitive names such as `boolean` and `String`;
* Java collection names `ArrayList` and `HashMap`;
* `@Override`, `super`, `instanceof`, and common control-flow syntax;
* Flax lifecycle methods: `onAwake`, `onStart`, `onEnable`, `onDisable`, `onDestroy`, `onUpdate`, `onLateUpdate`, `onFixedUpdate`, `onLateFixedUpdate`, `onDebugDraw`, `onDebugDrawSelected`, `onBeginPlay`, `onEndPlay`, `initialize`, and `deinitialize`.

Use the generated Java templates as the API contract. Java libraries that require a JVM, reflection, or bytecode execution are not embedded in the engine. Engine/build implementation files may still contain C++ and C#. New gameplay script creation is Java-first, while a project can select the legacy C# or C++ workflow in Source Code options when maintaining an existing project.
