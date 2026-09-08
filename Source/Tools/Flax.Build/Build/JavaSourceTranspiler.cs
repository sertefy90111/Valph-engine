// Copyright (c) Wojciech Figat. All rights reserved.

using System;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;

namespace Flax.Build.NativeCpp
{
    /// <summary>
    /// Converts the Java gameplay-script subset used by the editor into the managed source consumed by Flax.Build.
    /// The conversion intentionally keeps Java's familiar class, package, inheritance, primitive and lifecycle syntax
    /// while avoiding a JVM dependency in every shipped game.
    /// </summary>
    internal static class JavaSourceTranspiler
    {
        private static readonly (string JavaName, string RuntimeName)[] LifecycleMethods =
        {
            ("onAwake", "OnAwake"),
            ("onStart", "OnStart"),
            ("onEnable", "OnEnable"),
            ("onDisable", "OnDisable"),
            ("onDestroy", "OnDestroy"),
            ("onUpdate", "OnUpdate"),
            ("onLateUpdate", "OnLateUpdate"),
            ("onFixedUpdate", "OnFixedUpdate"),
            ("onLateFixedUpdate", "OnLateFixedUpdate"),
            ("onDebugDraw", "OnDebugDraw"),
            ("onDebugDrawSelected", "OnDebugDrawSelected"),
            ("onBeginPlay", "OnBeginPlay"),
            ("onEndPlay", "OnEndPlay"),
            ("initialize", "Initialize"),
            ("deinitialize", "Deinitialize"),
        };

        /// <summary>
        /// Generates a C# source file for the specified Java source file.
        /// </summary>
        /// <param name="sourceFile">The Java source file.</param>
        /// <param name="sourceRoot">The module source root.</param>
        /// <param name="outputRoot">The module intermediate directory.</param>
        /// <returns>The generated source path, or null if generation failed.</returns>
        public static string Generate(string sourceFile, string sourceRoot, string outputRoot)
        {
            try
            {
                var source = File.ReadAllText(sourceFile);
                var packageMatch = Regex.Match(source, @"(?m)^\s*package\s+([A-Za-z_][A-Za-z0-9_.]*)\s*;\s*$");
                var packageName = packageMatch.Success ? packageMatch.Groups[1].Value : string.Empty;
                bool usesGenericCollections = Regex.IsMatch(source, @"(?m)^\s*import\s+java\.util\.(?:\*|List|ArrayList|Map|HashMap|Set|HashSet)\s*;\s*$");
                var flaxBaseMatch = Regex.Match(source, @"\bclass\s+[A-Za-z_][A-Za-z0-9_]*[^\{]*\bextends\s+(Script|Actor|GamePlugin)\b");
                var flaxBase = flaxBaseMatch.Success ? flaxBaseMatch.Groups[1].Value : string.Empty;

                // Remove Java package/import declarations. The engine API is exposed through FlaxEngine in the runtime.
                source = Regex.Replace(source, @"(?m)^\s*package\s+[A-Za-z_][A-Za-z0-9_.]*\s*;\s*\r?\n?", string.Empty);
                source = Regex.Replace(source, @"(?m)^\s*import\s+java\.util\.\*\s*;\s*\r?\n?", string.Empty);
                source = Regex.Replace(source, @"(?m)^\s*import\s+[^;]+;\s*\r?\n?", string.Empty);

                // Java annotations do not need a runtime representation in the generated managed source.
                source = Regex.Replace(source, @"(?m)^\s*@(?:[A-Za-z_][A-Za-z0-9_.]*)(?:\([^\r\n]*\))?\s*\r?\n", string.Empty);

                // Translate the Java constructs supported by the gameplay scripting templates.
                source = Regex.Replace(source, @"\bextends\s+([A-Za-z_][A-Za-z0-9_.<>]*)", ": $1");
                source = Regex.Replace(source, @"\bimplements\s+([A-Za-z_][A-Za-z0-9_., <>]*)", ", $1");
                source = Regex.Replace(source, @"\binstanceof\b", "is");
                source = Regex.Replace(source, @"\bsuper\s*\(", "base(");
                source = Regex.Replace(source, @"\bsuper\s*\.", "base.");
                source = Regex.Replace(source, @"\bboolean\b", "bool");
                source = Regex.Replace(source, @"\bString\b", "string");
                source = Regex.Replace(source, @"\bArrayList\b", "List");
                source = Regex.Replace(source, @"\bHashMap\b|\bMap\b", "Dictionary");
                source = Regex.Replace(source, @"\bHashSet\b|\bSet\b", "HashSet");
                source = Regex.Replace(source, @"\bfinal\b", string.Empty);
                source = Regex.Replace(source, @"\bsynchronized\b", string.Empty);
                source = source.Replace("System.out.println", "Debug.Log");
                source = source.Replace("System.out.print", "Debug.Log");

                for (int i = 0; i < LifecycleMethods.Length; i++)
                {
                    var method = LifecycleMethods[i];
                    source = Regex.Replace(source, $@"\b{Regex.Escape(method.JavaName)}\b", method.RuntimeName);
                }

                if (!string.IsNullOrEmpty(flaxBase))
                {
                    var overridableMethods = flaxBase == "Script"
                        ? "OnAwake|OnStart|OnEnable|OnDisable|OnDestroy|OnUpdate|OnLateUpdate|OnFixedUpdate|OnLateFixedUpdate|OnDebugDraw|OnDebugDrawSelected"
                        : flaxBase == "Actor"
                            ? "OnEnable|OnDisable|OnBeginPlay|OnEndPlay"
                            : "Initialize|Deinitialize";
                    source = Regex.Replace(
                        source,
                        $@"(?m)^(\s*)public\s+void\s+({overridableMethods})\s*\(",
                        "$1public override void $2(");
                }

                var generated = new StringBuilder(source.Length + 256);
                generated.AppendLine("// <auto-generated />");
                generated.AppendLine("// Generated from Java source by Flax.Build. Do not edit this file.");
                generated.AppendLine("using System;");
                generated.AppendLine("using FlaxEngine;");
                if (usesGenericCollections)
                    generated.AppendLine("using System.Collections.Generic;");
                if (!string.IsNullOrEmpty(packageName))
                {
                    generated.Append("namespace ").Append(packageName).AppendLine(";");
                }
                generated.AppendLine();
                generated.Append("#line 1 \"").Append(sourceFile.Replace("\\", "\\\\").Replace("\"", "\\\"")).AppendLine("\"");
                generated.Append(source.Trim()).AppendLine();
                generated.AppendLine("#line default");

                var relativePath = Path.GetRelativePath(sourceRoot, sourceFile);
                var relativeDirectory = Path.GetDirectoryName(relativePath);
                var outputDirectory = Path.Combine(outputRoot, "Java", relativeDirectory ?? string.Empty);
                Directory.CreateDirectory(outputDirectory);
                var outputPath = Path.Combine(outputDirectory, Path.GetFileNameWithoutExtension(relativePath) + ".Gen.cs");
                var outputText = generated.ToString();
                if (!File.Exists(outputPath) || !string.Equals(File.ReadAllText(outputPath), outputText, StringComparison.Ordinal))
                    File.WriteAllText(outputPath, outputText, new UTF8Encoding(false));
                return outputPath;
            }
            catch (Exception ex)
            {
                Log.Error($"Failed to translate Java gameplay script '{sourceFile}'. {ex.Message}");
                return null;
            }
        }
    }
}
