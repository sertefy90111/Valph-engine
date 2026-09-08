// Copyright (c) Wojciech Figat. All rights reserved.

using System;
using System.IO;
using System.Linq;
using System.Text;
using FlaxEditor.Content.Settings;
using FlaxEngine;

namespace FlaxEditor.Content
{
    /// <summary>
    /// Context proxy object for Java gameplay source files.
    /// Java source is converted to the engine managed runtime by Flax.Build before the game scripts assembly is compiled.
    /// </summary>
    /// <seealso cref="FlaxEditor.Content.ScriptProxy" />
    public abstract class JavaProxy : ScriptProxy
    {
        /// <summary>
        /// The script files extension filter.
        /// </summary>
        public static readonly string ExtensionFilter = "*.java";

        private static readonly string[] ReservedNames =
        {
            "abstract", "assert", "boolean", "break", "byte", "case", "catch", "char", "class", "const", "continue",
            "default", "do", "double", "else", "enum", "extends", "final", "finally", "float", "for", "goto", "if",
            "implements", "import", "instanceof", "int", "interface", "long", "native", "new", "package", "private",
            "protected", "public", "return", "short", "static", "strictfp", "super", "switch", "synchronized", "this",
            "throw", "throws", "transient", "try", "void", "volatile", "while"
        };

        /// <summary>
        /// Gets the path for the Java template.
        /// </summary>
        /// <param name="path">The path to the template.</param>
        protected abstract void GetTemplatePath(out string path);

        /// <inheritdoc />
        public override bool IsProxyFor(ContentItem item)
        {
            return item is JavaScriptItem;
        }

        /// <inheritdoc />
        public override ContentItem ConstructItem(string path)
        {
            return new JavaScriptItem(path);
        }

        /// <inheritdoc />
        public override bool IsFileNameValid(string filename)
        {
            return base.IsFileNameValid(filename) && !ReservedNames.Contains(filename, StringComparer.Ordinal);
        }

        /// <inheritdoc />
        public override void Create(string outputPath, object arg)
        {
            GetTemplatePath(out var templatePath);
            var source = File.ReadAllText(templatePath);

            // Build a Java package from the project module and the folders below Source.
            var sourceDirectory = Globals.ProjectFolder.Replace('\\', '/') + "/Source/";
            var outputDirectory = new FileInfo(outputPath).DirectoryName.Replace('\\', '/');
            var relativeDirectory = outputDirectory.StartsWith(sourceDirectory, StringComparison.OrdinalIgnoreCase)
                ? outputDirectory.Substring(sourceDirectory.Length).Trim('/')
                : Editor.Instance.GameProject.Name;
            var package = BuildPackageName(relativeDirectory);
            if (string.IsNullOrEmpty(package))
                package = SanitizePackagePart(Editor.Instance.GameProject.Name);

            var gameSettings = GameSettings.Load();
            var scriptName = ScriptItem.CreateScriptName(outputPath);
            var copyrightComment = string.IsNullOrEmpty(gameSettings.CopyrightNotice)
                ? string.Empty
                : string.Format("// {0}{1}{1}", gameSettings.CopyrightNotice, Environment.NewLine);
            source = source.Replace("%copyright%", copyrightComment);
            source = source.Replace("%class%", scriptName);
            source = source.Replace("%package%", package);
            // Keep this replacement for plugins that use the common script template placeholders.
            source = source.Replace("%namespace%", package);

            File.WriteAllText(outputPath, source, Encoding.UTF8);
        }

        private static string BuildPackageName(string relativeDirectory)
        {
            var parts = relativeDirectory.Split(new[] { '/', '\\' }, StringSplitOptions.RemoveEmptyEntries);
            var result = new StringBuilder();
            for (int i = 0; i < parts.Length; i++)
            {
                var part = SanitizePackagePart(parts[i]);
                if (part.Length == 0)
                    continue;
                if (result.Length != 0)
                    result.Append('.');
                result.Append(part);
            }
            return result.ToString();
        }

        private static string SanitizePackagePart(string value)
        {
            if (string.IsNullOrEmpty(value))
                return string.Empty;

            var result = new StringBuilder(value.Length + 1);
            for (int i = 0; i < value.Length; i++)
            {
                var c = value[i];
                result.Append(char.IsLetterOrDigit(c) || c == '_' ? c : '_');
            }
            if (result.Length == 0)
                return string.Empty;
            if (char.IsDigit(result[0]))
                result.Insert(0, '_');
            if (ReservedNames.Contains(result.ToString(), StringComparer.Ordinal))
                result.Insert(0, '_');
            return result.ToString();
        }

        /// <inheritdoc />
        public override string FileExtension => "java";

        /// <inheritdoc />
        public override Color AccentColor => Color.FromRGB(0xB07219);
    }

    /// <summary>
    /// Context proxy object for Java Script files.
    /// </summary>
    [ContentContextMenu("New/Java/Java Script")]
    public sealed class JavaScriptProxy : JavaProxy
    {
        /// <inheritdoc />
        public override string Name => "Java Script";

        /// <inheritdoc />
        protected override void GetTemplatePath(out string path)
        {
            path = StringUtils.CombinePaths(Globals.EngineContentFolder, "Editor/Scripting/JavaScriptTemplate.java");
        }
    }

    /// <summary>
    /// Context proxy object for Java Actor files.
    /// </summary>
    [ContentContextMenu("New/Java/Java Actor")]
    public sealed class JavaActorProxy : JavaProxy
    {
        /// <inheritdoc />
        public override string Name => "Java Actor";

        /// <inheritdoc />
        protected override void GetTemplatePath(out string path)
        {
            path = StringUtils.CombinePaths(Globals.EngineContentFolder, "Editor/Scripting/JavaActorTemplate.java");
        }
    }

    /// <summary>
    /// Context proxy object for Java GamePlugin files.
    /// </summary>
    [ContentContextMenu("New/Java/Java GamePlugin")]
    public sealed class JavaGamePluginProxy : JavaProxy
    {
        /// <inheritdoc />
        public override string Name => "Java GamePlugin";

        /// <inheritdoc />
        protected override void GetTemplatePath(out string path)
        {
            path = StringUtils.CombinePaths(Globals.EngineContentFolder, "Editor/Scripting/JavaGamePluginTemplate.java");
        }
    }

    /// <summary>
    /// Context proxy object for empty Java files.
    /// </summary>
    [ContentContextMenu("New/Java/Java Empty File")]
    public sealed class JavaEmptyProxy : JavaProxy
    {
        /// <inheritdoc />
        public override string Name => "Java Empty File";

        /// <inheritdoc />
        protected override void GetTemplatePath(out string path)
        {
            path = StringUtils.CombinePaths(Globals.EngineContentFolder, "Editor/Scripting/JavaEmptyTemplate.java");
        }
    }

    /// <summary>
    /// Context proxy object for empty Java class files.
    /// </summary>
    [ContentContextMenu("New/Java/Java Class")]
    public sealed class JavaEmptyClassProxy : JavaProxy
    {
        /// <inheritdoc />
        public override string Name => "Java Class";

        /// <inheritdoc />
        protected override void GetTemplatePath(out string path)
        {
            path = StringUtils.CombinePaths(Globals.EngineContentFolder, "Editor/Scripting/JavaClassTemplate.java");
        }
    }

    /// <summary>
    /// Context proxy object for empty Java interface files.
    /// </summary>
    [ContentContextMenu("New/Java/Java Interface")]
    public sealed class JavaEmptyInterfaceProxy : JavaProxy
    {
        /// <inheritdoc />
        public override string Name => "Java Interface";

        /// <inheritdoc />
        protected override void GetTemplatePath(out string path)
        {
            path = StringUtils.CombinePaths(Globals.EngineContentFolder, "Editor/Scripting/JavaInterfaceTemplate.java");
        }
    }
}
